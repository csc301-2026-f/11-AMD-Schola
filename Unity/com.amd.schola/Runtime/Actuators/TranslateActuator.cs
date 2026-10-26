// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace AMD.Schola
{
    /// <summary>
    /// Scene-backed actuator that applies a 3-float BoxPoint delta to <see cref="Transform.position"/>.
    /// </summary>
    public sealed class TranslateActuator : MonoBehaviour, IActuator
    {
        const int DimensionCount = 3;

        [SerializeField]
        string m_ActionKey = "translate";

        /// <summary>
        /// Stable dict key used when an agent aggregates this actuator.
        /// </summary>
        public string ActionKey
        {
            get => m_ActionKey;
            set => m_ActionKey = value;
        }

        public void InitActuator()
        {
            if (string.IsNullOrEmpty(m_ActionKey))
            {
                Debug.LogWarning(
                    $"{nameof(TranslateActuator)} on '{name}' has an empty {nameof(ActionKey)}.",
                    this
                );
            }
        }

        public Space GetActionSpace()
        {
            var space = new BoxSpace();
            for (var i = 0; i < DimensionCount; i++)
            {
                space.Dimensions.Add(new BoxSpaceDimension());
            }

            return space;
        }

        public void TakeAction(Point action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!(action is BoxPoint boxPoint))
            {
                throw new ArgumentException(
                    $"Expected {nameof(BoxPoint)}, got {action.GetType().Name}.",
                    nameof(action)
                );
            }

            if (boxPoint.Values == null || boxPoint.Values.Count != DimensionCount)
            {
                var actualCount = boxPoint.Values == null ? 0 : boxPoint.Values.Count;
                throw new ArgumentException(
                    $"Action '{m_ActionKey}' expected {nameof(BoxPoint)} with {DimensionCount} values, got {actualCount}.",
                    nameof(action)
                );
            }

            transform.position += new Vector3(
                boxPoint.Values[0],
                boxPoint.Values[1],
                boxPoint.Values[2]
            );
        }
    }
}
