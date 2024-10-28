using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class NetworkGrabber : MonoBehaviour {

        [Header("Set the hand side of the remote grabber")]
        [SerializeField]
        ControllerHand controllerHand;

        // fix for triggering pickup when hovering overitems whileholding and object causing rings to disappear
        private Grabber grabber;

        private GrabbablesInTrigger grabInTrigger;

        private NetworkGrabbable netGrabInTrigger;

        private Coroutine pickUpCoroutine;

        public RemoteGrabber rGrabber;

        [Header("Set true to Request Authority on controller input, false to continuously request input")]
        public bool authorityOnInput = true;

        public void Start() {
            // Ensure network grabbable is reset on start / scene load
            netGrabInTrigger = null;
            grabber = GetComponent<Grabber>();
            grabInTrigger = GetComponent<GrabbablesInTrigger>();

            grabber.onGrabEvent.AddListener(RequestAuthority);
            
        }

        private void Update()
        {
            if (grabber.HeldGrabbable != null)
            {
                return;
            }

            if(grabInTrigger.ClosestRemoteGrabbable == null)
            {
                netGrabInTrigger = null;
            }
            else if (grabInTrigger.ClosestRemoteGrabbable != null)
            {
                netGrabInTrigger = grabInTrigger.ClosestRemoteGrabbable.GetComponent<NetworkGrabbable>();
            }

            if (netGrabInTrigger != null)
            {
                if (!authorityOnInput)
                {
                    if (pickUpCoroutine == null)
                    {
                        pickUpCoroutine = StartCoroutine(HandlePickUpEvent());
                    }
                }
                else if (authorityOnInput)
                {
                    // Change this input to suit your needs 
                    if (controllerHand == ControllerHand.Right && InputBridge.Instance.RightGripDown || controllerHand == ControllerHand.Left && InputBridge.Instance.LeftGripDown)
                    {
                        if (!netGrabInTrigger.flightStatus)
                        {
                            netGrabInTrigger.CmdSetFlightStatus(true);
                            netGrabInTrigger.PickUpEvent();
                        }
                    }
                    else if (controllerHand == ControllerHand.Right && !InputBridge.Instance.RightGripDown || controllerHand == ControllerHand.Left && !InputBridge.Instance.LeftGripDown)
                    {

                        if (netGrabInTrigger.flightStatus)
                        {
                            netGrabInTrigger.CmdSetFlightStatus(false);
                        }
                    }
                }
            }
        }

        public void RequestAuthority(Grabbable grab)
        {
            if(grab)
            {
               // Debug.Log(grab.name);
                // would like to move authority request here, but there isn't an event that fires for remote grab start, this would allow for removing the input needed to request.
                // this only fires after the grabbale is grabbed, would need the event fired as soon as remote grab is started
            }
        }

        IEnumerator HandlePickUpEvent() {
            yield return new WaitForSeconds(0.05f);

            while (netGrabInTrigger != null && !netGrabInTrigger.flightStatus) {
                netGrabInTrigger.PickUpEvent();
                yield return new WaitForSeconds(0.1f);
            }

            pickUpCoroutine = null;
        }

    }
}
