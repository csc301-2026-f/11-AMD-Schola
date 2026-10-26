// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola {
    /// <summary>
    /// Something a stepper can drive: it reports what it currently observes and
    /// carries out the actions a policy chooses for it.
    /// </summary>
    public interface IAgent {

        /// <summary>
        /// Collects what the agent currently sees. Typically a <see cref="DictPoint"/> keyed by
        /// sensor key, matching the observation space the policy was initialised with.
        /// </summary>
        Point Observe();

        /// <summary>
        /// Carries out an action chosen by a policy. Typically a <see cref="DictPoint"/> keyed by
        /// action key, in the same shape that <see cref="IPolicy.Think"/> returns.
        /// </summary>
        void Act(Point action);

    }

}