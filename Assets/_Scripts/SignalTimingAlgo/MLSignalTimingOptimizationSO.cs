// Assets\_Scripts\SignalTimingAlgo\MLSignalTimingOptimizationSO.cs

using Simulator.SignalTiming;
using Simulator.TrafficSignal;
using UnityEngine;

namespace Simulator.ScriptableObject {
    [CreateAssetMenu(menuName = "ScriptableObjects/MLAlgorithm/MLSignalTImingOptimization", fileName = "DefaultMLSignalTImingOptimization", order = 2)]
    internal class MLSignalTimingOptimizationSO : UnityEngine.ScriptableObject {

        // public void CalculateRewards(IntersectionDataCalculator intersectionDataCalculator, ML_DATA ml_data) {
        //     TrafficLightSetup setup = intersectionDataCalculator.GetComponent<TrafficLightSetup>();
            
        //     // ==========================================
        //     // 1. CALCULATE REWARD: R_t = -Sum(Queue_Lengths)
        //     // ==========================================
        //     float totalQueueLength = 0f;
        //     for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
        //         totalQueueLength += intersectionDataCalculator.LiveQueueLengths[i];  //.GetQueueLength(i);
        //     }
            
        //     // Assign the negative sum as the reward
        //     ml_data.rewards = -totalQueueLength;

        //     // ==========================================
        //     // 2. BUILD OBSERVATION SPACE
        //     // ==========================================
        //     int obsIndex = ml_data.OFSET;

        //     // A. Queue Length per Leg (4 floats)
        //     for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
        //         ml_data.observations[obsIndex++] = intersectionDataCalculator.LiveQueueLengths[i];  //.GetQueueLength(i);
        //     }

        //     // B. Max Wait Time per Leg (4 floats)
        //     for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
        //         ml_data.observations[obsIndex++] = intersectionDataCalculator.GetMaxWaitTime(i);
        //     }

        //     // C. Current Phase (One-Hot Encoded: N floats)
        //     int currentPhase = setup.CurrentPhaseIndex;
        //     int totalPhases = setup.GetTotalPhases();
        //     for (int i = 0; i < totalPhases; i++) {
        //         ml_data.observations[obsIndex++] = (i == currentPhase) ? 1.0f : 0.0f;
        //     }

        //     // D. Elapsed Time of Current Phase (1 float)
        //     ml_data.observations[obsIndex++] = setup.GetTimePassedInCurrentPhase();
        // }
        public void CalculateRewards(IntersectionDataCalculator intersectionDataCalculator, ML_DATA ml_data) {
            TrafficLightSetup setup = intersectionDataCalculator.GetComponent<TrafficLightSetup>();
            
            // ==========================================
            // 1. CALCULATE REWARD: Delay + Starvation Penalty
            // ==========================================
            float reward = 0f;
            for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
                // float queue = intersectionDataCalculator.LiveQueueLengths[i];
                float queue = intersectionDataCalculator.GetQueueLength(i);
                
                // Linear penalty for total global delay
                reward -= (queue * 0.01f); 
                
                // Exponential starvation penalty (squaring the queue)
                // A queue of 15 cars applies a massive -0.225 penalty compared to 1 car (-0.001)
                reward -= (queue * queue * 0.001f); 
            }
            
            ml_data.rewards = reward;

            // ==========================================
            // 2. BUILD OBSERVATION SPACE (Normalized to 0.0 -> 1.0)
            // ==========================================
            int obsIndex = ml_data.OFSET;

            
            // Normalization maximums (Adjust these based on your intersection size)
            float maxBaseGreenTime = 0f;
            for (int i = 0; i < setup.Phases.Length; i++) {
                if (setup.Phases[i].greenLightTime > maxBaseGreenTime) {
                    maxBaseGreenTime = setup.Phases[i].greenLightTime;
                }
            }
            float maxPhaseTime = maxBaseGreenTime + ml_data.MAXIMUM_GREEN_LIGHT_OFSET;
            
            float maxWaitTime = 2 * (maxPhaseTime + setup.ClearanceTime);
            
            float expectedSecondsPerCar = 2.5f;
            float maxQueueCapacity = maxWaitTime / expectedSecondsPerCar;
            
            // A. Queue Length per Leg (Normalized)
            for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
                // float q = intersectionDataCalculator.LiveQueueLengths[i];
                float queue = intersectionDataCalculator.GetQueueLength(i);
                ml_data.observations[obsIndex++] = Mathf.Clamp01(queue / maxQueueCapacity); 
            }

            // B. Max Wait Time per Leg (Normalized)
            for (int i = 0; i < ml_data.NUM_OF_LEGS; i++) {
                float w = intersectionDataCalculator.GetMaxWaitTime(i);
                ml_data.observations[obsIndex++] = Mathf.Clamp01(w / maxWaitTime);
            }

            // C. Current Phase (One-Hot Encoded)
            int currentPhase = setup.CurrentPhaseIndex;
            int totalPhases = setup.GetTotalPhases();
            for (int i = 0; i < totalPhases; i++) {
                ml_data.observations[obsIndex++] = (i == currentPhase) ? 1.0f : 0.0f;
            }

            // D. Elapsed Time of Current Phase (Normalized)
            float elapsedTime = setup.GetTimePassedInCurrentPhase();
            ml_data.observations[obsIndex++] = Mathf.Clamp01(elapsedTime / maxPhaseTime);
        }
    }
}