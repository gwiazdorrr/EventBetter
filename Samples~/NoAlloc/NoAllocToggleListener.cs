using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NoAllocToggleListener : MonoBehaviour
{
    private int counter;
    private Action<TextMessage> handler;

    private void Awake()
    {
        handler = (TextMessage msg) => ++counter;
    }

    private void Update()
    {
        EventBetter.Listen(this, handler);
        EventBetter.UnlistenAll(this);
    }
}
