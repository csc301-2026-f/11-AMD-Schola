// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMD.Schola
{
    /// <summary>
    /// Runs an <see cref="IStepper"/> from Unity's game loop. Call <see cref="Setup"/> once,
    /// then the runner calls <see cref="IStepper.Step"/> every N frames.
    /// </summary>
    public sealed class StepperRunner : MonoBehaviour
    {
        [SerializeField, Min(1)]
        int m_FramesPerStep = 1;

        IStepper m_Stepper;
        int m_FrameCounter;

        public bool IsRunning => m_Stepper != null;

        /// <summary>
        /// Initialises the stepper and starts running it. The policy must already be initialised.
        /// The runner does not own the policy: if it is IDisposable (like SentisPolicy), the code
        /// that created it must dispose it, for example in OnDestroy.
        /// </summary>
        /// <returns>False if the stepper could not be initialised; nothing runs in that case.</returns>
        public bool Setup(IStepper stepper, IReadOnlyList<IAgent> agents, IPolicy policy)
        {
            m_Stepper = null;
            m_FrameCounter = 0;

            if (stepper == null)
            {
                Debug.LogError($"{nameof(StepperRunner)}.Setup(): No stepper provided.");
                return false;
            }

            if (!stepper.Init(agents, policy))
            {
                return false; // the stepper already logged the reason
            }

            m_Stepper = stepper;
            return true;
        }

        /// <summary>
        /// Stops stepping. Setup must be called again to resume.
        /// </summary>
        public void StopRunning()
        {
            m_Stepper = null;
        }

        void Update()
        {
            if (m_Stepper == null)
            {
                return;
            }

            m_FrameCounter++;
            if (m_FrameCounter < m_FramesPerStep)
            {
                return;
            }

            m_FrameCounter = 0;

            try
            {
                if (m_Stepper.Step())
                {
                    return;
                }

                Debug.LogError($"{nameof(StepperRunner)}: Step failed, stopping.");
            }
            catch (Exception e)
            {
                // An agent or policy threw. Log it once and stop, instead of repeating every frame.
                Debug.LogException(e, this);
                Debug.LogError($"{nameof(StepperRunner)}: Step threw, stopping.");
            }

            m_Stepper = null;
        }
    }
}
