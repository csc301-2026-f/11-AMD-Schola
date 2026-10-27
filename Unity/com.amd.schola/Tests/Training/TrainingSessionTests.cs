// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using AMD.Schola.Training;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Training
{
    public sealed class TrainingSessionTests
    {
        sealed class Environment : IEnvironment
        {
            public int Initializes,
                Steps,
                Resets;
            public float Position;
            public bool Terminate;
            public bool Truncate;
            public int? LastSeed;
            public IReadOnlyDictionary<string, string> Options;

            public InteractionDefinition Initialize()
            {
                Initializes++;
                return new InteractionDefinition(
                    new BoxSpace { Dimensions = { new BoxSpaceDimension() } },
                    new BoxSpace { Dimensions = { new BoxSpaceDimension(-1, 1) } }
                );
            }

            public void Seed(int seed) => LastSeed = seed;

            public void SetOptions(IReadOnlyDictionary<string, string> options) =>
                Options = options;

            public InitialAgentState Reset()
            {
                Resets++;
                Position = 0;
                return new InitialAgentState
                {
                    Observations = new BoxPoint { Values = { Position } },
                };
            }

            public AgentState Step(Point action)
            {
                Steps++;
                Position += ((BoxPoint)action).Values[0];
                return new AgentState
                {
                    Observations = new BoxPoint { Values = { Position } },
                    Reward = 2,
                    Terminated = Terminate,
                    Truncated = Truncate,
                    Info = { ["step"] = Steps.ToString() },
                };
            }
        }

        static TrainingRequest Request(string command) => new TrainingRequest { command = command };

        static TrainingRequest Step(float value = 0.5f) =>
            new TrainingRequest
            {
                command = "step",
                action = TrainingWire.EncodePoint(new BoxPoint { Values = { value } }),
            };

        [Test]
        public void RequiresStartupAndReset_AndForwardsSeedAndOptions()
        {
            var env = new Environment();
            var session = new TrainingSession(env);
            Assert.That(session.Handle(Step()).ok, Is.False);
            Assert.That(session.Handle(Request("start")).ok, Is.True);
            Assert.That(session.Handle(Step()).ok, Is.False);
            Assert.That(
                session
                    .Handle(
                        new TrainingRequest
                        {
                            command = "reset",
                            hasSeed = true,
                            seed = 17,
                            options = new[]
                            {
                                new TrainingEntry { key = "difficulty", value = "easy" },
                            },
                        }
                    )
                    .ok,
                Is.True
            );
            Assert.That(env.LastSeed, Is.EqualTo(17));
            Assert.That(env.Options["difficulty"], Is.EqualTo("easy"));
            Assert.That(session.Handle(Step()).reward, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameStepReset_PreservesFinalObservationAndEpisodeFlags(bool truncate)
        {
            var env = new Environment { Terminate = !truncate, Truncate = truncate };
            var session = new TrainingSession(env);
            session.Handle(Request("start"));
            session.Handle(Request("reset"));
            var response = session.Handle(Step());
            Assert.That(response.ok, Is.True, response.error);
            Assert.That(response.terminated, Is.EqualTo(!truncate));
            Assert.That(response.truncated, Is.EqualTo(truncate));
            Assert.That(
                ((BoxPoint)TrainingWire.DecodePoint(response.observations)).Values[0],
                Is.EqualTo(0.5f)
            );
            Assert.That(response.hasInitial, Is.True);
            Assert.That(
                ((BoxPoint)TrainingWire.DecodePoint(response.initialObservations)).Values[0],
                Is.Zero
            );
            Assert.That(env.Resets, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void NextStepAndDisabledReset_RespectEpisodeBoundary(int mode)
        {
            var env = new Environment { Terminate = true };
            var session = new TrainingSession(env);
            session.Handle(new TrainingRequest { command = "start", autoreset = mode });
            session.Handle(Request("reset"));
            Assert.That(session.Handle(Step()).hasInitial, Is.False);
            var next = session.Handle(Step());
            Assert.That(env.Steps, Is.EqualTo(1));
            Assert.That(next.ok, Is.EqualTo(mode == 1));
            if (mode == 1)
            {
                Assert.That(next.reward, Is.Zero);
                Assert.That(next.terminated, Is.False);
                Assert.That(env.Resets, Is.EqualTo(2));
            }
        }

        [Test]
        public void InvalidAction_DoesNotAdvanceEnvironment_AndRequiresReset()
        {
            var env = new Environment();
            var session = new TrainingSession(env);
            session.Handle(Request("start"));
            session.Handle(Request("reset"));
            Assert.That(session.Handle(Step(2)).ok, Is.False);
            Assert.That(env.Steps, Is.Zero);
            Assert.That(session.Handle(Step()).ok, Is.False);
            session.Handle(Request("reset"));
            Assert.That(session.Handle(Step()).ok, Is.True);
        }

        [Test]
        public void Reconnect_DoesNotReinitializeResetSnapshot()
        {
            var env = new Environment();
            var session = new TrainingSession(env);
            session.Handle(Request("start"));
            session.Handle(Request("reset"));
            session.Handle(Step());
            session.Handle(Request("close"));
            Assert.That(session.Handle(Request("start")).ok, Is.True);
            Assert.That(env.Initializes, Is.EqualTo(1));
            Assert.That(session.Handle(Step()).ok, Is.False);
            session.Handle(Request("reset"));
            Assert.That(env.Position, Is.Zero);
        }

        [Test]
        public void Wire_RoundTripsNestedDictAndRejectsDuplicateOrMalformedNodes()
        {
            var source = new DictPoint
            {
                Points =
                {
                    ["nested"] = new DictPoint
                    {
                        Points = { ["move"] = new BoxPoint { Values = { 1, 2, 3 } } },
                    },
                    ["choice"] = new DiscretePoint { Value = 2 },
                },
            };
            var nodes = TrainingWire.EncodePoint(source);
            var decoded = (DictPoint)TrainingWire.DecodePoint(nodes);
            Assert.That(((DiscretePoint)decoded.Points["choice"]).Value, Is.EqualTo(2));
            Assert.That(
                ((BoxPoint)((DictPoint)decoded.Points["nested"]).Points["move"]).Values,
                Is.EqualTo(new[] { 1f, 2f, 3f })
            );
            // Also exercise the actual Unity serializer used on the socket boundary.
            var request = JsonUtility.FromJson<TrainingRequest>(
                JsonUtility.ToJson(new TrainingRequest { action = nodes })
            );
            Assert.That(TrainingWire.DecodePoint(request.action), Is.TypeOf<DictPoint>());
            nodes[2].parent = 100;
            Assert.Throws<ArgumentException>(() => TrainingWire.DecodePoint(nodes));
            Assert.Throws<ArgumentException>(() =>
                TrainingWire.DecodePoint(
                    new[]
                    {
                        new TrainingNode { kind = "box", values = new[] { float.NaN } },
                    }
                )
            );
        }

        [Test]
        public void BoundedTranslateActuator_PublishesAndEnforcesBounds()
        {
            var go = new GameObject("BoundedActuator");
            try
            {
                var actuator = go.AddComponent<TranslateActuator>();
                actuator.MaxDisplacement = 0.1f;
                Assert.That(
                    ((BoxSpace)actuator.GetActionSpace()).Dimensions[0].High,
                    Is.EqualTo(0.1f)
                );
                Assert.Throws<ArgumentException>(() =>
                    actuator.TakeAction(new BoxPoint { Values = { 1, 0, 0 } })
                );
                Assert.That(go.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
