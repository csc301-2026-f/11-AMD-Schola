// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola
{
    /// <summary>
    /// US4 placeholder. Receives protocol-shaped training payloads so US3 can report
    /// rewards and episode status at the contract layer without gRPC or protobuf.
    /// A Gym connector will implement this (or replace the call site) and serialize to
    /// <c>GymService.UpdateState</c>.
    /// </summary>
    public interface ITrainingStateSink
    {
        void SubmitState(AgentState state);

        void SubmitInitialState(InitialAgentState state);
    }
}
