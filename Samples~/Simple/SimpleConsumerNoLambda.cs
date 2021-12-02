using UnityEngine;

class SimpleConsumerNoLambda : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen<SimpleConsumerNoLambda, TextMessage>(this, HandlePrintMessage, once: true);
    }

    void HandlePrintMessage(TextMessage msg)
    {
        Debug.Log(msg.text, this);
    }
}