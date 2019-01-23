# EventBetter
A Unity pubsub/messaging/event system for the lazy. No interfaces to implement, no base types to derive from, no initialization, no message codes, no OnEnable/OnDisable shenanigans, no memory leaks, no casting, liteweight, easy to extend, .NET3.5 compatible, one source file (everything else here is just test environment).

# TL;DR:
Copy [EventBetter.cs](Assets/Plugins/EventBetter/EventBetter.cs) anywhere to your project. The API you need to know is `EventBetter.Listen` and `EventBetter.Raise`. Done! Example:

```
class TextMessage
{
    public string text;
}

class Producer : MonoBehaviour
{
    void Update()
    {
        EventBetter.Raise(new TextMessage() { text = "Hello World!" });
    }
}

class ConsumerSimple : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this));
    }
}
```

# More examples

Maybe you like async/await more?

```
class ConsumerAsync : MonoBehaviour
{
    async void Awake()
    {
        var msg = await EventBetter.ListenAsync<TextMessage>();
        Debug.Log(msg.text, this);
    }
}
```

Or maybe you'd rather stick with good old coroutines?
```
class ConsumerCoro : MonoBehaviour
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
```
EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this), once: true);
```

Or listen only if the listening script is active and enabled?
```
EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this), exculdeInactive: true);
```

If you are not in a MonoBehaviour and still want to use EventBetter, use:
```
IDisposable listener = EventBetter.ListenManual( (TextMessage msg) => Debug.Log(msg.text, this) );
// ...
listener.Dispose();
```
