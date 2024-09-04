using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class NetworkGrabber : MonoBehaviour {

        [Header("Set the hand side of the remote grabber")]
        [SerializeField] 
        ControllerHand controllerHand;

        [Header("Defauld Grabbable Layer is 10; Change If needed")]
        [SerializeField] 
        int grabbableLayer = 10; //  grabbale layer for VRIF is 10 by default, change if needed

        NetworkGrabbable networkGrabbable;

        public void Start() {
            // Ensure network grabbable is reset on start / scene load
            networkGrabbable = null;
        }

        void OnTriggerStay(Collider other) {
            // Only check the Grabbable Layer
            if (other.gameObject.layer == grabbableLayer) {
                networkGrabbable = other.GetComponent<NetworkGrabbable>();
            }

            if (networkGrabbable != null) {
                // Change this input to suit your needs //  only request authority if we pull the grip
                if (controllerHand == ControllerHand.Right && InputBridge.Instance.RightGripDown || controllerHand == ControllerHand.Left && InputBridge.Instance.LeftGripDown) {
                    networkGrabbable.PickUpEvent();
                }
            }
        }

        // Clear the current grabbable on exit
        void OnTriggerExit(Collider other) {
            if (networkGrabbable != null && other.gameObject.layer == grabbableLayer) {
                networkGrabbable = null;
            }
        }
    }
}
