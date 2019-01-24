using UnityEngine;

class SimpleConsumer : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (TextMessage msg) => Debug.Log(msg.text, this));
    }
}