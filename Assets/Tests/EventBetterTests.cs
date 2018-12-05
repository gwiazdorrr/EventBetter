using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class EventBetterTests
{
    public class TestMessage { }
    public class TestMessage2 { }
    public class TestMessage3 { }

    public class TestBehaviour : MonoBehaviour
    {
        private int expectedInstanceId;

        private struct SomeStruct
        {
            public int Value { get; set; }
        }

        private class SomeClass
        {
            public int Value { get; set; }

            public override bool Equals(object obj)
            {
                var otherValue = (obj as SomeClass)?.Value;
                return otherValue.HasValue && otherValue.Value == Value;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        public void TestIfActiveAndEnabled()
        {
            int count = 0;

            EventBetter.Listen(this, (TestMessage o) => ++count);
            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(1, count);

            enabled = false;
            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(2, count);
            enabled = true;

            gameObject.SetActive(false);
            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(3, count);
            gameObject.SetActive(true);

            Assert.IsTrue(EventBetter.Unlisten<TestMessage>(this));
            Assert.IsFalse(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(3, count);


            EventBetter.ListenIfActiveAndEnabled(this, (TestMessage o) => ++count);
            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(4, count);

            enabled = false;
            Assert.IsFalse(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(4, count);
            enabled = true;

            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(5, count);

            gameObject.SetActive(false);
            Assert.IsFalse(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(5, count);
            gameObject.SetActive(true);

            Assert.IsTrue(EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(6, count);
        }

        public void TestMutableLambda()
        {
            {
                int startValue = 100;
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue++, 100));
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue++, 101));
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue++, 102));
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue++, 103));
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue++, 104));
            }
            {
                int startValue = 200;
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue, 210));
                startValue += 10;
                EventBetter.Listen(this, (TestMessage o) => InstanceHandle(startValue, 210));
            }
        }

        public void TestSelf()
        {
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
        }

        public void TestSelfAdv()
        {
            var date = System.DateTime.Now.Date;
            var str = "TestString";
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(date, System.DateTime.Now.Date));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(str, "TestString"));
        }

        public void TestSelfStatic()
        {
            EventBetter.Listen(this, (TestMessage o) => StaticHandle(null, null));
        }

        public void TestStruct()
        {
            var someStruct = new SomeStruct() { Value = 123 };
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(someStruct, new SomeStruct() { Value = 123 }));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(someStruct.Value, 123));
        }

        public void TestStructStatic()
        {
            var someStruct = new SomeStruct() { Value = 123 };
            EventBetter.Listen(this, (TestMessage o) => StaticHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => StaticHandle(someStruct, new SomeStruct() { Value = 123 }));
            EventBetter.Listen(this, (TestMessage o) => StaticHandle(someStruct.Value, 123));
        }

        public void TestClass()
        {
            var someClass = new SomeClass() { Value = 456 };

            // this captured implicitly
            Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null)));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(this, (TestMessage o) => InstanceHandle(someClass, new SomeClass() { Value = 456 })));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(this, (TestMessage o) => InstanceHandle(someClass.Value, 456)));
        }

        public void TestClassStatic()
        {
            var someClass = new SomeClass() { Value = 456 };
            EventBetter.Listen(this, (TestMessage o) => StaticHandle(null, null));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(this, (TestMessage o) => StaticHandle(someClass, new SomeClass() { Value = 456 })));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(this, (TestMessage o) => StaticHandle(someClass.Value, 456)));
        }

        public void TestSomeOtherHost()
        {
            {
                var go = new GameObject("blah");
                go.transform.SetParent(transform);
                EventBetter.Listen(go, (TestMessage o) => StaticHandle(go.name, "blah"));
            }

            {
                var go = new GameObject("blah2");
                go.transform.SetParent(transform);
                Assert.Throws<InvalidOperationException>(() => EventBetter.Listen(go, (TestMessage o) => InstanceHandle(go.name, "blah2")));
            }
        }

        public void TestUnregister()
        {
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new TestMessage()));

            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            Assert.AreEqual(true, EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(true, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new TestMessage()));

            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            Assert.AreEqual(true, EventBetter.Raise(new TestMessage()));
            Assert.AreEqual(true, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new TestMessage()));

            EventBetter.Listen(this, (TestMessage o) => InstanceHandle(null, null));
            Assert.AreEqual(true, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Unlisten<TestMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new TestMessage()));
        }

        public void TestNestedRegisterSimple()
        {
            int totalInvocations = 0;

            EventBetter.Listen(this, (TestMessage o) =>
            {
                InstanceHandle(null, null);
                ++totalInvocations;

                EventBetter.Listen(this, (TestMessage oo) =>
                {
                    InstanceHandle(null, null);
                    ++totalInvocations;
                });
            });

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(2, totalInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(5, totalInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(9, totalInvocations);
        }

        List<System.IDisposable> manualHandlers = new List<System.IDisposable>();
        public void TestNestedRegisterSimpleManual()
        {
            int totalInvocations = 0;

            manualHandlers.Add(EventBetter.ListenManual((TestMessage o) =>
            {
                InstanceHandle(null, null);
                ++totalInvocations;

                manualHandlers.Add(EventBetter.ListenManual((TestMessage oo) =>
                {
                    InstanceHandle(null, null);
                    ++totalInvocations;
                }));
            }));

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(2, totalInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(5, totalInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(9, totalInvocations);

            foreach (var handler in manualHandlers)
                handler.Dispose();
        }

        public void TestNestedRaiseSimple()
        {
            int nestedCalls = 0;
            int totalInvocations = 0;

            EventBetter.Listen(this, (TestMessage o) =>
            {
                ++totalInvocations;
                InstanceHandle(null, null);
                try
                {
                    ++nestedCalls;
                    if (nestedCalls < 10)
                    {
                        EventBetter.Raise(new TestMessage());
                    }
                }
                finally
                {
                    --nestedCalls;
                }
            });

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(10, totalInvocations);
        }

        public void TestNestedRaiseSimpleManual()
        {
            int nestedCalls = 0;
            int totalInvocations = 0;

            var handler = EventBetter.ListenManual((TestMessage o) =>
            {
                ++totalInvocations;
                InstanceHandle(null, null);
                try
                {
                    ++nestedCalls;
                    if (nestedCalls < 10)
                    {
                        EventBetter.Raise(new TestMessage());
                    }
                }
                finally
                {
                    --nestedCalls;
                }
            });

            try
            {
                EventBetter.Raise(new TestMessage());
                Assert.AreEqual(10, totalInvocations);
            }
            finally
            {
                handler.Dispose();
            }
        }

        public void TestNestedMessedUp()
        {
            int nestedCalls = 0;
            int totalInvocations = 0;

            int dummyMessage2Invocations = 0;
            EventBetter.Listen(this, (TestMessage2 o) =>
            {
                InstanceHandle(null, null);
                ++dummyMessage2Invocations;

                EventBetter.Unlisten<TestMessage>(this);
            });

            int dummyMessage3Invocations = 0;
            EventBetter.Listen(this, (TestMessage3 o) =>
            {
                InstanceHandle(null, null);
                ++dummyMessage3Invocations;

                EventBetter.Unlisten<TestMessage3>(this);
                EventBetter.Raise(new TestMessage());
            });

            EventBetter.Listen(this, (TestMessage o) =>
            {
                InstanceHandle(null, null);
                ++totalInvocations;
                ++nestedCalls;
                if (nestedCalls >= 10)
                {
                    EventBetter.Raise(new TestMessage2());
                    EventBetter.Raise(new TestMessage3());

                    EventBetter.Raise(new TestMessage2());
                    EventBetter.Raise(new TestMessage3());

                    EventBetter.Raise(new TestMessage2());
                    EventBetter.Raise(new TestMessage3());
                }

                EventBetter.Raise(new TestMessage());
            });

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(10, totalInvocations);
            Assert.AreEqual(3, dummyMessage2Invocations);
            Assert.AreEqual(1, dummyMessage3Invocations);
        }

        int nestedInvocations = 0;
        public void TestNestedRaiseContexts()
        {
            int totalInvocations = 0;
            nestedInvocations = 0;

            EventBetter.Listen(this, (TestMessage o) =>
            {
                InstanceHandle(null, null);
                ++totalInvocations;
                NestedDifferentContext(totalInvocations);
            });

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(2, totalInvocations + nestedInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(5, totalInvocations + nestedInvocations);

            EventBetter.Raise(new TestMessage());
            Assert.AreEqual(9, totalInvocations + nestedInvocations);
        }

        public void NestedDifferentContext(int baseValue)
        {
            EventBetter.Listen(this, (TestMessage o) =>
            {
                InstanceHandle(null, null);
                ++nestedInvocations;
            });
        }
            

        private void Awake()
        {
            expectedInstanceId = GetInstanceID();
        }

        private void InstanceHandle(object param, object expectedValue)
        {
            Assert.AreEqual(expectedInstanceId, GetInstanceID());
            StaticHandle(param, expectedValue);
        }

        private static void StaticHandle(object param, object expectedValue)
        {
            Assert.AreEqual(expectedValue, param);
        }
    }

    private void SimpleTest(System.Action<TestBehaviour> doStuff, bool expectedResult = true)
    {
        var go = new GameObject("Test", typeof(TestBehaviour));

        try
        {
            doStuff(go.GetComponent<TestBehaviour>());
        }
        catch
        {
            Object.DestroyImmediate(go);
            throw;
        }

        Assert.AreEqual(expectedResult, EventBetter.Raise(new TestMessage()));

        UnityEngine.Object.DestroyImmediate(go);
        Assert.IsFalse(EventBetter.Raise(new TestMessage()));
    }

    [SetUp]
    public void SetUp()
    {
        EventBetter.Clear();
    }

    [Test] public void RegisterManual()
    {
        int someValue = 666;

        var disp1 = EventBetter.ListenManual((TestMessage msg) => someValue++);
        var disp2 = EventBetter.ListenManual((TestMessage msg) => someValue++);

        Assert.AreEqual(true, EventBetter.Raise(new TestMessage()));
        Assert.AreEqual(668, someValue);

        disp1.Dispose();
        Assert.AreEqual(true, EventBetter.Raise(new TestMessage()));
        Assert.AreEqual(669, someValue);

        // whether double dispose breaks anything
        disp1.Dispose();
        Assert.AreEqual(true, EventBetter.Raise(new TestMessage()));
        Assert.AreEqual(670, someValue);

        disp2.Dispose();
        Assert.AreEqual(false, EventBetter.Raise(new TestMessage()));
        Assert.AreEqual(670, someValue);
    }

    [Test] public void IfActiveAndEnabled() => SimpleTest(t => t.TestIfActiveAndEnabled());

    [Test] public void Self() => SimpleTest(t => t.TestSelf());
    [Test] public void SelfAdv() => SimpleTest(t => t.TestSelfAdv());
    [Test] public void MutableLambda() => SimpleTest(t => t.TestMutableLambda());
    [Test] public void SelfStatic() => SimpleTest(t => t.TestSelfStatic());
    [Test] public void CaptureStruct() => SimpleTest(t => t.TestStruct());
    [Test] public void CaptureStructStatic() => SimpleTest(t => t.TestStructStatic());
    [Test] public void SomeOtherHost() => SimpleTest(t => t.TestSomeOtherHost());
    [Test] public void Unregister() => SimpleTest(t => t.TestUnregister(), expectedResult: false);

    [Test] public void Destroy() => SimpleTest(t =>
    {
        t.TestSelf();
        Object.DestroyImmediate(t);
    }, expectedResult: false);

    [Test] public void NoCallbacks() => SimpleTest(t => { }, expectedResult: false);

    [Test] public void CaptureClass() => SimpleTest(t => t.TestClass(), expectedResult: false);
    [Test] public void CaptureClassStatic() => SimpleTest(t => t.TestClassStatic());


    [Test] public void NestedRegisterSimple() => SimpleTest(t => t.TestNestedRegisterSimple());
    [Test] public void NestedRegisterSimpleManual() => SimpleTest(t => t.TestNestedRegisterSimpleManual(), expectedResult: false);
    [Test] public void NestedRaiseSimple() => SimpleTest(t => t.TestNestedRaiseSimple());
    [Test] public void NestedRaiseContexts() => SimpleTest(t => t.TestNestedRaiseContexts());
    [Test] public void NestedRaiseSimpleManual() => SimpleTest(t => t.TestNestedRaiseSimpleManual(), expectedResult: false);
    [Test] public void NestedMessedUp() => SimpleTest(t => t.TestNestedMessedUp(), expectedResult: false);

    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityTest]
    public IEnumerator EventBetterTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // yield to skip a frame
        yield return null;
    }
}
