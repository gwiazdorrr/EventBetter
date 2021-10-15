using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollisionLogger : MonoBehaviour
{
    private void Awake()
    {
        EventBetter.Listen(this, (CollisionMessage msg) => LogCollision(msg.reporter, msg.collision));
    }

    private void LogCollision(Collider reporter, Collision collision)
    {
        Debug.Log(reporter.name + " with " + collision.gameObject.name);
    }

}
