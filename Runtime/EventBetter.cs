// EventBetter
// Copyright (c) 2018, Piotr Gwiazdowski <gwiazdorrr+github at gmail.com>

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if NET_4_6
using System.Threading.Tasks;
#endif



/// <summary>
/// Intentionally made partial, in case you want to extend it easily.
/// </summary>
public static partial class EventBetter
{
    /// <summary>
    /// Register a message handler.
    /// 
    /// The <paramref name="handler"/> will be invoked every time a message of type <typeparamref name="MessageType"/> is raised,
    /// unless <paramref name="listener"/> gets destroyed or one of Unlisten/Clear methods is called.
    /// </summary>
    /// <typeparam name="ListenerType"></typeparam>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="listener"></param>
    /// <param name="handler"></param>
    /// <param name="once">After the <paramref name="handler"/> is invoked - unlisten automatically.</param>
    /// <param name="exculdeInactive">If <paramref name="listener"/> is a Behaviour or GameObject, will only invoke <paramref name="handler"/>
    /// if <paramref name="listener"/> is active and enabled.</param>
    /// <exception cref="System.InvalidOperationException">Thrown if the internal worker has been disabled somehow.</exception>
    public static void Listen<ListenerType, MessageType>(ListenerType listener, System.Action<MessageType> handler,
        bool once = false,
        bool exculdeInactive = false)
        where ListenerType : UnityEngine.Object
    {
        HandlerFlags flags = HandlerFlags.IsUnityObject;

        if (once)
            flags |= HandlerFlags.Once;
        if (exculdeInactive)
            flags |= HandlerFlags.OnlyIfActiveAndEnabled;

        RegisterInternal(listener, handler, flags);
    }

    /// <summary>
    /// Register a message handler. No listener, you unregister by calling <see cref="IDisposable.Dispose">Dispose</see> on returned object.
    /// Handler is not limited in what it is allowed to capture.
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static IDisposable ListenManual<MessageType>(System.Action<MessageType> handler)
    {
        // use the dict as a listener here, it will ensure the handler is going to live forever
        var actualHandler = RegisterInternal<object, MessageType>(s_entries, (msg) => handler(msg), HandlerFlags.None);
        return new ManualHandlerDisposable()
        {
            Handler = actualHandler,
            MessageType = typeof(MessageType)
        };
    }

    /// <summary>
    /// Invoke all registered handlers for this message type immediately.
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="message"></param>
    /// <returns>True if there are any handlers for this message type, false otherwise.</returns>
    public static bool Raise<MessageType>(MessageType message)
    {
        return RaiseInternal(message);
    }

    /// <summary>
    /// Unregisters all <typeparamref name="MessageType"/> handlers for a given listener.
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="listener"></param>
    /// <returns>True if there were any handlers, false otherwise.</returns>
    public static bool Unlisten<MessageType>(UnityEngine.Object listener)
    {
        if (listener == null)
            throw new ArgumentNullException("listener");

        return UnregisterInternal(typeof(MessageType), listener, (eventEntry, index, referenceListener) => object.ReferenceEquals(eventEntry.listeners[index], referenceListener));
    }

    /// <summary>
    /// Unregisters all message types for a given listener.
    /// </summary>
    /// <param name="listener"></param>
    /// <returns>True if there were any handlers, false otherwise.</returns>
    public static bool UnlistenAll(UnityEngine.Object listener)
    {
        if (listener == null)
            throw new ArgumentNullException("listener");

        bool anyListeners = false;
        foreach (var entry in s_entriesList)
        {
            anyListeners |= UnregisterInternal(entry, listener, (eventEntry, index, referenceListener) => object.ReferenceEquals(eventEntry.listeners[index], referenceListener));
        }

        return anyListeners;
    }

    /// <summary>
    /// Unregisters everything.
    /// </summary>
    public static void Clear()
    {
        s_entries.Clear();
        s_entriesList.Clear();
        if (s_worker)
        {
            UnityEngine.Object.Destroy(s_worker.gameObject);
            s_worker = null;
        }
    }

    /// <summary>
    /// Removes handlers that will now longer be called because their listeners have been destroyed. Normally
    /// there's no reason to call that, since EventBetter does it behind the scenes in every LateUpdate.
    /// </summary>
    public static void RemoveUnusedHandlers()
    {
        foreach (var entry in s_entriesList)
        {
            RemoveUnusedHandlers(entry);
        }
    }


    #region Coroutine Support

    /// <summary>
    /// Use this in coroutines. Yield will return when at least one event of type <typeparamref name="MessageType"/>
    /// has been raised. To get the messages use <see cref="YieldListener{MessageType}.First"/> or 
    /// <see cref="YieldListener{MessageType}.Messages"/>
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <returns></returns>
    public static YieldListener<MessageType> ListenWait<MessageType>()
        where MessageType : class
    {
        return new YieldListener<MessageType>();
    }

    public class YieldListener<MessageType> : System.Collections.IEnumerator, IDisposable
        where MessageType : class
    {
        private Delegate handler;
        public List<MessageType> Messages { get; private set; }

        public MessageType First
        {
            get
            {
                if (Messages == null || Messages.Count == 0)
                    return null;
                return Messages[0];
            }
        }

        internal YieldListener()
        {
            handler = EventBetter.RegisterInternal<YieldListener<MessageType>, MessageType>(
                this, (msg) => OnMessage(msg), HandlerFlags.DontInvokeIfAddedInAHandler);
        }

        public void Dispose()
        {
            if (handler != null)
            {
                EventBetter.UnlistenHandler(typeof(MessageType), handler);
                handler = null;
            }
        }

        private void OnMessage(MessageType msg)
        {
            if (Messages == null)
            {
                Messages = new List<MessageType>();
            }
            Messages.Add(msg);
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            if (Messages != null)
            {
                Dispose();
                return false;
            }
            return true;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return null; }
        }

        void System.Collections.IEnumerator.Reset()
        {
        }
    }

    #endregion

    #region Async Support

#if NET_4_6

    public static async Task<MessageType> ListenAsync<MessageType>()
    {
        var tcs = new TaskCompletionSource<MessageType>();

        var handler = RegisterInternal<object, MessageType>(s_entries,
            (msg) => tcs.SetResult(msg), HandlerFlags.DontInvokeIfAddedInAHandler);

        try
        {
            return await tcs.Task;
        }
        finally
        {
            EventBetter.UnlistenHandler(typeof(MessageType), handler);
        }
    }

#endif

    #endregion

    #region Private

    private class ManualHandlerDisposable : IDisposable
    {
        public Type MessageType { get; set; }
        public Delegate Handler { get; set; }

        public void Dispose()
        {
            if (Handler == null)
                return;

            try
            {
                UnlistenHandler(MessageType, Handler);
            }
            finally
            {
                MessageType = null;
                Handler = null;
            }
        }
    }

    [Flags]
    private enum HandlerFlags
    {
        None = 0,
        OnlyIfActiveAndEnabled = 1 << 0,
        Once = 1 << 1,
        DontInvokeIfAddedInAHandler = 1 << 2,
        IsUnityObject = 1 << 3,
    }

    private sealed class EventBetterWorker : MonoBehaviour
    {
        private int instanceId;

        void Awake()
        {
            instanceId = this.GetInstanceID();
        }

        private void LateUpdate()
        {
            Debug.Assert(instanceId == s_worker.instanceId);
            EventBetter.RemoveUnusedHandlers();
        }
    }

    private class EventEntry
    {
        public uint invocationCount = 0;
        public bool needsCleanup = false;
        public readonly List<object> listeners = new List<object>();
        public readonly List<Delegate> handlers = new List<Delegate>();
        public readonly List<HandlerFlags> flags = new List<HandlerFlags>();

        public int Count
        {
            get { return listeners.Count; }
        }

        public bool HasFlag(int i, HandlerFlags flag)
        {
            return (flags[i] & flag) == flag;
        }

        public void SetFlag(int i, HandlerFlags flag, bool value)
        {
            if (value)
            {
                flags[i] |= flag;
            }
            else
            {
                flags[i] &= ~flag;
            }
        }

        public void Add(object listener, Delegate handler, HandlerFlags flag)
        {
            UnityEngine.Debug.Assert(listeners.Count == handlers.Count);

            // if not in a handler, don't set this flag as it would ignore first
            // nested handler
            if (invocationCount == 0)
                flag &= ~HandlerFlags.DontInvokeIfAddedInAHandler;

            listeners.Add(listener);
            handlers.Add(handler);
            flags.Add(flag);
        }

        public void NullifyAt(int i)
        {
            UnityEngine.Debug.Assert(listeners.Count == handlers.Count);
            listeners[i] = null;
            handlers[i] = null;
            flags[i] = HandlerFlags.None;
        }

        public void RemoveAt(int i)
        {
            UnityEngine.Debug.Assert(listeners.Count == handlers.Count);
            listeners.RemoveAt(i);
            handlers.RemoveAt(i);
            flags.RemoveAt(i);
        }
    }

    /// <summary>
    /// For lookups.
    /// </summary>
    private static Dictionary<Type, EventEntry> s_entries = new Dictionary<Type, EventEntry>();
    /// <summary>
    /// For faster iteration.
    /// </summary>
    private static List<EventEntry> s_entriesList = new List<EventEntry>();
    /// <summary>
    /// For removing dead handlers.
    /// </summary>
    private static EventBetterWorker s_worker;


    private static bool RaiseInternal<T>(T message)
    {
        EventEntry entry;

        if (!s_entries.TryGetValue(typeof(T), out entry))
            return false;

        bool hadActiveHandlers = false;

        var invocationCount = ++entry.invocationCount;

        try
        {
            int initialCount = entry.Count;

            for (int i = 0; i < entry.Count; ++i)
            {
                var listener = GetAliveTarget(entry.listeners[i]);

                bool removeHandler = true;

                if (listener != null)
                {
                    if (entry.HasFlag(i, HandlerFlags.OnlyIfActiveAndEnabled))
                    {
                        var behaviour = listener as UnityEngine.Behaviour;
                        if (!ReferenceEquals(behaviour, null))
                        {
                            if (!behaviour.isActiveAndEnabled)
                                continue;
                        }

                        var go = listener as GameObject;
                        if (!ReferenceEquals(go, null))
                        {
                            if (!go.activeInHierarchy)
                                continue;
                        }
                    }

                    if (i >= initialCount)
                    {
                        // this is a new handler; if it has a protection flag, don't call it
                        if (entry.HasFlag(i, HandlerFlags.DontInvokeIfAddedInAHandler))
                        {
                            entry.SetFlag(i, HandlerFlags.DontInvokeIfAddedInAHandler, false);
                            continue;
                        }
                    }

                    if (!entry.HasFlag(i, HandlerFlags.Once))
                    {
                        removeHandler = false;
                    }

                    ((Action<T>)entry.handlers[i])(message);

                    hadActiveHandlers = true;
                }

                if (removeHandler)
                {
                    if (invocationCount == 1)
                    {
                        // it's OK to compact now
                        entry.RemoveAt(i);
                        --i;
                        --initialCount;
                    }
                    else
                    {
                        // need to wait
                        entry.needsCleanup = true;
                        entry.NullifyAt(i);
                    }
                }
            }
        }
        finally
        {
            UnityEngine.Debug.Assert(invocationCount == entry.invocationCount);
            --entry.invocationCount;

            if (invocationCount == 1 && entry.needsCleanup)
            {
                entry.needsCleanup = false;
                RemoveUnusedHandlers(entry);
            }
        }

        return hadActiveHandlers;
    }

    private static Delegate RegisterInternal<ListenerType, MessageType>(ListenerType listener, System.Action<MessageType> handler, HandlerFlags flags)
    {
        return RegisterInternal<MessageType>(listener, handler, flags);
    }

    private static Delegate RegisterInternal<T>(object listener, Action<T> handler, HandlerFlags flags)
    {
        if (listener == null)
            throw new ArgumentNullException("listener");
        if (handler == null)
            throw new ArgumentNullException("handler");

        if ((flags & HandlerFlags.IsUnityObject) == HandlerFlags.IsUnityObject)
        {
            Debug.Assert(listener is UnityEngine.Object);
            EnsureWorkerExistsAndIsActive();
        }

        EventEntry entry;
        if (!s_entries.TryGetValue(typeof(T), out entry))
        {
            entry = new EventEntry();
            s_entries.Add(typeof(T), entry);
            s_entriesList.Add(entry);
        }

        entry.Add(listener, handler, flags);

        return handler;
    }

    private static bool UnlistenHandler(Type messageType, Delegate handler)
    {
        return EventBetter.UnregisterInternal(messageType, handler, (eventEntry, index, _handler) => eventEntry.handlers[index] == _handler);
    }

    private static bool UnregisterInternal<ParamType>(Type messageType, ParamType param, Func<EventEntry, int, ParamType, bool> predicate)
    {
        EventEntry entry;
        if (!s_entries.TryGetValue(messageType, out entry))
        {
            return false;
        }

        return UnregisterInternal(entry, param, predicate);
    }

    private static bool UnregisterInternal<ParamType>(EventEntry entry, ParamType param, Func<EventEntry, int, ParamType, bool> predicate)
    {
        bool found = false;

        for (int i = 0; i < entry.Count; ++i)
        {
            if (entry.listeners[i] == null)
                continue;

            if (predicate != null && !predicate(entry, i, param))
                continue;

            found = true;
            if (entry.invocationCount == 0)
            {
                // it's ok to compact now
                entry.RemoveAt(i);
                --i;
            }
            else
            {
                // need to wait
                entry.needsCleanup = true;
                entry.NullifyAt(i);
            }
        }

        return found;
    }
    
    private static object GetAliveTarget(object target)
    {
        if (target == null)
            return null;

        var targetAsUnityObject = target as UnityEngine.Object;
        if (object.ReferenceEquals(targetAsUnityObject, null))
            return target;

        if (targetAsUnityObject)
            return target;

        return null;
    }

    private static void RemoveUnusedHandlers(EventEntry entry)
    {
        for (int i = 0; i < entry.Count; ++i)
        {
            var listener = entry.listeners[i];
            if (entry.HasFlag(i, HandlerFlags.IsUnityObject))
            {
                if ((UnityEngine.Object)listener != null)
                    continue;
            }
            else
            {
                if (listener != null)
                    continue;
            }

            if (entry.invocationCount == 0)
                entry.RemoveAt(i--);
            else
                entry.NullifyAt(i);
        }
    }

    private static void EnsureWorkerExistsAndIsActive()
    {
        if (s_worker)
        {
            if (!s_worker.isActiveAndEnabled)
                throw new InvalidOperationException("EventBetterWorker is disabled");

            return;
        }

        var go = new GameObject("EventBetterWorker", typeof(EventBetterWorker));
        go.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(go);

        s_worker = go.GetComponent<EventBetterWorker>();
        if (!s_worker)
            throw new InvalidOperationException("Unable to create EventBetterWorker");
    }

    #endregion
}
