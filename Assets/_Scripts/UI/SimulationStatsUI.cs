using TMPro;
using UnityEngine;
using Simulator.SignalTiming;
using Simulator.TrafficSignal;
using System.Linq; // Added for easy array formatting

public class SimulationStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI agentInputsText;

    [Header("Data Sources")]
    public TrafficLightSetup observedIntersection;
    public TrafficSignalMlAgent mlAgent;

    [Header("Settings")]
    public float updateFrequency = 0.1f; // Update every 0.1 seconds

    private float lastUpdateTime;
    private float simulationStartTime;

    void Start()
    {
        simulationStartTime = Time.time;

        if (observedIntersection == null)
            observedIntersection = FindAnyObjectByType<TrafficLightSetup>();

        if (mlAgent == null && observedIntersection != null)
            mlAgent = observedIntersection.GetComponent<TrafficSignalMlAgent>();
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateFrequency)
        {
            UpdateStatsDisplay();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateStatsDisplay() {
        if (observedIntersection == null || statsText == null) {
            statsText.text = "No data source found";
            return;
        }

        // 1. Algorithm Name
        string algorithm = GetAlgorithmName();

        // 2. Current Phase
        int currentPhase = observedIntersection.CurrentPhaseIndex;

        // 3. Phase Timing decided at phase change
        float phaseTiming = observedIntersection.GetGreenLightTime();

        // 4. Cumulative Reward
        float reward = mlAgent != null
            ? mlAgent.GetReward()
            : -1f;

        // 5. Total Throughput
        int totalThroughput = observedIntersection != null
            ? observedIntersection.GetVehiclesCleared()
            : 0;

        int timePassed = observedIntersection != null
            ? observedIntersection.GetTimePassed()
            : 0;

        float throughput = timePassed > 0 ? (float)totalThroughput / timePassed * 60 : 0f;

        // Display without penalty
        if (algorithm == "Static") {
            statsText.text =
            $"<b>Algorithm: </b> Fixed-time plan\n" +
            $"<b>Phase: </b> {currentPhase}\n" +
            $"<b>Timing: </b> {phaseTiming:F1}s\n" +
            $"<b>Vehicles Cleared: </b> {totalThroughput}\n" +
            $"<b>Throughput: </b> {throughput:F2} v/min";
        }
        else if (algorithm == "PPO") {
            statsText.text =
            $"<b>Algorithm: </b> ML Agent Controller\n" +
            $"<b>Phase: </b> {currentPhase}\n" +
            $"<b>Timing: </b> {phaseTiming:F1}s\n" +
            $"<b>Reward: </b> {reward:F2}\n" +
            $"<b>Vehicles Cleared: </b> {totalThroughput}\n" +
            $"<b>Throughput: </b> {throughput:F2} v/min";
        }
        else if (algorithm == "Dynamic") {
            statsText.text =
            $"<b>Algorithm: </b> Dynamic time plan\n" +
            $"<b>Phase: </b> {currentPhase}\n" +
            $"<b>Timing: </b> {phaseTiming:F1}s\n" +
            $"<b>Vehicles Cleared: </b> {totalThroughput}\n" +
            $"<b>Throughput: </b> {throughput:F2} v/min";
        }
        else {
            statsText.text =
            $"<b>Algorithm: </b> {algorithm}\n" +
            $"<b>Phase: </b> {currentPhase}\n" +
            $"<b>Timing: </b> {phaseTiming:F1}s\n" +
            $"<b>Reward: </b> {reward:F2}\n" +
            $"<b>Vehicles Cleared: </b> {totalThroughput}\n" +
            $"<b>Throughput: </b> {throughput:F2} v/min";
        }

        // --- NEW AGENT OBSERVATIONS PANEL ---
        if (agentInputsText != null)
        {
            // Calculate time
            float timeSinceStart = Time.time - simulationStartTime;
            int minutes = Mathf.FloorToInt(timeSinceStart / 60F);
            int seconds = Mathf.FloorToInt(timeSinceStart - minutes * 60);

            // Fetch intersection data for human-readable stats
            var dataCalc = observedIntersection.GetComponent<IntersectionDataCalculator>();
            int totalVehiclesWaiting = 0;
            string queuesStr = "N/A";
            string waitTimesStr = "N/A";

            if (dataCalc != null) {
                // Sum all queues safely
                totalVehiclesWaiting = dataCalc.LiveQueueLengths[0] + dataCalc.LiveQueueLengths[1] + 
                                       dataCalc.LiveQueueLengths[2] + dataCalc.LiveQueueLengths[3];
                
                queuesStr = $"[{dataCalc.LiveQueueLengths[0]}, {dataCalc.LiveQueueLengths[1]}, {dataCalc.LiveQueueLengths[2]}, {dataCalc.LiveQueueLengths[3]}]";
                waitTimesStr = $"[{dataCalc.GetMaxWaitTime(0):F0}s, {dataCalc.GetMaxWaitTime(1):F0}s, {dataCalc.GetMaxWaitTime(2):F0}s, {dataCalc.GetMaxWaitTime(3):F0}s]";
            }

            // Fetch the raw ML-Agent normalized array
            string obsArrayStr = "Waiting for agent initialization...";
            if (mlAgent != null && mlAgent.Ml_data != null && mlAgent.Ml_data.observations != null && mlAgent.Ml_data.observations.Length > 0) {
                // Convert the float array to a string with 2 decimal places to keep it tidy
                var formattedObs = System.Array.ConvertAll(mlAgent.Ml_data.observations, x => x.ToString("F2"));
                obsArrayStr = string.Join(", ", formattedObs);
            }

            // Update New Panel
            agentInputsText.text = 
                $"<b><color=#5A9BD5>Human Readable State</color></b>\n" +
                $"<b>Sim Time: </b> {minutes:00}:{seconds:00}\n" +
                $"<b>Current Phase: </b> {currentPhase}\n" +
                $"<b>Total Queue: </b> {totalVehiclesWaiting}\n" +
                $"<b>Leg Queues: </b> {queuesStr}\n" +
                $"<b>Max Waits: </b> {waitTimesStr}\n\n" +
                $"<b><color=#5A9BD5>ML Normalized Array</color></b>\n" +
                $"<size=85%>[{obsArrayStr}]</size>";
        }
    }

    private string GetAlgorithmName()
    {
        switch (observedIntersection.signalTimingAlgorithmType)
        {
            case TrafficSignalAlogrithm.Static:
                return "Static";
            case TrafficSignalAlogrithm.SignalOptimizationML:
                return "PPO";
            case TrafficSignalAlogrithm.Dynamic:
                return "Dynamic";
            case TrafficSignalAlogrithm.PhaseOptimizationML:
                return "PPO Phase";
            default:
                return "Unknown";
        }
    }
}