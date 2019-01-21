using UnityEngine;

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