// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using AMD.Schola.Training;
using NUnit.Framework;

namespace AMD.Schola.Tests.Training
{
    public sealed class TrainingBatchSessionTests
    {
        sealed class Environment : IEnvironment
        {
            public int Steps;
            public int Resets;
            public int? SeedValue;
            public IReadOnlyDictionary<string, string> Options;
            public float Position;
            public float Goal;
            public bool TruncateAtGoal;
            public bool BothFlags;
            public int Initializes,
                SeedCalls,
                OptionCalls;
            public bool FailInitialize,
                FailSeed,
                FailOptions,
                FailReset,
                FailStep,
                BadReward;

            public InteractionDefinition Initialize()
            {
                Initializes++;
                if (FailInitialize)
                    throw new InvalidOperationException("Initialize failed");
                return new InteractionDefinition(
                    new BoxSpace { Dimensions = { new BoxSpaceDimension() } },
                    new BoxSpace { Dimensions = { new BoxSpaceDimension(-1, 1) } }
                );
            }

            public void Seed(int seed)
            {
                SeedCalls++;
                SeedValue = seed;
                if (FailSeed)
                    throw new InvalidOperationException("Seed failed");
            }

            public void SetOptions(IReadOnlyDictionary<string, string> options)
            {
                OptionCalls++;
                Options = options;
                if (FailOptions)
                    throw new InvalidOperationException("Options failed");
            }

            public InitialAgentState Reset()
            {
                Resets++;
                Position = 0;
                if (FailReset)
                    throw new InvalidOperationException("Reset failed");
                return new InitialAgentState
                {
                    Observations = new BoxPoint { Values = { 0 } },
                    Info = { ["episode"] = Resets.ToString() },
                };
            }

            public AgentState Step(Point action)
            {
                Steps++;
                Position += ((BoxPoint)action).Values[0];
                if (FailStep)
                    throw new InvalidOperationException("Step failed");
                return new AgentState
                {
                    Observations = new BoxPoint { Values = { Position } },
                    Reward = BadReward ? float.NaN : Position,
                    Terminated = Position >= Goal && (!TruncateAtGoal || BothFlags),
                    Truncated = Position >= Goal && (TruncateAtGoal || BothFlags),
                    Info = { ["step"] = Steps.ToString() },
                };
            }
        }

        static TrainingEnvironmentRequest Entry(int id, float action) =>
            new TrainingEnvironmentRequest
            {
                id = id,
                actions = new[]
                {
                    new TrainingAgentAction
                    {
                        id = "SingleAgent",
                        action = TrainingWire.EncodePoint(new BoxPoint { Values = { action } }),
                    },
                },
            };

        static TrainingRequest Reset() =>
            new TrainingRequest
            {
                command = "reset",
                environments = new[]
                {
                    new TrainingEnvironmentRequest
                    {
                        id = 0,
                        hasSeed = true,
                        seed = 17,
                    },
                    new TrainingEnvironmentRequest
                    {
                        id = 1,
                        options = new[]
                        {
                            new TrainingEntry { key = "difficulty", value = "easy" },
                        },
                    },
                },
            };

        [TestCase(0)]
        [TestCase(1)]
        public void Batch_RoutesDefinitionsActionsAndIndependentResetResults(int mode)
        {
            var a = new Environment { Goal = 0.5f };
            var b = new Environment { Goal = 1f };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            var start = batch.Handle(new TrainingRequest { command = "start", autoreset = mode });
            Assert.That(start.ok, Is.True, start.error);
            Assert.That(start.environmentCount, Is.EqualTo(2));
            var definition = batch.Handle(new TrainingRequest { command = "definition" });
            Assert.That(definition.environments[0].agents[0].id, Is.EqualTo("SingleAgent"));
            Assert.That(definition.environments[1].agents[0].id, Is.EqualTo("SingleAgent"));
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            Assert.That(a.SeedValue, Is.EqualTo(17));
            Assert.That(b.SeedValue, Is.Null);
            Assert.That(b.Options["difficulty"], Is.EqualTo("easy"));
            var final = batch.Handle(
                new TrainingRequest
                {
                    command = "step",
                    environments = new[] { Entry(0, 0.5f), Entry(1, 0.25f) },
                }
            );
            Assert.That(final.ok, Is.True, final.error);
            Assert.That(final.environments[0].states[0].terminated, Is.True);
            Assert.That(final.environments[0].states[0].reward, Is.EqualTo(0.5f));
            Assert.That(final.environments[1].states[0].terminated, Is.False);
            Assert.That(final.environments[0].hasInitial, Is.EqualTo(mode == 0));
            Assert.That(final.environments[1].hasInitial, Is.False);
            var nextA = Entry(0, 0.1f);
            if (mode == 1)
                nextA.actions = Array.Empty<TrainingAgentAction>();
            var next = batch.Handle(
                new TrainingRequest
                {
                    command = "step",
                    environments = new[] { nextA, Entry(1, 0.25f) },
                }
            );
            Assert.That(next.ok, Is.True, next.error);
            Assert.That(a.Resets, Is.EqualTo(2));
            Assert.That(b.Resets, Is.EqualTo(1));
            Assert.That(b.Position, Is.EqualTo(0.5f));
            Assert.That(a.Steps, Is.EqualTo(mode == 1 ? 1 : 2));
        }

        [Test]
        public void Batch_DisabledReset_FreezesCompletedEnvironmentsAndPreservesFinalStates()
        {
            var a = new Environment { Goal = 0.25f };
            var b = new Environment { Goal = 0.75f, TruncateAtGoal = true };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            Assert.That(
                batch.Handle(new TrainingRequest { command = "start", autoreset = 2 }).ok,
                Is.True
            );
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            var request = new TrainingRequest
            {
                command = "step",
                environments = new[] { Entry(0, 0.25f), Entry(1, 0.25f) },
            };
            var first = batch.Handle(request);
            Assert.That(first.ok, Is.True, first.error);
            // Returned objects must not give callers access to stored terminal state.
            first.environments[0].states[0].reward = 99;
            first.environments[0].states[0].observations[0].values[0] = 99;
            first.environments[0].states[0].info[0].value = "changed";
            request.environments[0].actions = Array.Empty<TrainingAgentAction>();
            for (var tick = 2; tick <= 4; tick++)
            {
                // Completed environments ignore supplied actions as well as empty ones.
                request.environments[0] = Entry(0, 2);
                if (tick == 3)
                    request.environments[0].actions = null;
                if (tick == 4)
                    request.environments[1] = Entry(1, 2);
                var response = batch.Handle(request);
                Assert.That(response.ok, Is.True, response.error);
                var state = response.environments[0].states[0];
                Assert.That(state.reward, Is.EqualTo(0.25f));
                Assert.That(state.observations[0].values[0], Is.EqualTo(0.25f));
                Assert.That(state.info[0].value, Is.EqualTo("1"));
                Assert.That(state.terminated, Is.True);
                Assert.That(state.truncated, Is.False);
                Assert.That(a.Steps, Is.EqualTo(1));
                Assert.That(b.Steps, Is.EqualTo(Math.Min(tick, 3)));
                Assert.That(b.Position, Is.EqualTo(Math.Min(tick, 3) * 0.25f));
                Assert.That(response.environments[1].states[0].truncated, Is.EqualTo(tick >= 3));
                Assert.That(a.Resets, Is.EqualTo(1));
                Assert.That(b.Resets, Is.EqualTo(1));
                Assert.That(response.environments[0].hasInitial, Is.False);
                Assert.That(response.environments[1].hasInitial, Is.False);
                // A subsequent response must also be independent of the retained snapshot.
                state.observations[0].values[0] = -99;
                state.info[0].value = "changed again";
            }
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            Assert.That(a.Resets, Is.EqualTo(2));
            Assert.That(b.Resets, Is.EqualTo(2));
            request.environments[0] = Entry(0, 0.1f);
            request.environments[1] = Entry(1, 0.25f);
            Assert.That(batch.Handle(request).ok, Is.True);
            Assert.That(a.Position, Is.EqualTo(0.1f));
            Assert.That(b.Position, Is.EqualTo(0.25f));

            Assert.That(batch.Handle(new TrainingRequest { command = "close" }).ok, Is.True);
            Assert.That(
                batch.Handle(new TrainingRequest { command = "start", autoreset = 0 }).ok,
                Is.True
            );
            Assert.That(batch.Handle(request).ok, Is.False);
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            request.environments[0] = Entry(0, 0.25f);
            var restarted = batch.Handle(request);
            Assert.That(restarted.ok, Is.True, restarted.error);
            Assert.That(restarted.environments[0].hasInitial, Is.True);
            Assert.That(restarted.environments[1].hasInitial, Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        public void Batch_RepeatedEpisodes_ResetOnlyTheCompletedEnvironment(int mode, int ending)
        {
            var environments = new[]
            {
                new Environment
                {
                    Goal = 0.5f,
                    TruncateAtGoal = ending == 1,
                    BothFlags = ending == 2,
                },
                new Environment { Goal = 0.75f, TruncateAtGoal = true },
            };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(environments[0]),
                    new SingleAgentEnvironmentAdapter(environments[1]),
                }
            );
            Assert.That(
                batch.Handle(new TrainingRequest { command = "start", autoreset = mode }).ok,
                Is.True
            );
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            var pending = new bool[2];
            var positions = new float[2];
            var steps = new int[2];
            var resets = new[] { 1, 1 };
            for (var tick = 1; tick <= 12; tick++)
            {
                var entries = new[] { Entry(0, 0.25f), Entry(1, 0.25f) };
                for (var i = 0; i < 2; i++)
                    if (pending[i])
                        entries[i].actions =
                            tick % 2 == 0 ? null : Array.Empty<TrainingAgentAction>();
                var result = batch.Handle(
                    new TrainingRequest { command = "step", environments = entries }
                );
                Assert.That(result.ok, Is.True, result.error);
                for (var i = 0; i < 2; i++)
                {
                    var resetStep = pending[i];
                    if (resetStep)
                    {
                        positions[i] = 0;
                        resets[i]++;
                    }
                    else
                    {
                        positions[i] += 0.25f;
                        steps[i]++;
                    }
                    var done = !resetStep && positions[i] >= environments[i].Goal;
                    var response = result.environments[i];
                    var state = response.states[0];
                    Assert.That(state.observations[0].values[0], Is.EqualTo(positions[i]));
                    Assert.That(state.reward, Is.EqualTo(resetStep ? 0 : positions[i]));
                    Assert.That(
                        state.terminated,
                        Is.EqualTo(
                            done && (!environments[i].TruncateAtGoal || environments[i].BothFlags)
                        )
                    );
                    Assert.That(
                        state.truncated,
                        Is.EqualTo(
                            done && (environments[i].TruncateAtGoal || environments[i].BothFlags)
                        )
                    );
                    Assert.That(response.hasInitial, Is.EqualTo(done && mode == 0));
                    if (response.hasInitial)
                    {
                        resets[i]++;
                        positions[i] = 0;
                        Assert.That(response.initialStates[0].observations[0].values[0], Is.Zero);
                        Assert.That(
                            response.initialStates[0].info[0].value,
                            Is.EqualTo(resets[i].ToString())
                        );
                    }
                    pending[i] = done && mode == 1;
                    Assert.That(environments[i].Steps, Is.EqualTo(steps[i]));
                    Assert.That(environments[i].Resets, Is.EqualTo(resets[i]));
                    Assert.That(environments[i].Position, Is.EqualTo(positions[i]));
                    Assert.That(
                        state.info[0].value,
                        Is.EqualTo(resetStep ? resets[i].ToString() : steps[i].ToString())
                    );
                }
            }
        }

        [Test]
        public void Batch_FailedStartupRequiresSuccessfulRestartBeforeReset()
        {
            var a = new Environment { Goal = 10 };
            var b = new Environment { Goal = 10, FailInitialize = true };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            Assert.That(batch.Handle(new TrainingRequest { command = "start" }).ok, Is.False);
            b.FailInitialize = false;
            Assert.That(batch.Handle(Reset()).ok, Is.False);
            Assert.That(a.Resets, Is.Zero);
            Assert.That(b.Resets, Is.Zero);
            Assert.That(batch.Handle(new TrainingRequest { command = "definition" }).ok, Is.False);
            Assert.That(batch.Handle(new TrainingRequest { command = "start" }).ok, Is.True);
            Assert.That(a.Initializes, Is.EqualTo(1));
            Assert.That(b.Initializes, Is.EqualTo(2));
            Assert.That(batch.Handle(Reset()).ok, Is.True);
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void Batch_InvalidStartupModeDoesNotInitializeAnyEnvironment(int mode)
        {
            var a = new Environment();
            var b = new Environment();
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            Assert.That(
                batch.Handle(new TrainingRequest { command = "start", autoreset = mode }).ok,
                Is.False
            );
            Assert.That(a.Initializes, Is.Zero);
            Assert.That(b.Initializes, Is.Zero);
        }

        [TestCase("count")]
        [TestCase("null_array")]
        [TestCase("null_entry")]
        [TestCase("duplicate_environment")]
        [TestCase("negative_environment")]
        [TestCase("wrong_order")]
        [TestCase("missing_action")]
        [TestCase("null_action")]
        [TestCase("unknown_agent")]
        [TestCase("duplicate_agent")]
        [TestCase("shape")]
        [TestCase("type")]
        [TestCase("nan")]
        [TestCase("infinity")]
        [TestCase("bounds")]
        public void Batch_InvalidStepNeverAdvancesEitherEnvironment(string failure)
        {
            var a = new Environment { Goal = 10 };
            var b = new Environment { Goal = 10 };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            batch.Handle(new TrainingRequest { command = "start" });
            batch.Handle(Reset());
            var request = new TrainingRequest
            {
                command = "step",
                environments = new[] { Entry(0, 0.5f), Entry(1, 0.25f) },
            };
            var second = request.environments[1];
            switch (failure)
            {
                case "count":
                    request.environments = new[] { request.environments[0] };
                    break;
                case "null_array":
                    request.environments = null;
                    break;
                case "null_entry":
                    request.environments[1] = null;
                    break;
                case "duplicate_environment":
                    second.id = 0;
                    break;
                case "negative_environment":
                    second.id = -1;
                    break;
                case "wrong_order":
                    Array.Reverse(request.environments);
                    break;
                case "missing_action":
                    second.actions = Array.Empty<TrainingAgentAction>();
                    break;
                case "null_action":
                    second.actions[0] = null;
                    break;
                case "unknown_agent":
                    second.actions[0].id = "unknown";
                    break;
                case "duplicate_agent":
                    second.actions = new[] { second.actions[0], second.actions[0] };
                    break;
                case "shape":
                    second.actions[0].action[0].shape = new[] { 2 };
                    break;
                case "type":
                    second.actions[0].action = TrainingWire.EncodePoint(
                        new DiscretePoint { Value = 0 }
                    );
                    break;
                case "nan":
                    second.actions[0].action[0].values[0] = float.NaN;
                    break;
                case "infinity":
                    second.actions[0].action[0].values[0] = float.PositiveInfinity;
                    break;
                case "bounds":
                    second.actions[0].action[0].values[0] = 2;
                    break;
                default:
                    throw new ArgumentException(failure);
            }
            var response = batch.Handle(request);
            Assert.That(response.ok, Is.False);
            Assert.That(response.environments, Is.Null);
            Assert.That(a.Steps, Is.Zero);
            Assert.That(b.Steps, Is.Zero);
            Assert.That(a.Position, Is.Zero);
            Assert.That(b.Position, Is.Zero);
            Assert.That(a.Resets, Is.EqualTo(1));
            Assert.That(b.Resets, Is.EqualTo(1));
        }

        [TestCase("null_option")]
        [TestCase("null_key")]
        [TestCase("duplicate_key")]
        public void Batch_InvalidResetOptionsNeverInvokeAnyEnvironmentHooks(string failure)
        {
            var a = new Environment();
            var b = new Environment();
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            batch.Handle(new TrainingRequest { command = "start" });
            var request = Reset();
            var option = new TrainingEntry { key = "key", value = "value" };
            request.environments[1].options =
                failure == "null_option" ? new TrainingEntry[] { null }
                : failure == "null_key" ? new[] { new TrainingEntry() }
                : new[] { option, option };
            Assert.That(batch.Handle(request).ok, Is.False);
            Assert.That(a.SeedCalls + b.SeedCalls, Is.Zero);
            Assert.That(a.OptionCalls + b.OptionCalls, Is.Zero);
            Assert.That(a.Resets + b.Resets, Is.Zero);
        }

        [TestCase("step")]
        [TestCase("reward")]
        [TestCase("reset")]
        [TestCase("seed")]
        [TestCase("options")]
        [TestCase("same_step_reset")]
        [TestCase("next_step_reset")]
        public void Batch_PartialFailureRequiresFullResetBeforeRetry(string failure)
        {
            var a = new Environment { Goal = 10 };
            var b = new Environment { Goal = failure.EndsWith("step_reset") ? 0.25f : 10 };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            batch.Handle(
                new TrainingRequest
                {
                    command = "start",
                    autoreset = failure == "next_step_reset" ? 1 : 0,
                }
            );
            batch.Handle(Reset());
            var step = new TrainingRequest
            {
                command = "step",
                environments = new[] { Entry(0, 0.25f), Entry(1, 0.25f) },
            };
            if (failure == "next_step_reset")
                Assert.That(batch.Handle(step).ok, Is.True);
            b.FailStep = failure == "step";
            b.BadReward = failure == "reward";
            b.FailReset = failure == "reset" || failure.EndsWith("step_reset");
            b.FailSeed = failure == "seed";
            b.FailOptions = failure == "options";
            var reset = Reset();
            reset.environments[1].hasSeed = true;
            var failing =
                failure == "reset" || failure == "seed" || failure == "options" ? reset : step;
            var response = batch.Handle(failing);
            Assert.That(response.ok, Is.False);
            Assert.That(response.error, Does.Contain("Environment 1"));
            Assert.That(response.environments, Is.Null);
            var steps = a.Steps + b.Steps;
            Assert.That(batch.Handle(step).ok, Is.False);
            Assert.That(a.Steps + b.Steps, Is.EqualTo(steps));
            b.FailStep = b.BadReward = b.FailReset = b.FailSeed = b.FailOptions = false;
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            Assert.That(a.Position, Is.Zero);
            Assert.That(b.Position, Is.Zero);
            Assert.That(batch.Handle(step).ok, Is.True);
        }

        [Test]
        public void Batch_ValidatesAllActionsBeforeSteppingAndRequiresResetAfterFailure()
        {
            var a = new Environment { Goal = 10 };
            var b = new Environment { Goal = 10 };
            var batch = new TrainingBatchSession(
                new IMultiAgentEnvironment[]
                {
                    new SingleAgentEnvironmentAdapter(a),
                    new SingleAgentEnvironmentAdapter(b),
                }
            );
            batch.Handle(new TrainingRequest { command = "start" });
            batch.Handle(Reset());
            var invalid = batch.Handle(
                new TrainingRequest
                {
                    command = "step",
                    environments = new[] { Entry(0, 0.5f), Entry(1, 2) },
                }
            );
            Assert.That(invalid.ok, Is.False);
            Assert.That(a.Steps, Is.Zero);
            Assert.That(b.Steps, Is.Zero);
            var valid = new TrainingRequest
            {
                command = "step",
                environments = new[] { Entry(0, 0.5f), Entry(1, 0.25f) },
            };
            Assert.That(batch.Handle(valid).ok, Is.False);
            Assert.That(batch.Handle(Reset()).ok, Is.True);
            Assert.That(batch.Handle(valid).ok, Is.True);
            Assert.That(a.Position, Is.EqualTo(0.5f));
            Assert.That(b.Position, Is.EqualTo(0.25f));
        }
    }
}
