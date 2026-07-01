// Assets\_Scripts\OnVehicleEnterTrigger.cs

using Simulator.RuntimeData;
using Simulator.TrafficSignal;
using UnityEngine;

namespace Simulator {
    public class OnVehicleEnterTrigger : MonoBehaviour {
        [SerializeField] private IntersectionDataCalculator intersectionDataCalculator;
        [SerializeField] private int legIndex;

        #region Unity Methods
        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag("Vehicle"))
                return;

            var vehicleData = other.GetComponent<VehicleDataCalculator>();
    
            // Tell the vehicle where it is
            vehicleData.AssignToIntersection(intersectionDataCalculator, legIndex);

            intersectionDataCalculator.VehicleEntered(vehicleData, legIndex);
            // Debug.Log($"[Enter] Vehicle {other.name} on leg {legIndex} at time {Time.time}");
            // intersectionDataCalculator.IncreaseVehiclesWaiting();
        }
        #endregion
    }
}
