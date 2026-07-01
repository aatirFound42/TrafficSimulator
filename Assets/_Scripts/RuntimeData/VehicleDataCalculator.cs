// Assets\_Scripts\RuntimeData\VehicleDataCalculator.cs

using Simulator.AI;
using Simulator.ScriptableObject;
using Simulator.Vehicle;
using Simulator.TrafficSignal;
using System.Collections;
using UnityEngine;
using Utilities;

namespace Simulator.RuntimeData {

    [RequireComponent(typeof(VehicleController), typeof(VehicleDriverAI))]
    public class VehicleDataCalculator : MonoBehaviour {

        private VehicleController vehicleController;
        private VehicleDriverAI vehicleDriverAI;
        //private bool _reachedAnIntersection;

        public bool Initialized { get; private set; } = false;
        public bool IsStopped => vehicleController.Speed < vehicleSettings.considerStopSpeed;
        public int TotalWaitTime { get; private set; }
        public float TotalDistanceTraveled { get; private set; }
        public int TotalTimeTaken { get; private set; }
        public float FuelUsed { get; private set; }

        // State tracking
        private bool isCurrentlyStopped = false;
        public int CurrentLegIndex { get; set; } = -1;
        public IntersectionDataCalculator CurrentIntersection { get; set; }

        //public int WaitTimeBeforeReachingIntersesction { get; private set; }
        //public bool ReachedAnIntersection {
        //    get { return _reachedAnIntersection; }
        //    private set {
        //        if (_reachedAnIntersection == true && value == false) {
        //            StoreData.WriteIntesectionWaitTime(intersectionRoadSetup.name, transform.name, WaitTimeBeforeReachingIntersesction);
        //            WaitTimeBeforeReachingIntersesction = 0;
        //        }
        //        _reachedAnIntersection = value;
        //    }
        //}

        //private RoadSetup intersectionRoadSetup;
        private Coroutine tickCoroutine;

        private VehicleSettingsSO vehicleSettings;
        

        private void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            vehicleDriverAI = GetComponent<VehicleDriverAI>();
            vehicleSettings = vehicleDriverAI.VehicleSettings;
        }

        void OnEnable() {
            Initialize();
        }

        void OnDisable() {
            DeInitialize();
        }

        IEnumerator Tick() {
            while (true) {
                yield return new WaitForSeconds(1f);

                // 1. Check current physical speed
                bool speedIsLow = vehicleController.Speed < vehicleSettings.considerStopSpeed;

                // 2. Add to total wait time if slow
                if (speedIsLow) {
                    TotalWaitTime++;
                }

                // --- NEW EVENT-DRIVEN QUEUE LOGIC ---
                // If the car JUST stopped...
                if (speedIsLow && !isCurrentlyStopped) {
                    isCurrentlyStopped = true;
                    if (CurrentIntersection != null && CurrentLegIndex >= 0) {
                        CurrentIntersection.LiveQueueLengths[CurrentLegIndex]++;
                    }
                }
                // If the car JUST started moving again...
                else if (!speedIsLow && isCurrentlyStopped) {
                    isCurrentlyStopped = false;
                    if (CurrentIntersection != null && CurrentLegIndex >= 0) {
                        CurrentIntersection.LiveQueueLengths[CurrentLegIndex]--;
                    }
                }

                TotalTimeTaken++;

                if (TotalDistanceTraveled == 0 && vehicleDriverAI.IsInitialized) {
                    TotalDistanceTraveled = vehicleDriverAI.DistanceToTravel();
                }


                //var currentRoadSetup = vehicleDriverAI.ShortestPathNodes[vehicleDriverAI.CurrentNodeIndex].roadSetup;

                //if (currentRoadSetup.RoadTypeSO.isIntersection) {
                //    ReachedAnIntersection = true;
                //    intersectionRoadSetup = currentRoadSetup;
                //}
                //else
                //    ReachedAnIntersection = false;

            }
        }

        private void Initialize() {
            //_reachedAnIntersection = false;
            //TotalWaitTime = WaitTimeBeforeReachingIntersesction = 0;
            TotalWaitTime = 0;
            TotalTimeTaken = 0;
            TotalDistanceTraveled = 0;
            FuelUsed = 0f;

            //ReachedAnIntersection = false;
            tickCoroutine = StartCoroutine(Tick());
            Initialized = true;
        }

        private void DeInitialize() {
            StopCoroutine(tickCoroutine);
            Initialized = false;
            FuelUsed = GetComponent<VehicleController>().FuelUsed;
            StoreData.WriteVehicleRuntimeData(new VehicleRuntimeData {
                vehicleName = transform.name,
                TotalDistanceTraveled = TotalDistanceTraveled,
                TotalTimeTaken = TotalTimeTaken,
                TotalWaitTime = TotalWaitTime,
                FuelUsed = FuelUsed 
            });
        }

        public void AssignToIntersection(IntersectionDataCalculator intersection, int leg) {
            CurrentIntersection = intersection;
            CurrentLegIndex = leg;

            // FIX: If the car was already crawling/stopped BEFORE it hit the trigger,
            // we must immediately add it to the queue now that it has arrived!
            if (isCurrentlyStopped && CurrentIntersection != null) {
                CurrentIntersection.LiveQueueLengths[CurrentLegIndex]++;
            }
        }

        public void ClearIntersectionData() {
            // Safety check: if it somehow exits while flagged as stopped, fix the array
            if (isCurrentlyStopped && CurrentIntersection != null && CurrentLegIndex >= 0) {
                CurrentIntersection.LiveQueueLengths[CurrentLegIndex]--;
            }
            
            // isCurrentlyStopped = false;
            CurrentIntersection = null;
            CurrentLegIndex = -1;
        }

    }
}