using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollisionUIVisualiser : MonoBehaviour
{
    public GameObject CollisionPrefab;

    private void Awake()
    {
        EventBetter.Listen(this, (CollisionMessage msg) => DrawCollision(msg.reporter, msg.collision));
    }

    private void DrawCollision(Collider reporter, Collision collision)
    {
        var vis = GameObject.Instantiate(CollisionPrefab, transform);
        var camera = Camera.main;

        var screenPoint = camera.WorldToScreenPoint(collision.contacts[0].point);
        vis.transform.position = screenPoint;
        vis.GetComponent<Text>().text = reporter.name + " with " + collision.gameObject.name;
    }

}
