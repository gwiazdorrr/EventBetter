# EventBetter
A Unity pubsub/messaging/event system for the lazy. 
- raising events is alloc-free
- one source file (everything else here is just for the test environment)
- no interfaces to implement and no base types to derive from
- no initialization needed
- no cleanup needed
- no memory leaks
- no message codes
- no OnEnable/OnDisable shenanigans
- no casting needed
- lightweight
- async/coroutine friendly

# TL;DR:
Copy [EventBetter.cs](Assets/Plugins/EventBetter/EventBetter.cs) anywhere to your project. The API you need to know is `EventBetter.Listen` and `EventBetter.Raise`. Done! Example:

```cs
class TextMessage
{
    public string text;
}

class SimpleProducer : MonoBehaviour
{
    void Update()
    {
        EventBetter.Raise(new TextMessage() { text = "Hello World!" });
    }
}

class SimpleConsumer : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this));
    }
}
```

There's no need to unlisten/unsubsribe from anything.

# How does it work

The first parameter in `Listen` is the listener; as long as it is alive, the handler (the second parameter) will be invoked whenever there is a `Raise` called with a matching type. If the listener gets destroyed (with Destroy or when changing scenes), the handler will not get invoked anymore and all the references will get cleaned up no later than in the next LateUpdate.

It is possible thanks to UnityEngine.Object being the main citizen in the Unity world - it has a native representation with lifetime controlled entirely by the engine. There's no need to use WeakReference, ConditionalWeakTable and boilerplate like "unsubsribe" to avoid leaks, just keep track of the native parts.

# More examples

Maybe you like async/await more?

```cs
class SimpleConsumerAsync : MonoBehaviour
{
    async void Awake()
    {
        var msg = await EventBetter.ListenAsync<TextMessage>();
        Debug.Log(msg.text, this);
    }
}
```

Or maybe you'd rather stick with good old coroutines?
```cs
class SimpleConsumerCoro : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(Coro());
    }

    IEnumerator Coro()
    {
        var listener = EventBetter.ListenWait<TextMessage>();
        yield return listener;
        Debug.Log(listener.First.text, this);
    }
}
```

Back to the basic Listen, maybe you want to stop listening after the first message arrives?
```cs
EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this), once: true);
```

Or listen only if the listening script is active and enabled?
```cs
EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this), exculdeInactive: true);
```

If you are not in a MonoBehaviour and still want to use EventBetter, use:
```cs
IDisposable listener = EventBetter.ListenManual( (TextMessage msg) => Debug.Log(msg.text, this) );
// ...
listener.Dispose();
``` 
