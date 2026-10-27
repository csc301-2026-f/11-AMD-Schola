// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace AMD.Schola.Training
{
    /// <summary>
    /// Main-thread, single-environment session implementing Schola's reset modes.
    /// A step calls US3's synchronous Step once; it does not advance Unity physics.
    /// </summary>
    public sealed class TrainingSession
    {
        readonly IEnvironment m_Environment;
        InteractionDefinition m_Definition;
        bool m_Started;
        bool m_NeedsReset = true;
        bool m_Done;
        int m_AutoReset;

        public TrainingSession(IEnvironment environment)
        {
            m_Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public TrainingResponse Handle(TrainingRequest request)
        {
            try
            {
                if (request == null || request.version != TrainingWire.Version)
                    throw new ArgumentException("Unsupported Unity training protocol version.");
                if (request.command == "close")
                {
                    m_Started = false;
                    m_NeedsReset = true;
                    return new TrainingResponse();
                }
                if (request.command == "start")
                {
                    if (request.autoreset < 0 || request.autoreset > 2)
                        throw new ArgumentException("Unknown autoreset mode.");
                    // Initialize once: reconnecting must not replace the original reset snapshot.
                    if (m_Definition == null)
                        m_Definition = m_Environment.Initialize();
                    TrainingWire.EncodeSpace(m_Definition.ObservationSpace);
                    TrainingWire.EncodeSpace(m_Definition.ActionSpace);
                    m_AutoReset = request.autoreset;
                    m_Started = true;
                    m_NeedsReset = true;
                    m_Done = false;
                    return new TrainingResponse();
                }
                if (!m_Started)
                    throw new InvalidOperationException("Start a training session first.");
                if (request.command == "definition")
                    return new TrainingResponse
                    {
                        observationSpace = TrainingWire.EncodeSpace(m_Definition.ObservationSpace),
                        actionSpace = TrainingWire.EncodeSpace(m_Definition.ActionSpace),
                    };
                if (request.command == "reset")
                {
                    var options = new Dictionary<string, string>();
                    foreach (var option in request.options ?? Array.Empty<TrainingEntry>())
                        options.Add(option.key, option.value);
                    if (request.hasSeed)
                        m_Environment.Seed(request.seed);
                    m_Environment.SetOptions(options);
                    return Reset();
                }
                if (request.command != "step")
                    throw new ArgumentException("Unknown training command.");
                if (m_NeedsReset)
                    throw new InvalidOperationException("Reset the environment before stepping.");
                if (m_Done)
                {
                    if (m_AutoReset == 1)
                        return Reset(); // NEXT_STEP ignores this action.
                    throw new InvalidOperationException(
                        "Episode ended; reset before stepping again."
                    );
                }
                var action = TrainingWire.DecodePoint(request.action);
                TrainingWire.Validate(action, m_Definition.ActionSpace);
                var state = m_Environment.Step(action);
                if (float.IsNaN(state.Reward) || float.IsInfinity(state.Reward))
                    throw new InvalidOperationException("Reward must be finite.");
                var response = new TrainingResponse
                {
                    observations = TrainingWire.EncodePoint(state.Observations),
                    reward = state.Reward,
                    terminated = state.Terminated,
                    truncated = state.Truncated,
                    info = TrainingWire.Entries(state.Info),
                };
                m_Done = state.Terminated || state.Truncated;
                if (m_Done && m_AutoReset == 0)
                {
                    var initial = Reset();
                    response.hasInitial = true;
                    response.initialObservations = initial.observations;
                    response.initialInfo = initial.info;
                }
                return response;
            }
            catch (Exception error)
            {
                // A failed operation can leave user-defined environment hooks partially advanced.
                // Require reset before the next step, rather than silently continuing that episode.
                m_NeedsReset = true;
                return new TrainingResponse { ok = false, error = error.Message };
            }
        }

        TrainingResponse Reset()
        {
            var initial = m_Environment.Reset();
            var response = new TrainingResponse
            {
                observations = TrainingWire.EncodePoint(initial.Observations),
                info = TrainingWire.Entries(initial.Info),
            };
            m_NeedsReset = false;
            m_Done = false;
            return response;
        }
    }
}
