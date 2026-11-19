// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola
{
    /// <summary>
    /// Actuator contract mirroring Unreal IScholaActuator:
    /// InitActuator, GetActionSpace, TakeAction.
    /// Implementations assigned to ScholaAgent must also be MonoBehaviour components.
    /// </summary>
    public interface IActuator
    {
        /// <summary>Stable, nonblank key, unique among this agent's actuators (ordinal, case-sensitive).</summary>
        string ActionKey { get; }

        void InitActuator();

        Space GetActionSpace();

        void TakeAction(Point action);
    }
}
