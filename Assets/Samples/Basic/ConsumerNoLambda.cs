using UnityEngine;

class ConsumerNoLambda : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen<ConsumerNoLambda, PrintMessage>(this, HandlePrintMessage, once: true);
    }

    void HandlePrintMessage(PrintMessage msg)
    {
        Debug.Log(msg.text, this);
    }
}