using UnityEngine;

class ConsumerSimple : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this));
    }
}