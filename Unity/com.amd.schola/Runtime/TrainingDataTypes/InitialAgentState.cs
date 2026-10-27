// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace AMD.Schola
{
    /// <summary>
    /// Reset payload. Matches proto <c>InitialAgentState</c> and Unreal <c>FInitialAgentState</c>:
    /// observations and info only. Separate from <see cref="AgentState"/> so reset cannot carry reward or done flags.
    /// </summary>
    public sealed class InitialAgentState
    {
        public Point Observations { get; set; }

        public Dictionary<string, string> Info { get; set; } = new Dictionary<string, string>();
    }
}
