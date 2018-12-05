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
        EventBetter.Listen(this, (PrintMessage msg) => Debug.Log(msg.text, this));
    }
}
```



