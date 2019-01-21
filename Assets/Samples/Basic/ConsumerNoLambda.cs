using UnityEngine;

class ConsumerNoLambda : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen<ConsumerNoLambda, TextMessage>(this, HandlePrintMessage, once: true);
    }

    void HandlePrintMessage(TextMessage msg)
    {
        Debug.Log(msg.text, this);
    }
}