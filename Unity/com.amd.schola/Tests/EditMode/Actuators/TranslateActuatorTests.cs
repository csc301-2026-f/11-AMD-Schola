// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using AMD.Schola;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Actuators
{
    /// <summary>
    /// US2 AC1/AC2 TranslateActuator tests.
    /// Valid BoxPoint deltas apply to the transform; invalid actions throw clear errors.
    /// </summary>
    public sealed class TranslateActuatorTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void TakeAction_ValidBoxPoint_TranslatesTransform()
        {
            var go = new GameObject("TranslateActuator_Valid");
            try
            {
                go.transform.position = new Vector3(1f, 2f, 3f);

                var actuator = go.AddComponent<TranslateActuator>();
                actuator.InitActuator();

                var action = new BoxPoint
                {
                    Values = { 0.5f, -1f, 2f },
                };

                actuator.TakeAction(action);

                var expected = new Vector3(1.5f, 1f, 5f);
                Assert.That(go.transform.position.x, Is.EqualTo(expected.x).Within(Tolerance));
                Assert.That(go.transform.position.y, Is.EqualTo(expected.y).Within(Tolerance));
                Assert.That(go.transform.position.z, Is.EqualTo(expected.z).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TakeAction_WrongPointType_ThrowsClearError()
        {
            var go = new GameObject("TranslateActuator_WrongType");
            try
            {
                var actuator = go.AddComponent<TranslateActuator>();
                actuator.InitActuator();

                var wrong = new DictPoint();

                var ex = Assert.Throws<ArgumentException>(() => actuator.TakeAction(wrong));
                Assert.That(ex.Message, Does.Contain("BoxPoint").IgnoreCase,
                    "Error must mention the expected BoxPoint type");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TakeAction_WrongDimensionCount_ThrowsClearError()
        {
            var go = new GameObject("TranslateActuator_WrongDims");
            try
            {
                var actuator = go.AddComponent<TranslateActuator>();
                actuator.InitActuator();

                var wrong = new BoxPoint
                {
                    Values = { 1f, 2f },
                };

                var ex = Assert.Throws<ArgumentException>(() => actuator.TakeAction(wrong));
                Assert.That(ex.Message, Does.Contain("3"),
                    "Error must mention the expected dimension count of 3");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
