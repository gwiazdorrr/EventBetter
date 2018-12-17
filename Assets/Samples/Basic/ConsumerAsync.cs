using UnityEngine;

class ConsumerAsync : MonoBehaviour
{
    async void Awake()
    {
        var msg = await EventBetter.ListenAsync<PrintMessage>();
        Debug.Log(msg.text, this);
    }
}