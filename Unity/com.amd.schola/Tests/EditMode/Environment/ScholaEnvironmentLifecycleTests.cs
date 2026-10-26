// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using AMD.Schola;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Environment
{
    /// <summary>
    /// US3 lifecycle integration: Reset → Observe → Action → Step → reward/status → episode end → Reset.
    /// </summary>
    public sealed class ScholaEnvironmentLifecycleTests
    {
        ScholaEnvironmentTestFixture m_Fixture;

        [SetUp]
        public void SetUp()
        {
            m_Fixture = ScholaEnvironmentTestFixture.Create();
        }

        [TearDown]
        public void TearDown()
        {
            m_Fixture?.Dispose();
        }

        [Test]
        public void Initialize_ReturnsAgentObservationAndActionSpaces()
        {
            var definition = m_Fixture.Environment.Initialize();

            Assert.That(definition, Is.Not.Null);
            var observationSpace = definition.ObservationSpace as DictSpace;
            Assert.That(observationSpace, Is.Not.Null, "ObservationSpace must be a DictSpace");
            Assert.That(
                observationSpace.Spaces.ContainsKey(ScholaEnvironmentTestFixture.PositionKey),
                Is.True,
                "Observation space must include the position sensor key"
            );

            var actionSpace = definition.ActionSpace as DictSpace;
            Assert.That(actionSpace, Is.Not.Null, "ActionSpace must be a DictSpace");
            Assert.That(
                actionSpace.Spaces.ContainsKey(ScholaEnvironmentTestFixture.TranslateKey),
                Is.True,
                "Action space must include the translate actuator key"
            );
        }

        [Test]
        public void SeedAndSetOptions_DoNotThrow()
        {
            Assert.DoesNotThrow(() => m_Fixture.Environment.Seed(0));
            Assert.DoesNotThrow(
                () => m_Fixture.Environment.SetOptions(new Dictionary<string, string>())
            );
        }

        [Test]
        public void Lifecycle_ResetObserveActStepTerminateReset_NextEpisodeUsesUs1AndUs2()
        {
            var start = ScholaEnvironmentTestFixture.StartPose;
            const float dx = 0.4f;
            const float dy = 0.7f;

            var initial = m_Fixture.Environment.Reset();
            Assert.That(initial, Is.InstanceOf<InitialAgentState>());
            Assert.That(initial, Is.Not.InstanceOf<AgentState>());
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(initial.Observations),
                start,
                "Reset observations must match snapshot pose P"
            );
            Assert.That(m_Fixture.Sink.InitialStates, Has.Count.EqualTo(1));

            m_Fixture.Reward.Reward = 1.5f;
            var step1 = m_Fixture.Environment.Step(m_Fixture.TranslateAction(dx));
            var afterDx = start + new Vector3(dx, 0f, 0f);
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(step1.Observations),
                afterDx,
                "First step observations must follow P+dx"
            );
            Assert.That(step1.Reward, Is.EqualTo(1.5f));
            Assert.That(step1.Terminated, Is.False);
            Assert.That(step1.Truncated, Is.False);

            m_Fixture.Status.Terminated = true;
            var terminal = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0f));
            Assert.That(terminal.Terminated, Is.True);
            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(2));
            Assert.That(m_Fixture.Sink.States[1].Terminated, Is.True);

            m_Fixture.Reward.Reward = 0.25f;
            var reset2 = m_Fixture.Environment.Reset();
            Assert.That(reset2, Is.InstanceOf<InitialAgentState>());
            Assert.That(reset2, Is.Not.InstanceOf<AgentState>());
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(reset2.Observations),
                start,
                "Second reset observations must be P again, not P+dx"
            );
            Assert.That(m_Fixture.Sink.InitialStates, Has.Count.EqualTo(2));

            var stepNext = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0f, dy));
            var afterDy = start + new Vector3(0f, dy, 0f);
            AssertPosition(
                m_Fixture.AgentGo.transform.position,
                afterDy,
                "Next episode must apply US2 from the reset pose (P+dy, not P+dx+dy)"
            );
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(stepNext.Observations),
                afterDy,
                "Next episode US1 observations must match the new pose"
            );
            Assert.That(stepNext.Reward, Is.EqualTo(0.25f));
            Assert.That(
                stepNext.Terminated,
                Is.False,
                "Reset must notify the status hook; the caller must not clear Terminated manually"
            );
        }

        static void AssertPosition(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(
                actual.x,
                Is.EqualTo(expected.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                message
            );
            Assert.That(
                actual.y,
                Is.EqualTo(expected.y).Within(ScholaEnvironmentTestFixture.Tolerance),
                message
            );
            Assert.That(
                actual.z,
                Is.EqualTo(expected.z).Within(ScholaEnvironmentTestFixture.Tolerance),
                message
            );
        }
    }
}
