using System.Collections;
using UnityEngine;

class SimpleConsumerCoro : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(Coro());
    }

    IEnumerator Coro()
    {
        var listener = EventBetter.ListenWait<TextMessage>();
        yield return listener;
        Debug.Log(listener.First.text, this);
    }
}