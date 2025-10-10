using Simulator.RuntimeData;
using Simulator.TrafficSignal;
using UnityEngine;

namespace Simulator {
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class OnVehicleExitTrigger : MonoBehaviour {
        #region Public Fields

        #endregion

        private int count = 0;


        private IntersectionDataCalculator intersectionDataCalculator;
        #region Unity Methods
        private void Awake() {
            intersectionDataCalculator = GetComponent<IntersectionDataCalculator>();
        }
        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag("Vehicle"))
                return;
            intersectionDataCalculator.VehicleExited(other.transform.GetComponent<VehicleDataCalculator>());
            // Debug.Log($"[Exit] Vehicle {other.name} at time {Time.time}");
            ++count;
            // Debug.Log($"Exit count: {count}");
            intersectionDataCalculator.SetVehiclesCleared(count);
            // intersectionDataCalculator.DecreaseVehiclesWaiting();
        }

        #endregion

        #region Private Methods

        #endregion

    }
}
