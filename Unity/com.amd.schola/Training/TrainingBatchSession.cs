// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace AMD.Schola.Training
{
    /// <summary>Routes one main-thread request to an ordered set of environment sessions.</summary>
    public sealed class TrainingBatchSession
    {
        readonly TrainingSession[] m_Sessions;
        readonly TrainingAgentState[][] m_CompletedStates;
        bool m_NeedsReset = true;
        bool m_Started;
        int m_AutoReset;

        public TrainingBatchSession(IReadOnlyList<IMultiAgentEnvironment> environments)
        {
            if (environments == null || environments.Count == 0)
                throw new ArgumentException(
                    "Register at least one environment.",
                    nameof(environments)
                );
            m_Sessions = new TrainingSession[environments.Count];
            m_CompletedStates = new TrainingAgentState[environments.Count][];
            for (var i = 0; i < environments.Count; i++)
                m_Sessions[i] = new TrainingSession(environments[i]);
        }

        public TrainingResponse Handle(TrainingRequest request)
        {
            try
            {
                if (request == null || request.version != TrainingWire.Version)
                    throw new ArgumentException(
                        TrainingWire.VersionMismatch(request?.version ?? 0)
                    );
                switch (request.command)
                {
                    case "start":
                        m_Started = false;
                        if (request.autoreset < 0 || request.autoreset > 2)
                            throw new ArgumentException("Unknown autoreset mode.");
                        break;
                    case "close":
                        m_Started = false;
                        break;
                    case "definition":
                    case "reset":
                    case "step":
                        if (!m_Started)
                            throw new InvalidOperationException(
                                "Start every environment successfully first."
                            );
                        break;
                    default:
                        throw new ArgumentException("Unknown training command.");
                }
                // Keep the version-2 single-environment message shape usable.
                if (m_Sessions.Length == 1 && request.environments == null)
                {
                    if (request.command == "step" && m_NeedsReset)
                        throw new InvalidOperationException(
                            "Reset all environments before stepping."
                        );
                    var single = m_Sessions[0].Handle(request);
                    if (!single.ok)
                        m_NeedsReset = true;
                    else
                        UpdateLifecycle(request);
                    single.environmentCount = 1;
                    return single;
                }

                var entries =
                    request.command == "step" || request.command == "reset"
                        ? request.environments
                        : null;
                if (request.command == "step" || request.command == "reset")
                {
                    if (entries == null || entries.Length != m_Sessions.Length)
                        throw new ArgumentException("Supply one entry per registered environment.");
                    for (var i = 0; i < entries.Length; i++)
                    {
                        if (entries[i] == null || entries[i].id != i)
                            throw new ArgumentException(
                                "Environment ids must match registration order."
                            );
                        var options = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var option in entries[i].options ?? Array.Empty<TrainingEntry>())
                        {
                            if (option == null || option.key == null || !options.Add(option.key))
                                throw new ArgumentException(
                                    $"Invalid reset options for environment {i}."
                                );
                        }
                    }
                }
                if (request.command == "step")
                {
                    if (m_NeedsReset)
                        throw new InvalidOperationException(
                            "Reset all environments before stepping."
                        );
                    for (var i = 0; i < m_Sessions.Length; i++)
                    {
                        // Like Unreal, a completed environment stays frozen in DISABLED
                        // mode while the remaining environments continue stepping.
                        if (m_CompletedStates[i] == null)
                            m_Sessions[i].ValidateStep(entries[i].actions);
                    }
                }

                var response = new TrainingResponse
                {
                    environmentCount = m_Sessions.Length,
                    environments = new TrainingEnvironmentResponse[m_Sessions.Length],
                };
                for (var i = 0; i < m_Sessions.Length; i++)
                {
                    if (request.command == "step" && m_CompletedStates[i] != null)
                    {
                        response.environments[i] = new TrainingEnvironmentResponse
                        {
                            id = i,
                            states = CopyStates(m_CompletedStates[i]),
                        };
                        continue;
                    }
                    var entry = entries == null ? null : entries[i];
                    var result = m_Sessions[i]
                        .Handle(
                            new TrainingRequest
                            {
                                command = request.command,
                                autoreset = request.autoreset,
                                hasSeed = entry?.hasSeed ?? false,
                                seed = entry?.seed ?? 0,
                                options = entry?.options,
                                actions = entry?.actions,
                            }
                        );
                    if (!result.ok)
                        throw new InvalidOperationException($"Environment {i}: {result.error}");
                    if (request.command == "step" && m_AutoReset == 2 && m_Sessions[i].IsCompleted)
                        m_CompletedStates[i] = CopyStates(result.states);
                    response.environments[i] = new TrainingEnvironmentResponse
                    {
                        id = i,
                        agents = result.agents,
                        states = result.states,
                        hasInitial = result.hasInitial,
                        initialStates = result.initialStates,
                    };
                }
                UpdateLifecycle(request);
                return response;
            }
            catch (Exception error)
            {
                m_NeedsReset = true;
                return new TrainingResponse { ok = false, error = error.Message };
            }
        }

        void UpdateLifecycle(TrainingRequest request)
        {
            if (request.command == "reset")
                m_NeedsReset = false;
            else if (request.command == "start" || request.command == "close")
            {
                m_NeedsReset = true;
                m_Started = request.command == "start";
                m_AutoReset = request.autoreset;
            }
            else
                return;
            Array.Clear(m_CompletedStates, 0, m_CompletedStates.Length);
        }

        static TrainingAgentState[] CopyStates(TrainingAgentState[] states)
        {
            return Array.ConvertAll(
                states,
                state => new TrainingAgentState
                {
                    id = state.id,
                    observations = TrainingWire.EncodePoint(
                        TrainingWire.DecodePoint(state.observations)
                    ),
                    reward = state.reward,
                    terminated = state.terminated,
                    truncated = state.truncated,
                    info = Array.ConvertAll(
                        state.info ?? Array.Empty<TrainingEntry>(),
                        entry => new TrainingEntry { key = entry.key, value = entry.value }
                    ),
                }
            );
        }
    }
}
