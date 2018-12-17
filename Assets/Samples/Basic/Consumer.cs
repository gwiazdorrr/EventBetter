using UnityEngine;

class Consumer : MonoBehaviour
{
    void Awake()
    {
        EventBetter.Listen(this, (PrintMessage msg) => Debug.Log(msg.text, this), onlyOnce: true);
    }
}