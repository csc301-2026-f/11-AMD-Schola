// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace AMD.Schola
{
    /// <summary>
    /// Single-agent training environment contract, mirroring Unreal
    /// <c>ISingleAgentScholaEnvironment</c> with C# return values instead of out-params.
    /// </summary>
    public interface IEnvironment
    {
        InteractionDefinition Initialize();

        void Seed(int seed);

        void SetOptions(IReadOnlyDictionary<string, string> options);

        InitialAgentState Reset();

        AgentState Step(Point action);
    }
}
