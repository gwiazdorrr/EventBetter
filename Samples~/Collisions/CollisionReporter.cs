using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class CollisionReporter : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        EventBetter.Raise(new CollisionMessage()
        {
            reporter = GetComponent<Collider>(),
            collision = collision,
        });
    }
}

public class CollisionMessage
{
    public Collider reporter;
    public Collision collision;
}