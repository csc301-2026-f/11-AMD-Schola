// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace AMD.Schola
{
    /// <summary>
    /// Drives the observe, think, act loop: each step it asks every agent what it sees,
    /// asks the policy what to do, and tells every agent to do it.
    /// </summary>
    public interface IStepper
    {
        /// <summary>
        /// Prepares the stepper to drive the given agents with the given policy.
        /// The policy must already be initialised by the caller.
        /// </summary>
        /// <returns>False if there are no agents, an agent is null, or the policy is null.</returns>
        bool Init(IReadOnlyList<IAgent> agents, IPolicy policy);

        /// <summary>
        /// Runs one observe, think, act cycle for every agent.
        /// </summary>
        /// <returns>False if the stepper is not initialised or the policy fails.</returns>
        bool Step();
    }
}
