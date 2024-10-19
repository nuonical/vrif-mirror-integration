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

        // fix for triggering pickup when hovering overitems whileholding and object causing rings to disappear
        public Grabber grabber;

        private Coroutine pickUpCoroutine;

        [Header("Set true to Request Authority on controller input, false to continuously request input")]
        public bool authorityOnInput = true;

        public void Start() {
            // Ensure network grabbable is reset on start / scene load
            networkGrabbable = null;
        }

        void OnTriggerStay(Collider other) {
            // if we are holding an object or the object is not a grabbable, do nothing
            if (grabber.HeldGrabbable != null || other.gameObject.layer != grabbableLayer) {
                return;
            }
            // Only check the Grabbable Layer
            if (other.gameObject.layer == grabbableLayer) {
                networkGrabbable = other.transform.root.GetComponent<NetworkGrabbable>();
            }

            if (networkGrabbable != null) {
                if (!authorityOnInput) {
                    if (pickUpCoroutine == null) {
                        pickUpCoroutine = StartCoroutine(HandlePickUpEvent());
                    }
                } else if (authorityOnInput) {
                    // Change this input to suit your needs 
                    if (controllerHand == ControllerHand.Right && InputBridge.Instance.RightGripDown || controllerHand == ControllerHand.Left && InputBridge.Instance.LeftGripDown) {
                        if (!networkGrabbable.flightStatus) {
                            networkGrabbable.CmdSetFlightStatus(true);
                            networkGrabbable.PickUpEvent();
                        }
                    } else if (controllerHand == ControllerHand.Right && !InputBridge.Instance.RightGripDown || controllerHand == ControllerHand.Left && !InputBridge.Instance.LeftGripDown) {

                        if (networkGrabbable.flightStatus) {
                            networkGrabbable.CmdSetFlightStatus(false);
                        }
                    }
                }
            }
        }

        IEnumerator HandlePickUpEvent() {
            yield return new WaitForSeconds(0.05f);

            while (networkGrabbable != null && !networkGrabbable.flightStatus) {
                networkGrabbable.PickUpEvent();
                yield return new WaitForSeconds(0.1f);
            }

            pickUpCoroutine = null;
        }

        // Clear the current grabbable on exit
        void OnTriggerExit(Collider other) {
            if (networkGrabbable != null && other.gameObject.layer == grabbableLayer) {
                networkGrabbable = null;
            }

            if (pickUpCoroutine != null) {
                StopCoroutine(pickUpCoroutine);
                pickUpCoroutine = null;
            }
        }
    }
}
