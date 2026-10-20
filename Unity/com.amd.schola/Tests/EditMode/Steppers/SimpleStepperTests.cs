// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AMD.Schola.Tests.Steppers
{
    public sealed class SimpleStepperTests
    {
        // Records "observe:<id>" and "act:<id>" in a shared log so tests can check ordering.
        sealed class FakeAgent : IAgent
        {
            readonly int m_Id;
            readonly List<string> m_Log;

            public FakeAgent(int id, List<string> log)
            {
                m_Id = id;
                m_Log = log;
            }

            public Point LastAction { get; private set; }

            public Point Observe()
            {
                m_Log.Add($"observe:{m_Id}");
                return new DiscretePoint { Value = m_Id };
            }

            public void Act(Point action)
            {
                m_Log.Add($"act:{m_Id}");
                LastAction = action;
            }
        }

        // A real MonoBehaviour agent, so a test can destroy its GameObject.
        sealed class FakeBehaviourAgent : MonoBehaviour, IAgent
        {
            public Point Observe()
            {
                return new DiscretePoint { Value = 99 };
            }

            public void Act(Point action) { }
        }

        // Echoes the observation back as the action, so a test can tell which agent got which.
        sealed class FakePolicy : IPolicy
        {
            public bool ShouldSucceed = true;
            public int ThinkCalls;

            public bool Init(InteractionDefinition definition)
            {
                return true;
            }

            public bool Think(Point observations, out Point actions)
            {
                ThinkCalls++;
                if (!ShouldSucceed)
                {
                    actions = null;
                    return false;
                }

                actions = observations;
                return true;
            }
        }

        List<string> m_Log;
        FakePolicy m_Policy;
        SimpleStepper m_Stepper;

        [SetUp]
        public void SetUp()
        {
            m_Log = new List<string>();
            m_Policy = new FakePolicy();
            m_Stepper = new SimpleStepper();
        }

        [Test]
        public void Init_NoAgents_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("No agents provided"));
            Assert.That(m_Stepper.Init(new List<IAgent>(), m_Policy), Is.False);
        }

        [Test]
        public void Init_NullAgentList_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("No agents provided"));
            Assert.That(m_Stepper.Init(null, m_Policy), Is.False);
        }

        [Test]
        public void Init_NullAgentInList_ReturnsFalse()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log), null };

            LogAssert.Expect(LogType.Error, new Regex("Agent 1 is null"));
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.False);
        }

        [Test]
        public void Init_NullPolicy_ReturnsFalse()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log) };

            LogAssert.Expect(LogType.Error, new Regex("No policy provided"));
            Assert.That(m_Stepper.Init(agents, null), Is.False);
        }

        [Test]
        public void Step_BeforeInit_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("Init must succeed before Step"));
            Assert.That(m_Stepper.Step(), Is.False);
        }

        [Test]
        public void Step_SingleAgent_ObservesThenActs()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log) };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);

            Assert.That(m_Stepper.Step(), Is.True);

            Assert.That(m_Log, Is.EqualTo(new[] { "observe:0", "act:0" }));
            Assert.That(m_Policy.ThinkCalls, Is.EqualTo(1));
        }

        [Test]
        public void Step_AllAgentsObserveBeforeAnyAct()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log), new FakeAgent(1, m_Log) };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);

            Assert.That(m_Stepper.Step(), Is.True);

            Assert.That(
                m_Log,
                Is.EqualTo(new[] { "observe:0", "observe:1", "act:0", "act:1" })
            );
        }

        [Test]
        public void Step_GivesEachAgentItsOwnAction()
        {
            var first = new FakeAgent(3, m_Log);
            var second = new FakeAgent(7, m_Log);
            Assert.That(m_Stepper.Init(new List<IAgent> { first, second }, m_Policy), Is.True);

            Assert.That(m_Stepper.Step(), Is.True);

            Assert.That(((DiscretePoint)first.LastAction).Value, Is.EqualTo(3));
            Assert.That(((DiscretePoint)second.LastAction).Value, Is.EqualTo(7));
        }

        [Test]
        public void Step_PolicyFails_NoAgentActs()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log), new FakeAgent(1, m_Log) };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);
            m_Policy.ShouldSucceed = false;

            LogAssert.Expect(LogType.Error, new Regex("Policy failed to think"));
            Assert.That(m_Stepper.Step(), Is.False);

            Assert.That(m_Log, Has.None.StartWith("act:"));
        }

        [Test]
        public void Step_CallerEditsListAfterInit_StillRunsOriginalAgents()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log) };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);

            agents.Add(null); // would crash Step if the stepper kept this list

            Assert.That(m_Stepper.Step(), Is.True);
            Assert.That(m_Log, Is.EqualTo(new[] { "observe:0", "act:0" }));
        }

        [Test]
        public void Step_DestroyedAgent_IsSkipped()
        {
            var go = new GameObject("agent");
            var behaviourAgent = go.AddComponent<FakeBehaviourAgent>();
            var agents = new List<IAgent> { new FakeAgent(0, m_Log), behaviourAgent };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);

            UnityEngine.Object.DestroyImmediate(go);

            LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed agent"));
            Assert.That(m_Stepper.Step(), Is.True);
            Assert.That(m_Log, Is.EqualTo(new[] { "observe:0", "act:0" }));
        }

        [Test]
        public void Step_CalledTwice_RunsFullCycleEachTime()
        {
            var agents = new List<IAgent> { new FakeAgent(0, m_Log) };
            Assert.That(m_Stepper.Init(agents, m_Policy), Is.True);

            Assert.That(m_Stepper.Step(), Is.True);
            Assert.That(m_Stepper.Step(), Is.True);

            Assert.That(m_Policy.ThinkCalls, Is.EqualTo(2));
            Assert.That(
                m_Log,
                Is.EqualTo(new[] { "observe:0", "act:0", "observe:0", "act:0" })
            );
        }
    }
}