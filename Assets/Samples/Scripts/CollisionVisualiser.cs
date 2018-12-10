using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionVisualiser : MonoBehaviour
{
    //private void Awake()
    //{
    //    EventBetter.Listen(this, (CollisionReporter.CollisionMessage msg) => DrawCollision(msg.reporter, msg.collision));
    //}

    async void Start()
    {
        for (; ;)
        {
            var msg = await EventBetter.ListenAsync<CollisionReporter.CollisionMessage>();
            Debug.Log($"2 frame {Time.frameCount}");
        }
    }
}
