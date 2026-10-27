// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola;
using NUnit.Framework;

namespace AMD.Schola.Tests.Environment
{
    /// <summary>
    /// US3 reward tests. Scripted IReward values must appear on AgentState and the training-state sink.
    /// </summary>
    public sealed class ScholaEnvironmentRewardTests
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
        public void Step_ScriptedReward_AppearsOnReturnedAgentState()
        {
            m_Fixture.Reward.Reward = 1.5f;

            var state = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(state, Is.Not.Null);
            Assert.That(state.Reward, Is.EqualTo(1.5f));
        }

        [Test]
        public void Step_ScriptedReward_IsSubmittedToTrainingStateSink()
        {
            m_Fixture.Reward.Reward = 1.5f;

            m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(1));
            Assert.That(m_Fixture.Sink.States[0].Reward, Is.EqualTo(1.5f));
        }

        [Test]
        public void Step_TwoRewards_StayAssociatedWithCorrectSteps()
        {
            m_Fixture.Reward.Reward = 1.5f;
            var first = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            m_Fixture.Reward.Reward = 0.25f;
            var second = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(first.Reward, Is.EqualTo(1.5f), "First step must keep its own reward");
            Assert.That(
                second.Reward,
                Is.EqualTo(0.25f),
                "Second step must not bleed the first reward"
            );
            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(2));
            Assert.That(m_Fixture.Sink.States[0].Reward, Is.EqualTo(1.5f));
            Assert.That(m_Fixture.Sink.States[1].Reward, Is.EqualTo(0.25f));
        }
    }
}
