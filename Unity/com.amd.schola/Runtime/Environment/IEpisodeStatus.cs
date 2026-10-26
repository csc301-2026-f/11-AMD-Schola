// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

namespace AMD.Schola
{
    /// <summary>
    /// Per-step episode-end hook. Called after act/reward and before observe.
    /// <c>terminated</c> and <c>truncated</c> are independent, matching proto/Python.
    /// </summary>
    public interface IEpisodeStatus
    {
        void GetStatus(out bool terminated, out bool truncated);
    }
}
