using UnityEngine;

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