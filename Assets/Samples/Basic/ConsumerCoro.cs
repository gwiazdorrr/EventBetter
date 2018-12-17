using System.Collections;
using UnityEngine;

class ConsumerCoro : MonoBehaviour
{
    IEnumerator Start()
    {
        var listener = EventBetter.ListenWait<PrintMessage>();
        yield return listener;
        Debug.Log(listener.First.text, this);
    }
}