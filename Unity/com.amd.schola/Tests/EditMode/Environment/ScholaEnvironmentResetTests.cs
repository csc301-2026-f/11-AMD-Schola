// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Environment
{
    /// <summary>
    /// US3 reset tests. Reset restores the initialize snapshot and returns InitialAgentState only.
    /// </summary>
    public sealed class ScholaEnvironmentResetTests
    {
        const float DeltaX = 4f;

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
        public void Reset_AfterTermination_RestoresAgentTransformToInitializeSnapshot()
        {
            StepToTerminalPose();

            var terminal = ScholaEnvironmentTestFixture.StartPose + new Vector3(DeltaX, 0f, 0f);
            Assert.That(
                m_Fixture.AgentGo.transform.position.x,
                Is.EqualTo(terminal.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                "Step must apply the translate action so there is a terminal pose to restore from"
            );

            m_Fixture.Environment.Reset();

            Assert.That(
                m_Fixture.AgentGo.transform.position.x,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.x)
                    .Within(ScholaEnvironmentTestFixture.Tolerance)
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.y,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.y)
                    .Within(ScholaEnvironmentTestFixture.Tolerance)
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.z,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.z)
                    .Within(ScholaEnvironmentTestFixture.Tolerance)
            );
        }

        [Test]
        public void Reset_ObservationsMatchResetPose_NotTerminalPose()
        {
            StepToTerminalPose();
            var terminalPose = m_Fixture.AgentGo.transform.position;

            var initial = m_Fixture.Environment.Reset();

            Assert.That(initial, Is.Not.Null);
            var observed = ScholaEnvironmentTestFixture.PositionFromObservations(
                initial.Observations
            );
            Assert.That(
                observed.x,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.x)
                    .Within(ScholaEnvironmentTestFixture.Tolerance),
                "Reset observations must match the snapshot pose, not the terminal pose"
            );
            Assert.That(
                observed.y,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.y)
                    .Within(ScholaEnvironmentTestFixture.Tolerance)
            );
            Assert.That(
                observed.z,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.z)
                    .Within(ScholaEnvironmentTestFixture.Tolerance)
            );
            Assert.That(
                Mathf.Abs(observed.x - terminalPose.x),
                Is.GreaterThan(ScholaEnvironmentTestFixture.Tolerance),
                "Reset observations must not be stale from the terminal pose"
            );
        }

        [Test]
        public void Step_AfterReset_AppliesTranslateActionFromResetPose()
        {
            StepToTerminalPose();
            m_Fixture.Environment.Reset();

            const float nextDeltaX = 0.5f;
            m_Fixture.Environment.Step(m_Fixture.TranslateAction(nextDeltaX));

            var expected = ScholaEnvironmentTestFixture.StartPose + new Vector3(nextDeltaX, 0f, 0f);
            Assert.That(
                m_Fixture.AgentGo.transform.position.x,
                Is.EqualTo(expected.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                "Next episode must apply US2 actions from the reset pose, not the previous terminal pose"
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.y,
                Is.EqualTo(expected.y).Within(ScholaEnvironmentTestFixture.Tolerance)
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.z,
                Is.EqualTo(expected.z).Within(ScholaEnvironmentTestFixture.Tolerance)
            );
        }

        [Test]
        public void Reset_ReturnsInitialAgentState_NotAgentState()
        {
            m_Fixture.Reward.Reward = 1.5f;
            StepToTerminalPose();
            var trainingCountAfterTerminal = m_Fixture.Sink.States.Count;

            var initial = m_Fixture.Environment.Reset();

            Assert.That(initial, Is.InstanceOf<InitialAgentState>());
            Assert.That(
                initial,
                Is.Not.InstanceOf<AgentState>(),
                "Reset must return InitialAgentState, which cannot carry reward or done flags"
            );
            Assert.That(
                m_Fixture.Sink.InitialStates,
                Has.Count.EqualTo(1),
                "Reset must submit InitialAgentState to the sink"
            );
            Assert.That(
                m_Fixture.Sink.States,
                Has.Count.EqualTo(trainingCountAfterTerminal),
                "Reset must not submit a training AgentState with the terminal reward"
            );
        }

        [Test]
        public void Reset_StatefulStatusHook_StartsFreshEpisodeWithoutCallerClearingState()
        {
            var status =
                m_Fixture.AgentGo.AddComponent<ScholaEnvironmentTestFixture.TruncateAfterNStepsStatus>();
            status.MaxSteps = 2;
            m_Fixture.Environment.SetEpisodeStatus(status);

            var step1 = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));
            Assert.That(
                step1.Truncated,
                Is.False,
                "First step of an episode must not truncate yet"
            );

            var step2 = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));
            Assert.That(step2.Truncated, Is.True, "Status hook truncates after N steps");

            m_Fixture.Environment.Reset();

            var next1 = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));
            Assert.That(
                next1.Truncated,
                Is.False,
                "After Reset, a stateful status hook must start a fresh episode; the caller must not clear its counter"
            );
            Assert.That(next1.Terminated, Is.False);

            var next2 = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));
            Assert.That(
                next2.Truncated,
                Is.True,
                "The hook must still truncate after N steps in the new episode"
            );
        }

        [Test]
        public void Reset_AfterStep_RestoresSiblingActuatorOutsideAgentHierarchy()
        {
            m_Fixture.Dispose();
            m_Fixture = ScholaEnvironmentTestFixture.Create(
                ScholaEnvironmentTestFixture.ActuatorPlacement.OnSibling
            );

            var start = m_Fixture.ActuatorGo.transform.position;
            m_Fixture.Environment.Step(m_Fixture.TranslateAction(DeltaX));

            var terminal = start + new Vector3(DeltaX, 0f, 0f);
            Assert.That(
                m_Fixture.ActuatorGo.transform.position.x,
                Is.EqualTo(terminal.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                "Step must move the sibling actuator so there is a terminal pose to restore from"
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.x,
                Is.EqualTo(ScholaEnvironmentTestFixture.StartPose.x)
                    .Within(ScholaEnvironmentTestFixture.Tolerance),
                "Sibling actuator must not move the agent itself"
            );

            m_Fixture.Environment.Reset();

            AssertPosition(
                m_Fixture.ActuatorGo.transform.position,
                start,
                "Reset must restore configured actuator transforms that sit outside the agent hierarchy"
            );

            const float nextDeltaX = 0.5f;
            m_Fixture.Environment.Step(m_Fixture.TranslateAction(nextDeltaX));
            AssertPosition(
                m_Fixture.ActuatorGo.transform.position,
                start + new Vector3(nextDeltaX, 0f, 0f),
                "Next episode must apply US2 actions from the restored sibling pose, not the terminal pose"
            );
        }

        [Test]
        public void Reset_AfterStep_RestoresParentActuatorOutsideAgentHierarchy()
        {
            m_Fixture.Dispose();
            m_Fixture = ScholaEnvironmentTestFixture.Create(
                ScholaEnvironmentTestFixture.ActuatorPlacement.OnParent
            );

            var start = m_Fixture.ActuatorGo.transform.position;
            m_Fixture.Environment.Step(m_Fixture.TranslateAction(DeltaX));

            var terminal = start + new Vector3(DeltaX, 0f, 0f);
            Assert.That(
                m_Fixture.ActuatorGo.transform.position.x,
                Is.EqualTo(terminal.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                "Step must move the parent actuator so there is a terminal pose to restore from"
            );
            Assert.That(
                m_Fixture.AgentGo.transform.position.x,
                Is.EqualTo(terminal.x).Within(ScholaEnvironmentTestFixture.Tolerance),
                "Moving a parent actuator must also move the child agent in world space"
            );

            var initial = m_Fixture.Environment.Reset();

            AssertPosition(
                m_Fixture.ActuatorGo.transform.position,
                start,
                "Reset must restore a parent actuator; restoring only the child agent's local pose is not enough"
            );
            AssertPosition(
                m_Fixture.AgentGo.transform.position,
                start,
                "Restoring the parent actuator must also restore the agent world pose"
            );
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(initial.Observations),
                start,
                "Reset observations must match the restored world pose, not the terminal pose"
            );

            const float nextDeltaX = 0.5f;
            var next = m_Fixture.Environment.Step(m_Fixture.TranslateAction(nextDeltaX));
            var expected = start + new Vector3(nextDeltaX, 0f, 0f);
            AssertPosition(
                m_Fixture.ActuatorGo.transform.position,
                expected,
                "Next episode must apply US2 actions from the restored parent pose, not the terminal pose"
            );
            AssertPosition(
                ScholaEnvironmentTestFixture.PositionFromObservations(next.Observations),
                expected,
                "US1 observations after the next step must follow the restored parent pose"
            );
        }

        void StepToTerminalPose()
        {
            m_Fixture.Status.Terminated = true;
            m_Fixture.Environment.Step(m_Fixture.TranslateAction(DeltaX));
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
