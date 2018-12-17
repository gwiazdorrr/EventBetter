# EventBetter
A Unity messaging/event system for the lazy. No interfaces to implement, no base types to derive from, no initialization, no message codes, no OnEnable/OnDisable shenanigans, no hanging references, one source file.

# TL;DR:
Copy [EventBetter.cs](Assets/Plugins/EventBetter/EventBetter.cs) to your project. The API you need to know is `EventBetter.Listen` and `EventBetter.Raise`. Done! Example:

```
class PrintMessage { public string text; }

class Producer : MonoBehaviour
{
    void Start()
    {
        EventBetter.Raise(new PrintMessage() { text = "Hello World!" });
    }
}

class Consumer : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (PrintMessage msg) => Debug.Log(msg.text, this), onlyOnce: true);
    }
}
```

# More examples

Maybe you like async/await?
```
class AsyncConsumer : MonoBehaviour
{	
	async void Awake()
    {
        var msg = await EventBetter.ListenAsync<PrintMessage>();
        Debug.Log(msg.text, this);
    }
}
```

Or maybe you'd rather stick with good old coroutines?
```
class ConsumerCoro : MonoBehaviour
{
    IEnumerator Start()
    {
        var listener = EventBetter.ListenWait<PrintMessage>();
        yield return listener;
        Debug.Log(listener.First.text, this);
    }
}
```