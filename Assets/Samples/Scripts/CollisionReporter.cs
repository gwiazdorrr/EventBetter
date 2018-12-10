using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CollisionReporter : MonoBehaviour
{
    public class CollisionMessage
    {
        public Collider reporter;
        public Collision collision;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"RAISING {Time.frameCount}");
        EventBetter.Raise(new CollisionMessage()
        {
            reporter = GetComponent<Collider>(),
            collision = collision,
        });
    }


}
