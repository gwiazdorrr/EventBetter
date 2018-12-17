using UnityEngine;

class Producer : MonoBehaviour
{
    void Update()
    {
        EventBetter.Raise(new PrintMessage() { text = "Hello World!" });
    }
}

class PrintMessage { public string text; }