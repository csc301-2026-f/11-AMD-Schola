// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola;
using NUnit.Framework;

namespace AMD.Schola.Tests.Environment
{
    /// <summary>
    /// US3 episode-status tests. Terminated and truncated are independent proto fields.
    /// </summary>
    public sealed class ScholaEnvironmentTerminationTests
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
        public void Step_Normal_TerminatedAndTruncatedAreFalse()
        {
            var state = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(state.Terminated, Is.False);
            Assert.That(state.Truncated, Is.False);
            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(1));
            Assert.That(m_Fixture.Sink.States[0].Terminated, Is.False);
            Assert.That(m_Fixture.Sink.States[0].Truncated, Is.False);
        }

        [Test]
        public void Step_StatusHook_CanSetTerminatedTrue()
        {
            m_Fixture.Status.Terminated = true;

            var state = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(state.Terminated, Is.True);
            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(1));
            Assert.That(m_Fixture.Sink.States[0].Terminated, Is.True);
        }

        [Test]
        public void Step_Truncated_CanBeSetIndependentlyOfTerminated()
        {
            m_Fixture.Status.Truncated = true;
            m_Fixture.Status.Terminated = false;

            var state = m_Fixture.Environment.Step(m_Fixture.TranslateAction(0.1f));

            Assert.That(state.Truncated, Is.True, "Truncated must be independently settable");
            Assert.That(state.Terminated, Is.False, "Setting truncated must not force terminated");
            Assert.That(m_Fixture.Sink.States, Has.Count.EqualTo(1));
            Assert.That(m_Fixture.Sink.States[0].Truncated, Is.True);
            Assert.That(m_Fixture.Sink.States[0].Terminated, Is.False);
        }
    }
}
