﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NoAllocProducer : MonoBehaviour
{
    private TextMessage message = new TextMessage()
    {
        text = "NoAlloc"
    };

    private void Update()
    {
        EventBetter.Raise(message);
    }
}
