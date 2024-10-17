using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG {
    public class NetworkPlayer : NetworkBehaviour {

        [Header("The Network Rig Root")]
        [SerializeField] 
        Transform networkPlayer;

        [Header("The Network Rig Head")]
        [SerializeField] 
        Transform networkHead;
        
        [Header("The Network Rig LeftHand")]
        [SerializeField] 
        Transform networkLeftHand;
        
        [Header("The Network Rig RightHand")]
        [SerializeField] 
        Transform networkRightHand;

        [Header("Then Network Rig Body")]
        [SerializeField] 
        Transform networkBody;

        public XRLocalRig hardwareRig;

        // Transforms cached from the XRLocalRig script
        Transform hardwarePlayer;
        Transform hardwareHead;
        Transform hardwareLeftHand;
        Transform hardwareRightHand;
        Transform hardwarePlayerBody;
        Grabber leftGrabber;
        Grabber rightGrabber;

        // bools to set held grabbable status to sync
        private Grabbable previousRightHeldGrabbable;
        // Graphics of the player so they can be disabled for the local network rig
        public List<SkinnedMeshRenderer> SkinnedRenderers;
        public List<MeshRenderer> MeshRenderers;

        public float checkInterval = 0.5f; // Time interval between ownership checks
        public float timeout = 5f;         // Total time before giving up on ownership
        public int maxRetries = 10;        // Maximum retries before stopping

        // Hand pose sync
        [SerializeField] 
        HandPoseBlender leftNetworkPoseBlender;

        [SerializeField] 
        HandPoseBlender rightNetworkPoseBlender;

        [SerializeField] 
        HandPoser rightNetworkHandPoser;

        [SerializeField] 
        HandPoser leftNetworkHandPoser;

        // For Debug
        [SyncVar]
        public NetworkIdentity grabbableID;

        // lone collider used for explosion damage
        public Collider explosionCollider;
        // struct to hold the hand pose data
        [System.Serializable]
        public struct HandPoseData {
            public float ThumbValue;
            public float IndexValue;
            public float GripValue;

            public HandPoseData(float thumbValue, float indexValue, float gripValue) {
                ThumbValue = thumbValue;
                IndexValue = indexValue;
                GripValue = gripValue;
            }
        }

        // the previous hand pose data
        HandPoseData previousRightHandPoseData;
        HandPoseData previousLeftHandPoseData;

        // the threshold of the grip, thumb and index values in which to send the data to conserve network traffic, we don't want to send data every frame constantly
        // only if we are holding down the button or releasing it
        private const float thresholdHigh = 0.8f;
        private const float thresholdLow = 0.2f;

        void Start() {


            // Initialize previous pose data with invalid values
            previousRightHandPoseData = new HandPoseData(-1f, -1f, -1f);
            previousLeftHandPoseData = new HandPoseData(-1f, -1f, -1f);

            if (!isOwned)
                return;
            hardwareRig = XRLocalRig.Instance;

            if (hardwareRig != null)
            {
                hardwareRig.SetNetworkPlayer(this);

                // Cache the transforms needed for position and rotation of the network rig from the BNGHardwareRig Script
                hardwarePlayer = hardwareRig.playerTransform;
                hardwareHead = hardwareRig.headTransform;
                hardwareLeftHand = hardwareRig.LeftHandTransform;
                hardwareRightHand = hardwareRig.RightHandTransform;
                hardwarePlayerBody = hardwareRig.playerBody;
                leftGrabber = hardwareRig.GrabberLeft;
                rightGrabber = hardwareRig.GrabberRight;
            }
            // disable all skinned mesh renders on the local network rig
            SkinnedRenderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());

            for (int x = 0; x < SkinnedRenderers.Count; x++)
            {
                SkinnedRenderers[x].enabled = false;
            }
            // disable all mesh renderers on the the local network rig
            MeshRenderers.AddRange(GetComponentsInChildren<MeshRenderer>());

            for (int x = 0; x < MeshRenderers.Count; x++)
            {
                MeshRenderers[x].enabled = false;
            }

            // disable colliders on local network player
            List<Collider> colliders = new();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            foreach(Collider col in colliders)
            {
                if (col != explosionCollider)
                {
                    col.enabled = false;
                }
            }
            
            // quick fix, but this needs moved to some other one and done function
           // StartCoroutine(SendReleaseHandPoses());
        }

        private IEnumerator SendReleaseHandPoses()
        {
            while (true) // Keep the coroutine running indefinitely
            {
                // Check if the right grabber is not holding anything and send the right hand release command
                if (rightGrabber.HeldGrabbable == null)
                {
                    CmdReleaseRightHandPose();
                }

                // Check if the left grabber is not holding anything and send the left hand release command
                if (leftGrabber.HeldGrabbable == null)
                {
                    CmdReleaseLeftHandPose();
                }

                yield return new WaitForSeconds(1f); // Wait for 1 second before checking again
            }
        }

        void LateUpdate() {
            // Only run this if we are the local player, so if we own it, then it is the local representation
            if (!isOwned || hardwareRig == null) {
                return;
            }

            if (hardwareRig != null) {
                // Set the position and rotation of the network rig player, head and hands to match that of the Hardware Rig transforms
                networkPlayer.SetPositionAndRotation(hardwarePlayer.position, hardwarePlayer.rotation);
                networkHead.SetPositionAndRotation(hardwareHead.position, hardwareHead.rotation);
                if(hardwarePlayerBody)
                {
                    networkBody.SetPositionAndRotation(hardwarePlayerBody.position, hardwarePlayerBody.rotation);
                }

                networkLeftHand.SetPositionAndRotation(hardwareLeftHand.position, hardwareLeftHand.rotation);
                networkRightHand.SetPositionAndRotation(hardwareRightHand.position, hardwareRightHand.rotation);
            }

            if (rightNetworkPoseBlender != null && leftNetworkPoseBlender != null) {
                // Right Hand Pose Data
                HandPoseData currentRightHandPoseData = new HandPoseData(
                    hardwareRig.RightHandPoseBlender.ThumbValue,
                    hardwareRig.RightHandPoseBlender.IndexValue,
                    hardwareRig.RightHandPoseBlender.GripValue
                );

                // Left Hand Pose Data
                HandPoseData currentLeftHandPoseData = new HandPoseData(
                    hardwareRig.LeftHandPoseBlender.ThumbValue,
                    hardwareRig.LeftHandPoseBlender.IndexValue,
                    hardwareRig.LeftHandPoseBlender.GripValue
                );

                // Sync Right Hand if needed
                if (rightNetworkHandPoser.CurrentPose == null) {
                    if (ShouldSendPoseData(previousRightHandPoseData, currentRightHandPoseData)) {
                        previousRightHandPoseData = currentRightHandPoseData;
                        CmdSyncPoseData(currentRightHandPoseData, true);
                    }
                }

                // Sync Left Hand if needed
                if (leftNetworkHandPoser.CurrentPose == null) {
                    if (ShouldSendPoseData(previousLeftHandPoseData, currentLeftHandPoseData)) {
                        previousLeftHandPoseData = currentLeftHandPoseData;
                        CmdSyncPoseData(currentLeftHandPoseData, false);
                    }
                }
            }

        }


        // Check to see if the pose data has changed
        public virtual bool ShouldSendPoseData(HandPoseData previousData, HandPoseData currentData) {
            // check to see if we brieched the threshold to send the data values accross the network with a command
            return (ShouldSendValue(previousData.ThumbValue, currentData.ThumbValue) ||
                    ShouldSendValue(previousData.IndexValue, currentData.IndexValue) ||
                    ShouldSendValue(previousData.GripValue, currentData.GripValue));
        }

        // Check on the value to see if we brieched the threshhold
        public virtual bool ShouldSendValue(float previousValue, float currentValue) {
            return (previousValue <= thresholdLow && currentValue > thresholdLow) ||
                   (previousValue >= thresholdHigh && currentValue < thresholdHigh) ||
                   (Mathf.Abs(currentValue - previousValue) > Mathf.Epsilon);
        }

        // Command to the server to change the Hand Pose Data 
        [Command]
        void CmdSyncPoseData(HandPoseData poseData, bool isRightHand) {
            RpcSyncPoseData(poseData, isRightHand);
        }

        // rpc back to the client to change the values of the hand poser
        [ClientRpc]
        void RpcSyncPoseData(HandPoseData poseData, bool isRightHand) {
            if (isRightHand) {
                rightNetworkPoseBlender.ThumbValue = poseData.ThumbValue;
                rightNetworkPoseBlender.IndexValue = poseData.IndexValue;
                rightNetworkPoseBlender.GripValue = poseData.GripValue;
            } 
            else {
                leftNetworkPoseBlender.ThumbValue = poseData.ThumbValue;
                leftNetworkPoseBlender.IndexValue = poseData.IndexValue;
                leftNetworkPoseBlender.GripValue = poseData.GripValue;
            }
        }

        // set the hand pose if we are holding an object
        [Header("List of all available hand poses")]
        public List<HandPose> HandPoses;

        // set left hand pose
        [SyncVar(hook = nameof(SetLeftHandPoseIndex))]
        public int LeftPoseIndex;

        [SyncVar(hook = nameof(ApplyLeftHandPoseIndex))]
        public bool LeftPoseBool;

        [SyncVar(hook = nameof(ReleaseLeftHandPose))]
        public bool ReleaseLeftBool;

        public void SetLeftHandPoseIndex(int oldLeftIndex, int newLeftIndex) {
            // left blank, this is only here so the int gets updated to late joiners   
        }

        public void ApplyLeftHandPoseIndex(bool oldBool, bool newBool) {
           
            StartCoroutine(WaitToSetLeftHandPose());
        }

        // fix for not setting hand pose correctly when grabbing from the snap point, a delay was needed
        IEnumerator WaitToSetLeftHandPose()
        {
            yield return null;
            leftNetworkHandPoser.CurrentPose = HandPoses[LeftPoseIndex];
            leftNetworkHandPoser.OnPoseChanged();
            leftNetworkPoseBlender.UpdatePose = false;
        }

        [Command]
        public void CmdSyncLeftPose(int leftIndex) {
            LeftPoseIndex = leftIndex;
            LeftPoseBool = !LeftPoseBool;
        }

        public void ReleaseLeftHandPose(bool oldStatus, bool newStatus) {
            leftNetworkHandPoser.CurrentPose = null;
            leftNetworkPoseBlender.UpdatePose = true;
        }

        [Command]
        public void CmdReleaseLeftHandPose() {
            ReleaseLeftBool = !ReleaseLeftBool;
        }

        // Set right hand pose
        [SyncVar(hook = nameof(SetRightHandPoseIndex))]
        public int rightPoseIndex;

        [SyncVar(hook = nameof(ApplyRightHandPoseIndex))]
        public bool rightPoseBool;

        [SyncVar(hook = nameof(ReleaseRightHandPose))]
        public bool releaseRightBool;

        public void SetRightHandPoseIndex(int oldLeftIndex, int newLeftIndex) {
            // left blank, this is only here so the int gets updated to late joiners   
        }

        public void ApplyRightHandPoseIndex(bool oldBool, bool newBool) {
            if(HandPoses != null && HandPoses.Count >0 && HandPoses.Count >= rightPoseIndex) {
              
                StartCoroutine(WaitToSetRightHandPose());
            }
        }
        // fix for not setting hand pose correctly when grabbing from the snap point, a delay was needed
        IEnumerator WaitToSetRightHandPose()
        {
            yield return null;
            rightNetworkHandPoser.CurrentPose = HandPoses[rightPoseIndex];
            rightNetworkHandPoser.OnPoseChanged();
            rightNetworkPoseBlender.UpdatePose = false;
        }

        [Command]
        public void CmdSyncRightPose(int rightIndex) {
            rightPoseIndex = rightIndex;
            rightPoseBool = !rightPoseBool;
        }

        public void SetGrabbableID(GameObject grabbableGo) {
            NetworkIdentity networkID = grabbableGo.GetComponent<NetworkIdentity>();
            // get the network id of the grabbable
            CmdSetGrabbableIdentity(networkID);
        }

        [Command]
        public void CmdSetGrabbableIdentity(NetworkIdentity netID) {
            grabbableID = netID;
        }

        public void ReleaseRightHandPose(bool oldStatus, bool newStatus) {
            rightNetworkHandPoser.CurrentPose = null;
            rightNetworkPoseBlender.UpdatePose = true;
        }

        [Command]
        public void CmdReleaseRightHandPose() {
            releaseRightBool = !releaseRightBool;
        }

        
    }
}
