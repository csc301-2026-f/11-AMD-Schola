// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using UnityEngine;

namespace AMD.Schola.Tests.Agent
{
    /// <summary>Independent sensor implementation used to exercise interface composition.</summary>
    public sealed class InterfaceTestSensor : MonoBehaviour, ISensor
    {
        [SerializeField]
        string m_ObservationKey = "category";

        public string ObservationKey
        {
            get => m_ObservationKey;
            set => m_ObservationKey = value;
        }

        public int Value = 2;
        public int ObservationCount { get; private set; }
        public int InitializationCount { get; private set; }
        public int SpaceRequestCount { get; private set; }

        public void InitSensor()
        {
            InitializationCount++;
        }

        public Space GetObservationSpace()
        {
            SpaceRequestCount++;
            return new DiscreteSpace(8);
        }

        public Point CollectObservations()
        {
            ObservationCount++;
            return new DiscretePoint { Value = Value };
        }
    }
}
