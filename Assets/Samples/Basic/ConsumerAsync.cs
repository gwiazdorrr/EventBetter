using UnityEngine;

class ConsumerAsync : MonoBehaviour
{
    async void Awake()
    {
        var msg = await EventBetter.ListenAsync<TextMessage>();
        Debug.Log(msg.text, this);
    }
}