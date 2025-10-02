using UnityEngine;
using Simulator.TrafficSignal;
using Simulator.ScriptableObject;

[RequireComponent(typeof(TrafficLightSetup), typeof(IntersectionDataCalculator))]
public class StaticSignalController : MonoBehaviour {
    
    [Header("CSV Logging")]
    public bool enableLogging = true;
    
    [Header("Interval Logging Configuration")]
    [SerializeField] private float loggingInterval = 10f;  // Adjustable interval in seconds
    
    [Header("Static Timing Configuration")]
    public StaticSignalTimingSO staticSignalAlgorithm;
    
    private CsvLogger episodeLogger;
    private CsvLogger intervalLogger;
    private TrafficLightSetup trafficLightSetup;
    private IntersectionDataCalculator intersectionDataCalculator;
    
    private int episodeCounter = 0;
    private float episodeStartTime;
    private float simulationStartTime;
    private float lastIntervalLogTime = 0f;
    
    void Start() {
        trafficLightSetup = GetComponent<TrafficLightSetup>();
        intersectionDataCalculator = GetComponent<IntersectionDataCalculator>();
        
        // Create default static algorithm if none assigned
        if (staticSignalAlgorithm == null) {
            staticSignalAlgorithm = ScriptableObject.CreateInstance<StaticSignalTimingSO>();
        }
        
        if (enableLogging) {
            InitializeLoggers();
        }
        
        simulationStartTime = Time.time;
        StartEpisode();
    }
    
    void InitializeLoggers() {
        // Initialize interval logger
        episodeLogger = new CsvLogger("static_episode_results.csv", 
            "Episode", 
            "TotalVehicles", 
            "VehiclesWaiting", 
            "EpisodeDuration",
            "Throughput",
            "CurrentPhase",
            "PhaseGreenTime");
            
        // Initialize interval logger
        intervalLogger = new CsvLogger("static_interval_data.csv",
            "SimulationTime",
            "Episode",
            "TotalVehicles",
            "QueueLength",
            "Throughput",
            "CurrentPhase",
            "PhaseGreenTime");
            // "VehiclesDeparted",
            // "TrafficDensity");
    }
    
    void StartEpisode() {
        episodeCounter++;
        episodeStartTime = Time.time;
        lastIntervalLogTime = 0f;
        
        // Start logging coroutine
        if (enableLogging) {
            StartCoroutine(LogMetrics());
        }
    }
    
    System.Collections.IEnumerator LogMetrics() {
        int stepCounter = 0;
        
        while (true) {
            yield return new WaitForSeconds(1f); // Log every second
            stepCounter++;
            
            // Check if it's time for interval logging
            float currentSimulationTime = Time.time - simulationStartTime;
            if (currentSimulationTime >= lastIntervalLogTime + loggingInterval) {
                LogIntervalData(currentSimulationTime);
                lastIntervalLogTime = currentSimulationTime;
            }
            
            // Log episode data every 30 seconds (simulate episodes)
            if (stepCounter % 30 == 0) {
                LogEpisodeData();
            }
        }
    }

    // Method to log interval data
    void LogIntervalData(float currentSimulationTime) {
        float throughput = CalculateThroughput();
        // int vehiclesDeparted = CalculateVehiclesDeparted();
        // float trafficDensity = CalculateTrafficDensity();
        
        intervalLogger?.LogRow(
            currentSimulationTime,
            episodeCounter,
            intersectionDataCalculator.TotalNumberOfVehicles,
            intersectionDataCalculator.TotalNumberOfVehiclesWaitingInIntersection,
            throughput,
            trafficLightSetup.CurrentPhaseIndex,
            trafficLightSetup.Phases[trafficLightSetup.CurrentPhaseIndex].greenLightTime
            // vehiclesDeparted,
            // trafficDensity
        );
        
        // Debug.Log($"Interval data logged at simulation time: {currentSimulationTime:F1}s");
    }

    void LogEpisodeData() {
        // float avgWaitTime = CalculateAverageWaitTime();
        float throughput = CalculateThroughput();
        // float fuel = intersectionDataCalculator.totalFuelConsumed;

        episodeLogger?.LogRow(
            episodeCounter,
            intersectionDataCalculator.TotalNumberOfVehicles,
            intersectionDataCalculator.TotalNumberOfVehiclesWaitingInIntersection,
            Time.time - episodeStartTime,
            throughput,
            trafficLightSetup.CurrentPhaseIndex,
            trafficLightSetup.Phases[trafficLightSetup.CurrentPhaseIndex].greenLightTime
        );
        
        episodeCounter += 1;
        episodeStartTime = Time.time; // optional: reset episode clock
        intersectionDataCalculator.totalFuelConsumed = 0f;
    }

    // float CalculateAverageWaitTime() {
    //     // Calculate average wait time from intersection data
    //     if (intersectionDataCalculator.vehiclesWaitingAtLeg == null) return 0f;

    //     float totalWaitTime = 0f;
    //     int totalVehicles = 0;

    //     foreach (var leg in intersectionDataCalculator.vehiclesWaitingAtLeg) {
    //         if (leg != null) {
    //             foreach (var vehicle in leg.Values) {
    //                 totalWaitTime += vehicle;
    //                 totalVehicles++;
    //             }
    //         }
    //     }

    //     return totalVehicles > 0 ? totalWaitTime / totalVehicles : 0f;
    // }
    
    // Calculate current throughput
    float CalculateThroughput() {
        float elapsedTime = Time.time - episodeStartTime;
        return elapsedTime > 0 ? intersectionDataCalculator.TotalNumberOfVehicles / elapsedTime : 0f;
    }
    
    // Public method to change logging interval at runtime
    public void SetLoggingInterval(float newInterval) {
        loggingInterval = newInterval;
        // Debug.Log($"Logging interval changed to {newInterval} seconds");
    }
    
    void OnApplicationQuit() {
        SaveAllData();
    }
    
    void OnDestroy() {
        SaveAllData();
    }
    
    void SaveAllData() {
        episodeLogger?.SaveToFile();
        intervalLogger?.SaveToFile();
    }
    
    [ContextMenu("Save Static CSV Data")]
    public void ManualSave() {
        SaveAllData();
    }
}
