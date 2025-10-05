using TMPro;
using UnityEngine;
using Simulator.SignalTiming;
using Simulator.TrafficSignal;

public class SimulationStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statsText;

    [Header("Data Sources")]
    public TrafficLightSetup observedIntersection;
    public TrafficSignalMlAgent mlAgent;

    [Header("Settings")]
    public float updateFrequency = 0.1f; // Update every 0.1 seconds

    private float lastUpdateTime;

    void Start()
    {
        if (observedIntersection == null)
            observedIntersection = FindFirstObjectByType<TrafficLightSetup>();

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

    private void UpdateStatsDisplay()
    {
        if (observedIntersection == null || statsText == null)
        {
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
        // var dataCalc = observedIntersection.GetComponent<IntersectionDataCalculator>();
        int totalThroughput = mlAgent != null
            ? mlAgent.GetTotalVehicles()
            : 0;

        // Display without penalty
        statsText.text =
            $"<b>Algo:</b> {algorithm}  |  <b>Phase:</b> {currentPhase}  |  <b>Timing:</b> {phaseTiming:F1}s\n" +
            $"<b>Reward:</b> {reward:F2}  |  <b>Throughput:</b> {totalThroughput}";
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
