// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Steppers
{
    /// <summary>
    /// Runs a real ScholaAgent (PositionSensor + TranslateActuator) through SimpleStepper
    /// with a stand-in policy, so observe, think and act all touch a real scene object.
    /// </summary>
    public sealed class ScholaAgentSteppingTests
    {
        const float Tolerance = 1e-4f;
        const string PositionKey = "position";
        const string TranslateKey = "translate";

        // Stands in for a trained model: reads "position" and answers with
        // "translate" = -0.1 * position, so the object drifts toward the origin.
        sealed class MoveTowardOriginPolicy : IPolicy
        {
            public bool Init(InteractionDefinition definition)
            {
                return true;
            }

            public bool Think(Point observations, out Point actions)
            {
                actions = null;

                if (
                    !(observations is DictPoint dict)
                    || !dict.Points.TryGetValue(PositionKey, out var point)
                    || !(point is BoxPoint position)
                    || position.Values.Count != 3
                )
                {
                    return false;
                }

                actions = new DictPoint
                {
                    Points =
                    {
                        [TranslateKey] = new BoxPoint
                        {
                            Values =
                            {
                                -0.1f * position.Values[0],
                                -0.1f * position.Values[1],
                                -0.1f * position.Values[2],
                            },
                        },
                    },
                };
                return true;
            }
        }

        GameObject m_Go;
        ScholaAgent m_Agent;
        SimpleStepper m_Stepper;

        [SetUp]
        public void SetUp()
        {
            m_Go = new GameObject("SteppedAgent");
            var sensor = m_Go.AddComponent<PositionSensor>();
            var actuator = m_Go.AddComponent<TranslateActuator>();

            m_Agent = m_Go.AddComponent<ScholaAgent>();
            m_Agent.SetSensors(new[] { sensor });
            m_Agent.SetActuators(new[] { actuator });
            sensor.InitSensor();
            actuator.InitActuator();

            m_Stepper = new SimpleStepper();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Go);
        }

        [Test]
        public void ScholaAgent_CanBeUsedAsIAgent()
        {
            IAgent agent = m_Agent;
            Assert.That(agent, Is.Not.Null);
        }

        [Test]
        public void Step_MovesRealAgentUsingItsOwnObservation()
        {
            m_Go.transform.position = new Vector3(10f, 0f, 0f);
            Assert.That(
                m_Stepper.Init(new List<IAgent> { m_Agent }, new MoveTowardOriginPolicy()),
                Is.True
            );

            Assert.That(m_Stepper.Step(), Is.True);

            // 10 + (-0.1 * 10) = 9
            Assert.That(m_Go.transform.position.x, Is.EqualTo(9f).Within(Tolerance));
            Assert.That(m_Go.transform.position.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(m_Go.transform.position.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Step_CalledTwice_SecondStepSeesTheMovedPosition()
        {
            m_Go.transform.position = new Vector3(10f, 0f, 0f);
            Assert.That(
                m_Stepper.Init(new List<IAgent> { m_Agent }, new MoveTowardOriginPolicy()),
                Is.True
            );

            Assert.That(m_Stepper.Step(), Is.True);
            Assert.That(m_Stepper.Step(), Is.True);

            // 10 -> 9 -> 9 + (-0.1 * 9) = 8.1
            Assert.That(m_Go.transform.position.x, Is.EqualTo(8.1f).Within(Tolerance));
        }
    }
}
