using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X509;
using System.Security.Principal;

// script to handle authority switching of the grabbable so everyone can pickup items
namespace BNG {
    [RequireComponent(typeof(NetworkGrabbableEvents))]
    public class NetworkGrabbable : NetworkBehaviour {

        [Header("Object Index to Be used With Scene Object Spawner")]
        [Tooltip("Index number must match the index in the list")]
        public int objectIndex = 0;

        // Grabbable components
        public List<Grabbable> grabbables;

        // Ringhelper canvas
        public Canvas ringCanvas;
        public RingHelper ringHelper;

        // Pose ID to use for left / right hand
        public int LeftHandPoseIndex = 1;
        public int RightHandPoseIndex = 0;

        [SyncVar(hook = (nameof(UpdateGrabStatus)))]
        public bool holdingStatus = false;

        [SyncVar(hook = (nameof(SetFlightStatus)))]
        public bool flightStatus = false;


        Rigidbody rb;

        NetworkGrabbableEvents networkGrabbableEvents;

        void Awake() {
            rb = GetComponent<Rigidbody>();
            networkGrabbableEvents = GetComponent<NetworkGrabbableEvents>();
            grabbables.AddRange(GetComponentsInChildren<Grabbable>());

            if (GetComponentInChildren<RingHelper>() != null) {
                ringHelper = GetComponentInChildren<RingHelper>();
                ringCanvas = ringHelper.GetComponent<Canvas>();
            }
        }       
       
        void Update() {

            if (!NetworkServer.active)
            {
                return;
            }
            CheckResetGrabbableVelocity();

        }

        public void UpdateGrabStatus(bool oldHoldStatus, bool newHoldStatus) {
            // Disable Grabbable to ensure object can't be grabbed while someone is holding it
            if (!isOwned) {
                for (int x = 0; x < grabbables.Count; x++) {
                    grabbables[x].enabled = !newHoldStatus;
                }
            }
            // Enable/ disable the helper ring on grab and release
            if (ringHelper != null) {
                ringHelper.enabled = !newHoldStatus;
            }

            if (ringCanvas) {
                ringCanvas.enabled = !newHoldStatus;
            }
        }

        public void SetFlightStatus(bool oldFlightStatus, bool newFlightStatus)
        {
            // just need the bool to be synced, no logic to run here
        }
    
        // Called from the NetworkGrabber component on the RemoteGrabber of the Network Rig
        public void PickUpEvent() {
            // Check to see if the object is being held, if so, abort
            if (holdingStatus) {
                return;
            }

            if (!isOwned) {
                CmdPickup();
            }
            
            StartCoroutine(WaitForOwnership());
        }

        // Wait till we own the object before setting the holding status
        IEnumerator WaitForOwnership() {
            while (!isOwned) {
                yield return null;
            }

            float timeNotHeld = 0f; // Timer for how long the object has not been held
            float maxTime = 3f; // Time limit for how long we wait

            // fix for not fully grabbing the grabbable so the ring and grabbable don't get disabled unless the object is fully grabbed
            // set with a timer to avoid leaving the coroutine in an endless loop waiting to be held
            while (true) 
            {
                if (!grabbables[0].BeingHeld)
                {
                    timeNotHeld += UnityEngine.Time.deltaTime; 

                    if (timeNotHeld >= maxTime) 
                    {
                       
                        yield break; 
                    }
                }
                else
                {                   
                    CmdSetHoldingStatus(true);
                    yield break;
                }

                yield return null; 
            }    
        }

        public void DropEventHoldFalse() {
            // set the holding status to false when you let go so others can pick it up
            CmdSetHoldingStatus(false);
            
        }

        // Request object authority if we don't already own it
        [Command(requiresAuthority = false)]
        public void CmdPickup(NetworkConnectionToClient sender = null) {

            // Reset the velocity to zero on pickup
            ResetInteractableVelocity();

            // Check if the sender already has authority
            if (sender != netIdentity.connectionToClient) {
                // Remove the current authority and assign it to the sender
                netIdentity.RemoveClientAuthority();
                netIdentity.AssignClientAuthority(sender);
            }
        }

        [Command(requiresAuthority = false)]
        void CmdSetHoldingStatus(bool status) {
            holdingStatus = status;
        }

        [Command(requiresAuthority = false)]
        public void CmdSetFlightStatus(bool currentStatus)
        {
            flightStatus = currentStatus;
        }

        public void SnapZoneSetHoldingStatus(bool status) {
            CmdSetHoldingStatus(status);
        }

        public virtual void ResetInteractableVelocity() {
            // Without this you may notice some pickups rapidly fall through the floor
            if (rb != null) {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Check to see if the object is owned by the server, if it is, reset the velocity and set the holding status to false
        [Server]
        void CheckResetGrabbableVelocity() {
            if (IsOwnedByServer() == true && holdingStatus == true) {
                Debug.Log(string.Format("This object {0} is owned by the server. Resetting velocity.", transform.name));
                ResetInteractableVelocity();
                holdingStatus = false;
            } 
        }
        [Server]
        public bool IsOwnedByServer() {
            if(netIdentity != null && netIdentity.connectionToClient == null)
            {
                return true;
            }

            return false;
        }

    }
}
