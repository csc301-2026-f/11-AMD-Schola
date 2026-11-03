// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AMD.Schola.Tests.Policies
{
    /// <summary>
    /// Tests that SentisPolicy clamps Box actions to their bounds.
    /// </summary>
    public sealed class SentisPolicyBoxActionTests
    {
        const string ModelPath =
            "Packages/com.amd.schola/Tests/EditMode/Policies/TestModels/reach_target.onnx";
        const float Bound = 0.1f;

        ModelAsset m_ModelAsset;
        SentisPolicy m_Policy;

        [SetUp]
        public void SetUp()
        {
            m_ModelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
            Assert.That(m_ModelAsset, Is.Not.Null, $"Test model missing at {ModelPath}");
            m_Policy = new SentisPolicy(m_ModelAsset);
        }

        [TearDown]
        public void TearDown()
        {
            m_Policy.Dispose();
        }

        [Test]
        public void Init_TwoObservationInputs_Succeeds()
        {
            Assert.That(m_Policy.Init(ReachTargetDefinition(Bound)), Is.True);
        }

        [Test]
        public void Init_MissingSecondObservation_FailsWithClearError()
        {
            var definition = ReachTargetDefinition(Bound);
            ((DictSpace)definition.ObservationSpace).Spaces.Remove("target_position");

            LogAssert.Expect(
                LogType.Error,
                new Regex("Model expects input 'target_position' but no observation has that key")
            );
            Assert.That(m_Policy.Init(definition), Is.False);
        }

        [Test]
        public void Think_BoundedBoxAction_IsClampedToBounds()
        {
            Assert.That(m_Policy.Init(ReachTargetDefinition(Bound)), Is.True);

            var translate = Think(new Vector3(2f, 0f, 0f), Vector3.zero);

            Assert.That(translate.Values.Count, Is.EqualTo(3));
            foreach (var value in translate.Values)
            {
                Assert.That(value, Is.InRange(-Bound, Bound));
            }

            // The raw model output for x is about -2.4, so it is clamped to the lower bound.
            Assert.That(translate.Values[0], Is.EqualTo(-Bound));
        }

        [Test]
        public void Think_UnboundedBoxAction_IsNotClamped()
        {
            Assert.That(m_Policy.Init(ReachTargetDefinition(null)), Is.True);

            var translate = Think(new Vector3(2f, 0f, 0f), Vector3.zero);

            Assert.That(translate.Values[0], Is.LessThan(-Bound));
        }

        BoxPoint Think(Vector3 agent, Vector3 target)
        {
            var observations = new DictPoint
            {
                Points =
                {
                    ["agent_position"] = new BoxPoint
                    {
                        Values = new List<float> { agent.x, agent.y, agent.z },
                    },
                    ["target_position"] = new BoxPoint
                    {
                        Values = new List<float> { target.x, target.y, target.z },
                    },
                },
            };

            Assert.That(m_Policy.Think(observations, out var actions), Is.True);
            var translate = ((DictPoint)actions).Points["translate"] as BoxPoint;
            Assert.That(translate, Is.Not.Null, "Box action space must produce a BoxPoint");
            return translate;
        }

        static InteractionDefinition ReachTargetDefinition(float? bound)
        {
            var action = new BoxSpace();
            for (var i = 0; i < 3; i++)
            {
                if (bound.HasValue)
                {
                    action.Dimensions.Add(new BoxSpaceDimension(-bound.Value, bound.Value));
                }
                else
                {
                    action.Dimensions.Add(new BoxSpaceDimension());
                }
            }

            return new InteractionDefinition(
                new DictSpace
                {
                    Spaces =
                    {
                        ["agent_position"] = Position(),
                        ["target_position"] = Position(),
                    },
                },
                new DictSpace { Spaces = { ["translate"] = action } }
            );
        }

        static BoxSpace Position()
        {
            var space = new BoxSpace();
            for (var i = 0; i < 3; i++)
            {
                space.Dimensions.Add(new BoxSpaceDimension());
            }

            return space;
        }
    }
}
