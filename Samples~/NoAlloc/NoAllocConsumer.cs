using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NoAllocConsumer : MonoBehaviour
{
    private int count = 0;

    private void Awake()
    {
        EventBetter.Listen(this, (TextMessage msg) => ++count);
    }

    private void OnDestroy()
    {
        Debug.Log("Messages received: " + count);
    }
}
