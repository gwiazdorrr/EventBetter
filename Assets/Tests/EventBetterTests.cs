using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class EventBetterTests
{
    public class DummyMessage { }
    public class DummyMessage2 { }
    public class DummyMessage3 { }

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

        public void TestMutableLambda()
        {
            {
                int startValue = 100;
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue++, 100));
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue++, 101));
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue++, 102));
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue++, 103));
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue++, 104));
            }
            {
                int startValue = 200;
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue, 210));
                startValue += 10;
                EventBetter.Register(this, (DummyMessage o) => Dummy(startValue, 210));
            }
        }

        public void TestSelf()
        {
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
        }

        public void TestSelfAdv()
        {
            var date = System.DateTime.Now.Date;
            var str = "TestString";
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            EventBetter.Register(this, (DummyMessage o) => Dummy(date, System.DateTime.Now.Date));
            EventBetter.Register(this, (DummyMessage o) => Dummy(str, "TestString"));
        }

        public void TestSelfStatic()
        {
            EventBetter.Register(this, (DummyMessage o) => DummyStatic(null, null));
        }

        public void TestStruct()
        {
            var someStruct = new SomeStruct() { Value = 123 };
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            EventBetter.Register(this, (DummyMessage o) => Dummy(someStruct, new SomeStruct() { Value = 123 }));
            EventBetter.Register(this, (DummyMessage o) => Dummy(someStruct.Value, 123));
        }

        public void TestStructStatic()
        {
            var someStruct = new SomeStruct() { Value = 123 };
            EventBetter.Register(this, (DummyMessage o) => DummyStatic(null, null));
            EventBetter.Register(this, (DummyMessage o) => DummyStatic(someStruct, new SomeStruct() { Value = 123 }));
            EventBetter.Register(this, (DummyMessage o) => DummyStatic(someStruct.Value, 123));
        }

        public void TestClass()
        {
            var someClass = new SomeClass() { Value = 456 };

            // this captured implicitly
            Assert.Throws<InvalidOperationException>(() => EventBetter.Register(this, (DummyMessage o) => Dummy(null, null)));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Register(this, (DummyMessage o) => Dummy(someClass, new SomeClass() { Value = 456 })));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Register(this, (DummyMessage o) => Dummy(someClass.Value, 456)));
        }

        public void TestClassStatic()
        {
            var someClass = new SomeClass() { Value = 456 };
            EventBetter.Register(this, (DummyMessage o) => DummyStatic(null, null));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Register(this, (DummyMessage o) => DummyStatic(someClass, new SomeClass() { Value = 456 })));
            Assert.Throws<InvalidOperationException>(() => EventBetter.Register(this, (DummyMessage o) => DummyStatic(someClass.Value, 456)));
        }

        public void TestSomeOtherHost()
        {
            {
                var go = new GameObject("blah");
                go.transform.SetParent(transform);
                EventBetter.Register(go, (DummyMessage o) => DummyStatic(go.name, "blah"));
            }

            {
                var go = new GameObject("blah2");
                go.transform.SetParent(transform);
                Assert.Throws<InvalidOperationException>(() => EventBetter.Register(go, (DummyMessage o) => Dummy(go.name, "blah2")));
            }
        }

        public void TestUnregister()
        {
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new DummyMessage()));

            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            Assert.AreEqual(true, EventBetter.Raise(new DummyMessage()));
            Assert.AreEqual(true, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new DummyMessage()));

            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            Assert.AreEqual(true, EventBetter.Raise(new DummyMessage()));
            Assert.AreEqual(true, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new DummyMessage()));

            EventBetter.Register(this, (DummyMessage o) => Dummy(null, null));
            Assert.AreEqual(true, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Unregister<DummyMessage>(this));
            Assert.AreEqual(false, EventBetter.Raise(new DummyMessage()));
        }

        public void TestNestedRegisterSimple()
        {
            int totalInvocations = 0;

            EventBetter.Register(this, (DummyMessage o) =>
            {
                Dummy(null, null);
                ++totalInvocations;

                EventBetter.Register(this, (DummyMessage oo) =>
                {
                    Dummy(null, null);
                    ++totalInvocations;
                });
            });

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(2, totalInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(5, totalInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(9, totalInvocations);
        }

        List<System.IDisposable> manualHandlers = new List<System.IDisposable>();
        public void TestNestedRegisterSimpleManual()
        {
            int totalInvocations = 0;

            manualHandlers.Add(EventBetter.RegisterManual((DummyMessage o) =>
            {
                Dummy(null, null);
                ++totalInvocations;

                manualHandlers.Add(EventBetter.RegisterManual((DummyMessage oo) =>
                {
                    Dummy(null, null);
                    ++totalInvocations;
                }));
            }));

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(2, totalInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(5, totalInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(9, totalInvocations);

            foreach (var handler in manualHandlers)
                handler.Dispose();
        }

        public void TestNestedRaiseSimple()
        {
            int nestedCalls = 0;
            int totalInvocations = 0;

            EventBetter.Register(this, (DummyMessage o) =>
            {
                ++totalInvocations;
                Dummy(null, null);
                try
                {
                    ++nestedCalls;
                    if (nestedCalls < 10)
                    {
                        EventBetter.Raise(new DummyMessage());
                    }
                }
                finally
                {
                    --nestedCalls;
                }
            });

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(10, totalInvocations);
        }

        public void TestNestedRaiseSimpleManual()
        {
            int nestedCalls = 0;
            int totalInvocations = 0;

            var handler = EventBetter.RegisterManual((DummyMessage o) =>
            {
                ++totalInvocations;
                Dummy(null, null);
                try
                {
                    ++nestedCalls;
                    if (nestedCalls < 10)
                    {
                        EventBetter.Raise(new DummyMessage());
                    }
                }
                finally
                {
                    --nestedCalls;
                }
            });

            try
            {
                EventBetter.Raise(new DummyMessage());
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
            EventBetter.Register(this, (DummyMessage2 o) =>
            {
                Dummy(null, null);
                ++dummyMessage2Invocations;

                EventBetter.Unregister<DummyMessage>(this);
            });

            int dummyMessage3Invocations = 0;
            EventBetter.Register(this, (DummyMessage3 o) =>
            {
                Dummy(null, null);
                ++dummyMessage3Invocations;

                EventBetter.Unregister<DummyMessage3>(this);
                EventBetter.Raise(new DummyMessage());
            });

            EventBetter.Register(this, (DummyMessage o) =>
            {
                Dummy(null, null);
                ++totalInvocations;
                ++nestedCalls;
                if (nestedCalls >= 10)
                {
                    EventBetter.Raise(new DummyMessage2());
                    EventBetter.Raise(new DummyMessage3());

                    EventBetter.Raise(new DummyMessage2());
                    EventBetter.Raise(new DummyMessage3());

                    EventBetter.Raise(new DummyMessage2());
                    EventBetter.Raise(new DummyMessage3());
                }

                EventBetter.Raise(new DummyMessage());
            });

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(10, totalInvocations);
            Assert.AreEqual(3, dummyMessage2Invocations);
            Assert.AreEqual(1, dummyMessage3Invocations);
        }

        int nestedInvocations = 0;
        public void TestNestedRaiseContexts()
        {
            int totalInvocations = 0;
            nestedInvocations = 0;

            EventBetter.Register(this, (DummyMessage o) =>
            {
                Dummy(null, null);
                ++totalInvocations;
                NestedDifferentContext(totalInvocations);
            });

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(2, totalInvocations + nestedInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(5, totalInvocations + nestedInvocations);

            EventBetter.Raise(new DummyMessage());
            Assert.AreEqual(9, totalInvocations + nestedInvocations);
        }

        public void NestedDifferentContext(int baseValue)
        {
            EventBetter.Register(this, (DummyMessage o) =>
            {
                Dummy(null, null);
                ++nestedInvocations;
            });
        }
            

        private void Awake()
        {
            expectedInstanceId = GetInstanceID();
        }

        private void Dummy(object param, object expectedValue)
        {
            Assert.AreEqual(expectedInstanceId, GetInstanceID());
            DummyStatic(param, expectedValue);
        }

        private static void DummyStatic(object param, object expectedValue)
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

        Assert.AreEqual(expectedResult, EventBetter.Raise(new DummyMessage()));

        UnityEngine.Object.DestroyImmediate(go);
        Assert.IsFalse(EventBetter.Raise(new DummyMessage()));
    }

    [SetUp]
    public void SetUp()
    {
        EventBetter.Clear();
    }

    [Test] public void RegisterManual()
    {
        int someValue = 666;

        var disp1 = EventBetter.RegisterManual((DummyMessage msg) => someValue++);
        var disp2 = EventBetter.RegisterManual((DummyMessage msg) => someValue++);

        Assert.AreEqual(true, EventBetter.Raise(new DummyMessage()));
        Assert.AreEqual(668, someValue);

        disp1.Dispose();
        Assert.AreEqual(true, EventBetter.Raise(new DummyMessage()));
        Assert.AreEqual(669, someValue);

        // whether double dispose breaks anything
        disp1.Dispose();
        Assert.AreEqual(true, EventBetter.Raise(new DummyMessage()));
        Assert.AreEqual(670, someValue);

        disp2.Dispose();
        Assert.AreEqual(false, EventBetter.Raise(new DummyMessage()));
        Assert.AreEqual(670, someValue);
    }

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
