using System.Collections;
using UnityEngine;

class ConsumerCoro : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(Coro());
    }
    
    private IEnumerator Coro()
    {
        var listener = EventBetter.ListenWait<PrintMessage>();
        yield return listener;
        Debug.Log(listener.First.text, this);
    }
}