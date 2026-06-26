using Simulator.RuntimeData;
using Simulator.ScriptableObject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Simulator.Manager;

namespace Simulator.TrafficSignal {
    [RequireComponent(typeof(TrafficLightSetup))]
    public class IntersectionDataCalculator : MonoBehaviour {
        [SerializeField] private DataGenerationSettingsSO dataGenerationSetting;
        [SerializeField] private int numberOfLegs;
        //[SerializeField] private int[] vehiclesInLeg;

        #region Public Fields
        [field: SerializeField] public int TotalNumberOfVehicles { get; private set; } = 0;
        [field: SerializeField] public int TotalNumberOfVehiclesWaitingInIntersection { get; private set; } = 0;
        
        [Header("Debugging")]
        [SerializeField] private bool logQueueArray = true;
        #endregion


        private int throughput = 0;
        private int vehiclesCleared = 0;
        // private int vehiclesWaiting = 0;
        // Dictionary value: (legIndex, wait time at interection)
        //private readonly Dictionary<VehicleDataCalculator, (int, float)> vehiclesWaitingInIntersection = new();

        // Dictionary value: (wait time at interection)
        public readonly List<Dictionary<VehicleDataCalculator, float>> vehiclesWaitingAtLeg = new();
        public int[] LiveQueueLengths { get; private set; } = new int[4];

        private int waitTimeAtIntersection;

        private string Name;


        // public float totalFuelConsumed = 0f;

        #region Unity Methods
        private void Awake() {
            Name = transform.name;
            //vehiclesInLeg = new int[numberOfLegs];
            for (int i = 0; i < numberOfLegs; i++) {
                vehiclesWaitingAtLeg.Add(new Dictionary<VehicleDataCalculator, float>());
            }
        }
        private void Start() {
            // totalFuelConsumed = 0f;
            StartCoroutine(Tick());
            StartCoroutine(DebugLogQueueArray());   
        }
        #endregion

        private IEnumerator Tick() {
            int lastVehicleCount = 0;
            while (true) {
                yield return new WaitForSeconds(dataGenerationSetting.writeIntersectionThroughputPerNSec);
                throughput = (TotalNumberOfVehicles - lastVehicleCount) / dataGenerationSetting.writeIntersectionThroughputPerNSec;
                StoreData.WriteIntesectionThroughput(Name, throughput);
                lastVehicleCount = TotalNumberOfVehicles;
            }
        }

        // internal void VehicleEntered(VehicleDataCalculator vehicleDataCalculator, int legIndex) {
        //     if (vehiclesWaitingAtLeg[legIndex].ContainsKey(vehicleDataCalculator)) {
        //         vehiclesWaitingAtLeg[legIndex].Add(vehicleDataCalculator, vehicleDataCalculator.TotalWaitTime);

        //     }
        //     //vehiclesWaitingAtLeg[vehicleDataCalculator] = (legIndex, vehicleDataCalculator.TotalWaitTime);
        //     vehiclesWaitingAtLeg[legIndex][vehicleDataCalculator] = vehicleDataCalculator.TotalWaitTime;
        //     TotalNumberOfVehicles++;
        //     //vehiclesInLeg[legIndex]++;
        //     TotalNumberOfVehiclesWaitingInIntersection++;
        //     // Debug.Log($"Vehicle {vehicleDataCalculator.name} entered intersection {Name} at leg {legIndex}");
        //     // Debug.Log($"Entered: Total={TotalNumberOfVehicles}, Waiting={TotalNumberOfVehiclesWaitingInIntersection}");
        //     // Debug.Log($"TotalNumberOfVehicles: {TotalNumberOfVehicles}, TotalNumberOfVehiclesWaitingInIntersection: {TotalNumberOfVehiclesWaitingInIntersection}");
        // }

        internal void VehicleEntered(VehicleDataCalculator vehicleDataCalculator, int legIndex) {
            // 1. Safety Shield: If the car is already in the dictionary, ignore this duplicate trigger!
            if (vehiclesWaitingAtLeg[legIndex].ContainsKey(vehicleDataCalculator)) {
                return; 
            }

            // 2. Safely add or update the vehicle's wait time
            vehiclesWaitingAtLeg[legIndex][vehicleDataCalculator] = vehicleDataCalculator.TotalWaitTime;
            
            // 3. Update the global counts
            TotalNumberOfVehicles++;
            TotalNumberOfVehiclesWaitingInIntersection++;
        }

        internal void VehicleExited(VehicleDataCalculator vehicleDataCalculator) {
            for (int i = 0; i < numberOfLegs; i++) {
                if (vehiclesWaitingAtLeg[i].ContainsKey(vehicleDataCalculator)) {
                    float t = vehiclesWaitingAtLeg[i][vehicleDataCalculator];
                    vehiclesWaitingAtLeg[i].Remove(vehicleDataCalculator);
                    waitTimeAtIntersection = Mathf.RoundToInt(vehicleDataCalculator.TotalWaitTime - t);
                    StoreData.WriteIntesectionWaitTime(Name, vehicleDataCalculator.name, waitTimeAtIntersection);
                    TotalNumberOfVehiclesWaitingInIntersection--;
                    vehiclesCleared++;
                    // Debug.Log($"Vehicle {vehicleDataCalculator.name} exited intersection {Name} from leg {i}. Wait time at intersection: {waitTimeAtIntersection} seconds");
                    // Debug.Log($"Vehicles Cleared: {vehiclesCleared}, TotalNumberOfVehiclesWaitingInIntersection: {TotalNumberOfVehiclesWaitingInIntersection}");
                    // Debug.Log($"Exited: Waiting={TotalNumberOfVehiclesWaitingInIntersection}");
                    // totalFuelConsumed += vehicleDataCalculator.FuelUsed;
                    GameManager.Instance.TotalFuelUsed += vehicleDataCalculator.FuelUsed;
                    break;
                }

            }
        }

        public int GetQueueLength(int legIndex) {
            if (legIndex < 0 || legIndex >= vehiclesWaitingAtLeg.Count) return 0;
            
            int queueLength = 0;
            foreach (var vehicle in vehiclesWaitingAtLeg[legIndex].Keys) {
                // Only count vehicles currently stopped (speed < threshold)
                if (vehicle.IsStopped) {
                    queueLength++;
                }
            }
            return queueLength;
        }

        public float GetMaxWaitTime(int legIndex) {
            if (legIndex < 0 || legIndex >= vehiclesWaitingAtLeg.Count) return 0;
            
            float maxWait = 0f;
            foreach (var vehicle in vehiclesWaitingAtLeg[legIndex].Keys) {
                // Find the vehicle with the longest wait time in this leg
                if (vehicle.TotalWaitTime > maxWait) {
                    maxWait = vehicle.TotalWaitTime;
                }
            }
            return maxWait;
        }

        public int GetVehiclesCleared() {
            return vehiclesCleared;
        }

        public void SetVehiclesCleared(int value) {
            vehiclesCleared = value;
        }

        private IEnumerator DebugLogQueueArray() {
            while (true) {
                // Wait exactly 1 second
                yield return new WaitForSeconds(1f);

                if (logQueueArray) {
                    // string.Join neatly prints the whole array separated by commas
                    string arrayValues = string.Join(", ", LiveQueueLengths);
                    // Debug.Log($"[{Name}] Live Queues: [{arrayValues}]");
                }
            }
        }
        
        public float GetAverageWaitTime() {
            float totalWait = 0f;
            int count = 0;
            for (int i = 0; i < numberOfLegs; i++) {
                foreach (var waitTime in vehiclesWaitingAtLeg[i].Values) {
                    totalWait += waitTime;
                    count++;
                }
            }
            return count > 0 ? totalWait / count : 0f;
        }

        public float GetWaitTimeVariance() {
            float avgWait = GetAverageWaitTime();
            if (avgWait == 0f) return 0f;

            int count = 0;
            float sumOfSquaredDifferences = 0f;
            for (int i = 0; i < numberOfLegs; i++) {
                foreach (var waitTime in vehiclesWaitingAtLeg[i].Values) {
                    float diff = waitTime - avgWait;
                    sumOfSquaredDifferences += (diff * diff);
                    count++;
                }
            }
            // Using sample variance formula (N-1)
            return count > 1 ? sumOfSquaredDifferences / (count - 1) : 0f; 
        }

        // public int GetVehiclesWaiting() {
        //     return vehiclesWaiting;
        // }
        // public void SetVehiclesWaiting(int value) {
        //     vehiclesWaiting = value;
        // }
        // public void DecreaseVehiclesWaiting() {
        //     --vehiclesWaiting;
        // }
        // public void IncreaseVehiclesWaiting() {
        //     ++vehiclesWaiting;
        // }
    }
}