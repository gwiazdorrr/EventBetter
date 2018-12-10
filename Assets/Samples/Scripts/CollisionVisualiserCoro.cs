using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionVisualiserCoro : MonoBehaviour
{
    public class TestEnumerator : CustomYieldInstruction, IDisposable
    {
        public override bool keepWaiting
        {
            get
            {
                return false;
            }
        }

        public void Dispose()
        {
            
        }
    }

    IEnumerator Start()
    {
        //yield return new TestEnumerator(
        for (; ; )
        {
            using (var awaiter = EventBetter.ListenWait<CollisionReporter.CollisionMessage>())
            {
                yield return awaiter;
                foreach (var msg in awaiter.Messages)
                    Debug.Log($"1 frame {Time.frameCount}");
            }
        }
    }
}
