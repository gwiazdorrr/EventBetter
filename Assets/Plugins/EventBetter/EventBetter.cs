using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

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
    /// <exception cref="System.InvalidOperationException">Thrown if the handler as any class references other than the one to the <paramref name="host"/></exception>
    public static void Register<HostType, MessageType>(this HostType host, System.Action<MessageType> handler)
        where HostType : UnityEngine.Object
    {
        RegisterDeleakifiedHandler(host, handler);
    }

    public static IDisposable RegisterManual<MessageType>(System.Action<MessageType> handler)
    {
        // use the dict as a host here, it will ensure the handler is going to live forever
        var actualHandler = RegisterInternal<object, MessageType>(s_entries, (_dummy, msg) => handler(msg));
        return new ManualHandlerDisposable()
        {
            Handler = actualHandler,
            MessageType = typeof(MessageType)
        };
    }

    public static bool Raise<MessageType>(MessageType message)
    {
        return Raise(message, typeof(MessageType));
    }

    public static bool Unregister<MessageType>(UnityEngine.Object host)
    {
        if (host == null)
            throw new ArgumentNullException("host");

        return UnregisterInternal(typeof(MessageType), host, (eventEntry, index, referenceHost) => object.ReferenceEquals(eventEntry.hosts[index].Target, referenceHost));
    }

    public static bool UnregisterAll(UnityEngine.Object host)
    {
        if (host == null)
            throw new ArgumentNullException("host");

        bool anyListeners = false;
        foreach (var kv in s_entries)
        {
            anyListeners |= UnregisterInternal(kv.Key, host, (eventEntry, index, referenceHost) => object.ReferenceEquals(eventEntry.hosts[index].Target, referenceHost));
        }

        return anyListeners;
    }

    public static void Clear()
    {
        s_entries.Clear();
    }

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
                EventBetter.UnregisterInternal(MessageType, Handler, (eventEntry, index, handler) => eventEntry.handlers[index] == handler);
            }
            finally
            {
                MessageType = null;
                Handler = null;
            }
        }
    }

    private class EventEntry
    {
        public uint invocationCount = 0;
        public bool needsCleanup = false;
        public List<WeakReference> hosts = new List<WeakReference>();
        public List<Delegate> handlers = new List<Delegate>();
    }

    private static Dictionary<Type, EventEntry> s_entries = new Dictionary<Type, EventEntry>();
    private static object[] s_args = new object[2];


    private static bool Raise(object message, Type messageType)
    {
        EventEntry entry;

        if (!s_entries.TryGetValue(messageType, out entry))
            return false;

        bool hadActiveHandlers = false;

        var args = s_args;
        // do this instead in case of some weird bugs
        // var args = new object[2];

        try
        {
            ++entry.invocationCount;

            for (int i = 0; i < entry.hosts.Count; ++i)
            {
                var host = GetAliveTarget(entry.hosts[i]);

                if (host != null)
                {
                    var handler = entry.handlers[i];

                    // premature optimization - don't need to allocate anything!
                    // it depends, but this "might" lead to some interesting bugs in case of nested invokes
                    try
                    {
                        args[0] = host;
                        args[1] = message;
                        handler.DynamicInvoke(args);
                    }
                    finally
                    {
                        args[0] = args[1] = null;
                    }

                    hadActiveHandlers = true;
                }
                else if (entry.invocationCount == 1)
                {
                    // it's OK to compact now
                    entry.hosts.RemoveAt(i);
                    entry.handlers.RemoveAt(i);
                    --i;
                }
                else
                {
                    // need to wait
                    entry.needsCleanup = true;
                    entry.hosts[i] = null;
                    entry.handlers[i] = null;
                }
            }

            if ( entry.invocationCount == 1 && entry.needsCleanup )
            {
                CleanUpEntry(entry);
            }
        }
        finally
        {
            --entry.invocationCount;
        }

        return hadActiveHandlers;
    }

    private static Delegate RegisterInternal<HostType, MessageType>(HostType host, System.Action<HostType, MessageType> handler)
    {
        return RegisterInternal(typeof(MessageType), host, handler);
    }

    private static Delegate RegisterInternal(Type messageType, object host, Delegate handler)
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
        }

        entry.hosts.Add(new WeakReference(host));
        entry.handlers.Add(handler);

        return handler;
    }

    private static bool UnregisterInternal<ParamType>(Type messageType, ParamType param, Func<EventEntry, int, ParamType, bool> predicate)
    {
        EventEntry entry;
        if (!s_entries.TryGetValue(messageType, out entry))
        {
            return false;
        }

        bool found = false;

        for ( int i = 0; i < entry.handlers.Count; ++i )
        {
            if (entry.hosts[i] == null)
                continue;

            if (!predicate(entry, i, param))
                continue;

            found = true;
            if (entry.invocationCount == 0)
            {
                // it's ok to compact now
                entry.hosts.RemoveAt(i);
                entry.handlers.RemoveAt(i);
                --i;
            }
            else
            {
                // need to wait
                entry.needsCleanup = true;
                entry.hosts[i] = null;
                entry.handlers[i] = null;
            }
        }

        return found;
    }

    private static Delegate RegisterDeleakifiedHandler<HostType, MessageType>(HostType host, System.Action<MessageType> handler) where HostType : class
    {
        if (host == null)
            throw new ArgumentNullException("host");
        if (handler == null)
            throw new ArgumentNullException("handler");

        if (handler.Target == null)
        {
            // perfect! no context!
            return RegisterInternal<HostType, MessageType>(host, (_dummy, msg) => handler(msg));
        }
        else if (handler.Target == host)
        {
            // easy - fallback to the "old" ways of lazy events
            var actualHandler = (System.Action<HostType, MessageType>)System.Delegate.CreateDelegate(typeof(System.Action<HostType, MessageType>), null, handler.Method, true);

            // inner handler is a workaround for mono not being table to dynamicaly invoke open delegates
            return RegisterInternal<HostType, MessageType>(host, (_host, _handler) => actualHandler(_host, _handler));
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
                    if ( thisField == null )
                        thisField = field;
                    else
                        throw new System.InvalidOperationException($"Field {thisField.Name} is not safe to capture");
                }
                else if (!IsSafeToImplicitlyCapture(field.FieldType))
                {
                    // if this is the only "unsafe" let's mark it as a this, for now
                    if (thisField == null)
                        thisField = field;
                    else
                        throw new System.InvalidOperationException($"Field {field.Name} is not safe to capture");
                }
            }

            if (thisField == null)
            {
                // all good, all fields are "safe"
                return RegisterInternal<HostType, MessageType>(host, (_dummy, msg) => handler(msg));
            }
            else
            {
                if (!typeof(HostType).IsSubclassOf(thisField.FieldType) && typeof(HostType) != thisField.FieldType)
                {
                    // captured something completely unexpected, not supported!
                    throw new System.InvalidOperationException($"Incompatible {thisField.Name} type: {typeof(HostType)} vs {thisField.FieldType}");
                }
                else
                {

                    var thisFieldValue = thisField.GetValue(target);
                    // the null check is here in case we have already nullified for this target (happens if
                    // multiple event better registrations happen is same scope)
                    if (thisFieldValue != null && thisFieldValue != host)
                    {
                        // why is this some other host?
                        throw new System.InvalidOperationException($"Incomatible $this value: {host} vs {thisFieldValue}");
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
                    });
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
        for (int i = 0; i < entry.hosts.Count; ++i)
        {
            if (GetAliveTarget(entry.hosts[i]) != null)
                continue;

            entry.hosts.RemoveAt(i);
            entry.handlers.RemoveAt(i);
        }
    }
}