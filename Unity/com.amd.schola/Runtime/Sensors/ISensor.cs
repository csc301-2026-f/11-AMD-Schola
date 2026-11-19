// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola
{
    /// <summary>
    /// Sensor contract mirroring Unreal IScholaSensor:
    /// InitSensor, GetObservationSpace, CollectObservations.
    /// Implementations assigned to ScholaAgent must also be MonoBehaviour components.
    /// </summary>
    public interface ISensor
    {
        /// <summary>Stable, nonblank key, unique among this agent's sensors (ordinal, case-sensitive).</summary>
        string ObservationKey { get; }

        void InitSensor();

        Space GetObservationSpace();

        Point CollectObservations();
    }
}
