// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola;
using NUnit.Framework;

namespace AMD.Schola.Tests.Environment
{
    /// <summary>
    /// Protocol-shaped AgentState field tests. These may pass in Phase 2 because the DTO is a stub of fields only.
    /// </summary>
    public sealed class AgentStateTests
    {
        [Test]
        public void AgentState_Defaults_HaveZeroRewardAndNotDone()
        {
            var state = new AgentState();

            Assert.That(state.Reward, Is.EqualTo(0f));
            Assert.That(state.Terminated, Is.False);
            Assert.That(state.Truncated, Is.False);
        }

        [Test]
        public void AgentState_CanCarryObservationsRewardAndFlags()
        {
            var observations = new DictPoint
            {
                Points = { ["position"] = new BoxPoint { Values = { 1f, 2f, 3f } } },
            };

            var state = new AgentState
            {
                Observations = observations,
                Reward = 1.5f,
                Terminated = true,
                Truncated = true,
            };
            state.Info["key"] = "value";

            Assert.That(state.Observations, Is.SameAs(observations));
            Assert.That(state.Reward, Is.EqualTo(1.5f));
            Assert.That(state.Terminated, Is.True);
            Assert.That(state.Truncated, Is.True);
            Assert.That(state.Info["key"], Is.EqualTo("value"));
        }
    }
}
