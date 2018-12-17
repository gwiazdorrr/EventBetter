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
    /// Register a message handler. Doesn't store strong reference to the <paramref name="host"/>, makes sure <paramref name="handler"/>
    /// doesn't store any strong references, making it effectively leak-free.
    /// 
    /// The Target of <paramref name="handler"/> may contain value types, strings and a reference to the <paramref name="host"/>, 
    /// either explicit or implicit.
    /// 
    /// The <paramref name="handler"/> will be invoked every time a message of type <typeparamref name="MessageType"/> is raised,
    /// unless <paramref name="host"/> gets destroyed or any Unregister method is called.
    ///  
    /// Behind the scenes, <paramref name="host"/> is stored as a weak reference. If <paramref name="handler"/> contains <paramref name="host"/>
    /// reference, this reference is removed. 
    /// </summary>
    /// <typeparam name="HostType"></typeparam>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="host"></param>
    /// <param name="handler"></param>
    /// <param name="onlyOnce">After the <paramref name="handler"/> is invoked - unlisten automatically.</param>
    /// <param name="onlyIfActiveAndEnabled">If <paramref name="host"/> is a Behaviour or GameObject, will only invoke <paramref name="handler"/>
    /// if <paramref name="host"/> is ative and enabled.</param>
    /// <exception cref="System.InvalidOperationException">Thrown if the handler as any class references other than the one to the <paramref name="host"/></exception>
    public static void Listen<HostType, MessageType>(HostType host, System.Action<MessageType> handler,
        bool onlyOnce = false,
        bool onlyIfActiveAndEnabled = false)
        where HostType : UnityEngine.Object
    {
        HandlerFlags flags = HandlerFlags.None;

        if (onlyOnce)
            flags |= HandlerFlags.Once;
        if (onlyIfActiveAndEnabled)
            flags |= HandlerFlags.OnlyIfActiveAndEnabled;

        RegisterWeakifiedHandler(host, handler, flags);
    }

    /// <summary>
    /// Register a message handler. No host, you unregister by calling <see cref="IDisposable.Dispose">Dispose</see> on returned object.
    /// Handler is not limited in what it is allowed to capture.
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static IDisposable ListenManual<MessageType>(System.Action<MessageType> handler)
    {
        // use the dict as a host here, it will ensure the handler is going to live forever
        var actualHandler = RegisterInternal<object, MessageType>(s_entries, (_dummy, msg) => handler(msg), HandlerFlags.None);
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
        return Raise(message, typeof(MessageType));
    }

    /// <summary>
    /// Unregisters all <typeparamref name="MessageType"/> handlers for a given host.
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <param name="host"></param>
    /// <returns>True if there were any handlers, false otherwise.</returns>
    public static bool Unlisten<MessageType>(UnityEngine.Object host)
    {
        if (host == null)
            throw new ArgumentNullException("host");

        return UnregisterInternal(typeof(MessageType), host, (eventEntry, index, referenceHost) => object.ReferenceEquals(eventEntry.hosts[index].Target, referenceHost));
    }

    /// <summary>
    /// Unregisters all message types for a given host.
    /// </summary>
    /// <param name="host"></param>
    /// <returns>True if there were any handlers, false otherwise.</returns>
    public static bool UnlistenAll(UnityEngine.Object host)
    {
        if (host == null)
            throw new ArgumentNullException("host");

        bool anyListeners = false;
        foreach (var entry in s_entriesList)
        {
            anyListeners |= UnregisterInternal(entry, host, (eventEntry, index, referenceHost) => object.ReferenceEquals(eventEntry.hosts[index].Target, referenceHost));
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
    }

    #region Coroutine Support

    /// <summary>
    /// Use this in coroutines. Yield will return when at least one event of type <typeparamref name="MessageType"/>
    /// has been raised. To get the messages
    /// </summary>
    /// <typeparam name="MessageType"></typeparam>
    /// <returns></returns>
    public static YieldListener<MessageType> ListenWait<MessageType>()
        where MessageType : class
    {
        return new YieldListener<MessageType>();
    }

    public class YieldListener<MessageType> : System.Collections.IEnumerator, IDisposable
    {
        private Delegate handler;
        public List<MessageType> Messages { get; private set; }

        internal YieldListener()
        {
            handler = EventBetter.RegisterInternal<YieldListener<MessageType>, MessageType>(
                this, (x, msg) => x.OnMessage(msg), HandlerFlags.DontInvokeIfAddedInAHandler);
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
            if (Messages != null)
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
            (_dummy, msg) => tcs.SetResult(msg), HandlerFlags.DontInvokeIfAddedInAHandler);

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
    }

    private class EventEntry
    {
        public uint invocationCount = 0;
        public bool needsCleanup = false;
        public readonly List<WeakReference> hosts = new List<WeakReference>();
        public readonly List<Delegate> handlers = new List<Delegate>();
        public readonly List<HandlerFlags> flags = new List<HandlerFlags>();

        public int Count
        {
            get { return hosts.Count; }
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

        public void Add(WeakReference host, Delegate handler, HandlerFlags flag)
        {
            UnityEngine.Debug.Assert(hosts.Count == handlers.Count);

            // if not in a handler, don't set this flag as it would ignore first
            // nested handler
            if (invocationCount == 0)
                flag &= ~HandlerFlags.DontInvokeIfAddedInAHandler;

            hosts.Add(host);
            handlers.Add(handler);
            flags.Add(flag);
        }

        public void NullifyAt(int i)
        {
            UnityEngine.Debug.Assert(hosts.Count == handlers.Count);
            hosts[i] = null;
            handlers[i] = null;
            flags[i] = HandlerFlags.None;
        }

        public void RemoveAt(int i)
        {
            UnityEngine.Debug.Assert(hosts.Count == handlers.Count);
            hosts.RemoveAt(i);
            handlers.RemoveAt(i);
            flags.RemoveAt(i);
        }
    }

    /// <summary>
    /// For lookups.
    /// </summary>
    private static Dictionary<Type, EventEntry> s_entries = new Dictionary<Type, EventEntry>();
    /// <summary>
    /// For iterating without allocation.
    /// </summary>
    private static List<EventEntry> s_entriesList = new List<EventEntry>();
    /// <summary>
    /// To avoid allocs when raising.
    /// </summary>
    private static object[] s_args = new object[2];


    private static bool Raise(object message, Type messageType)
    {
        EventEntry entry;

        if (!s_entries.TryGetValue(messageType, out entry))
            return false;

        bool hadActiveHandlers = false;

        var invocationCount = ++entry.invocationCount;
        var args = s_args;

        try
        {
            int initialCount = entry.Count;

            for (int i = 0; i < entry.Count; ++i)
            {
                var host = GetAliveTarget(entry.hosts[i]);

                bool removeHandler = true;

                if (host != null)
                {
                    if (entry.HasFlag(i, HandlerFlags.OnlyIfActiveAndEnabled))
                    {
                        var behaviour = host as UnityEngine.Behaviour;
                        if (!ReferenceEquals(behaviour, null))
                        {
                            if (!behaviour.isActiveAndEnabled)
                                continue;
                        }

                        var go = host as GameObject;
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

                    try
                    {
                        // This prevents the code from allocating anything, making it effectively single-threaded - that's fine,
                        // since it revolves around UnityEngine.Objects, which are inherently single-threaded.
                        // Also, this *seems* to be safe, as DynamicInvoke eventually calls MethodBase.CheckArguments,
                        // and it copies the array
                        // https://github.com/Microsoft/referencesource/blob/60a4f8b853f60a424e36c7bf60f9b5b5f1973ed1/mscorlib/system/reflection/methodbase.cs#L338
                        args[0] = host;
                        args[1] = message;
                        entry.handlers[i].DynamicInvoke(args);
                    }
                    finally
                    {
                        args[0] = args[1] = null;
                    }

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

            if (invocationCount == 1 && entry.needsCleanup)
            {
                CleanUpEntry(entry);
            }
        }
        finally
        {
            UnityEngine.Debug.Assert(invocationCount == entry.invocationCount);
            --entry.invocationCount;
        }

        return hadActiveHandlers;
    }

    private static Delegate RegisterInternal<HostType, MessageType>(HostType host, System.Action<HostType, MessageType> handler, HandlerFlags flags)
    {
        return RegisterInternal(typeof(MessageType), host, handler, flags);
    }

    private static Delegate RegisterInternal(Type messageType, object host, Delegate handler, HandlerFlags flags)
    {
        if (messageType == null)
            throw new ArgumentNullException("messageType");
        if (host == null)
            throw new ArgumentNullException("host");
        if (handler == null)
            throw new ArgumentNullException("handler");

        EventEntry entry;
        if (!s_entries.TryGetValue(messageType, out entry))
        {
            entry = new EventEntry();
            s_entries.Add(messageType, entry);
            s_entriesList.Add(entry);
        }

        entry.Add(new WeakReference(host), handler, flags);

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
            if (entry.hosts[i] == null)
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

    private static Delegate RegisterWeakifiedHandler<HostType, MessageType>(HostType host, System.Action<MessageType> handler, HandlerFlags flags = HandlerFlags.None)
        where HostType : class
    {
        if (host == null)
            throw new ArgumentNullException("host");
        if (handler == null)
            throw new ArgumentNullException("handler");

        if (handler.Target == null)
        {
            // perfect! no context!
            return RegisterInternal<HostType, MessageType>(host, (_dummy, msg) => handler(msg), flags);
        }
        else if (handler.Target == host)
        {
            // easy - fallback to the "old" ways of lazy events
            var actualHandler = (System.Action<HostType, MessageType>)System.Delegate.CreateDelegate(typeof(System.Action<HostType, MessageType>), null, handler.Method, true);

            // inner handler is a workaround for mono not being table to dynamicaly invoke open delegates
            return RegisterInternal<HostType, MessageType>(host, (_host, _handler) => actualHandler(_host, _handler), flags);
        }
        else
        {
            // ok, it gets complicated...
            var target = handler.Target;
            var targetType = target.GetType();
            var attributes = targetType.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true);
            if (attributes == null || attributes.Length == 0)
            {
                throw new System.InvalidOperationException("Does not work for non-compiler generated targets");
            }

            var fields = targetType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            FieldInfo thisField = null;

            foreach (var field in fields)
            {
                if (field.Name == "$this")
                {
                    if (thisField == null)
                        thisField = field;
                    else
                        throw new System.InvalidOperationException(string.Format("Field {0} is not safe to capture", thisField.Name));
                }
                else if (!IsSafeToImplicitlyCapture(field.FieldType))
                {
                    // if this is the only "unsafe" let's mark it as a this, for now
                    if (thisField == null)
                        thisField = field;
                    else
                        throw new System.InvalidOperationException(string.Format("Field {0} is not safe to capture", field.Name));
                }
            }

            if (thisField == null)
            {
                // all good, all fields are "safe"
                return RegisterInternal<HostType, MessageType>(host, (_dummy, msg) => handler(msg), flags);
            }
            else
            {
                if (!typeof(HostType).IsSubclassOf(thisField.FieldType) && typeof(HostType) != thisField.FieldType)
                {
                    // captured something completely unexpected, not supported!
                    throw new System.InvalidOperationException(string.Format("Incompatible {0} type: {1} vs {2}", thisField.Name, typeof(HostType), thisField.FieldType));
                }
                else
                {

                    var thisFieldValue = thisField.GetValue(target);
                    // the null check is here in case we have already nullified for this target (happens if
                    // multiple event better registrations happen is same scope)
                    if (thisFieldValue != null && thisFieldValue != host)
                    {
                        // why is this some other host?
                        throw new System.InvalidOperationException(string.Format("Incomatible $this value: {0} vs {1}", host, thisFieldValue));
                    }

                    // this gets fun... since target is some sort of compiler generated stuff, but has safe fields,
                    // EXCEPT for $this, let's nullify that field and set/unset in a handler wrapper
                    thisField.SetValue(target, null);
                    return RegisterInternal<HostType, MessageType>(host, (x, msg) =>
                    {
                        var prevValue = thisField.GetValue(target);
                        thisField.SetValue(target, x);
                        try
                        {
                            handler(msg);
                        }
                        finally
                        {
                            // can't just set it back to null since there may be nested events going on...
                            // if nested event registers it can mess this up (the outer SetValue is the culprit)
                            // Debug.Assert(thisField.GetValue(target) == x);
                            thisField.SetValue(target, prevValue);
                        }
                    }, flags);
                }
            }
        }

    }

    private static bool IsSafeToImplicitlyCapture(System.Type type, HashSet<Type> knownValueTypes = null)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            return true;

        if (!type.IsValueType)
        {
            // TODO: add some attribute here because maybe some types will be suitable
            return false;
        }

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            if (!IsSafeToImplicitlyCapture(f.FieldType, knownValueTypes))
                return false;
        }

        return true;
    }

    private static object GetAliveTarget(WeakReference reference)
    {
        if (reference == null)
            return null;

        var target = reference.Target;
        if (target == null)
            return null;

        var targetAsUnityObject = target as UnityEngine.Object;
        if (object.ReferenceEquals(targetAsUnityObject, null))
            return target;

        if (targetAsUnityObject)
            return target;

        return null;
    }

    private static void CleanUpEntry(EventEntry entry)
    {
        for (int i = 0; i < entry.Count; ++i)
        {
            if (GetAliveTarget(entry.hosts[i]) != null)
                continue;

            if (entry.invocationCount == 0)
                entry.RemoveAt(i--);
            else
                entry.NullifyAt(i);
        }
    }

    #endregion
}