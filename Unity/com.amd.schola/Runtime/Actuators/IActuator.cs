// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola
{
    /// <summary>
    /// Actuator contract mirroring Unreal IScholaActuator:
    /// InitActuator, GetActionSpace, TakeAction.
    /// </summary>
    public interface IActuator
    {
        void InitActuator();

        Space GetActionSpace();

        void TakeAction(Point action);
    }
}
