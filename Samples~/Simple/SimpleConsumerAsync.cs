using UnityEngine;

class SimpleConsumerAsync : MonoBehaviour
{
#if NET_4_6
    async void Awake()
    {
        var msg = await EventBetter.ListenAsync<TextMessage>();
        Debug.Log(msg.text, this);
    }
#endif
}