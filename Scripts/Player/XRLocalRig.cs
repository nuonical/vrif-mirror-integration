using System.Collections;
using UnityEngine;

// script to hold all the transforms needed to position and rotate the Network Rig
namespace BNG {
    public class XRLocalRig : MonoBehaviour {

        public static XRLocalRig Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<XRLocalRig>();
                    if (_instance == null) {
                        Debug.Log("XR Local Rig should be present in scene.");
                    }
                }
                return _instance;
            }
        }
        private static XRLocalRig _instance;

        // A reference to the networkPlayer. Poplulates when the player prefab spawns
        NetworkPlayer networkPlayer;

        [Header("The Player Controller Transform")]
        public Transform playerTransform;

        [Header("The Head Transform")]
        public Transform headTransform;

        [Header("The Left Hand Transform")]
        public Transform LeftHandTransform;

        [Header("The Right Hand Transform")]
        public Transform RightHandTransform;

        [Header("Player Body")]
        public Transform playerBody;

        // Hand pose blenders
        [Header("Hand / Grabbers")]
        public Grabber GrabberLeft;
        public Grabber GrabberRight;

        public HandPoseBlender LeftHandPoseBlender;
        public HandPoseBlender RightHandPoseBlender;

        int handPoseIndexLeft, handPoseIndexRight;

        // additive level grabbable spawner
        public int rightGrabbableInt = -1;
        public int leftGrabbableInt = -1;

        void Awake() {
            // Only one rig may exist at a time.
            
            if (_instance != null && _instance != this) {
                Destroy(this);
                return;
            }
        }

        void Start() {
            // Setup the Grabber Hand Pose Events
            GrabberLeft.onGrabEvent.AddListener(GetHandPoseIndexLeft);
            GrabberLeft.onReleaseEvent.AddListener(ReleaseNetworkHandPoseLeft);

            GrabberRight.onGrabEvent.AddListener(GetHandPoseIndexRight);
            GrabberRight.onReleaseEvent.AddListener(ReleaseNetworkHandPoseRight);
        }

        public void SetNetworkPlayer(NetworkPlayer netPlayer) {
            networkPlayer = netPlayer;
        }

        public void GetHandPoseIndexLeft(Grabbable grabbed) {
            
            // Look up hand pose index
            var netGrabbable = grabbed.GetComponent<NetworkGrabbable>();
            if (netGrabbable != null) {
                handPoseIndexLeft = netGrabbable.LeftHandPoseIndex;

                if(networkPlayer) {
                    networkPlayer.CmdSyncLeftPose(handPoseIndexLeft);
                }

                // Fix for if grabbable is outside of trigger
                netGrabbable.PickUpEvent();
                // set grabbable int for respawning in new scene
                NetworkGrabbable netGrab = grabbed.GetComponent<NetworkGrabbable>();
                if(netGrab)
                {
                    leftGrabbableInt = netGrab.objectIndex;
                }
            }
        }

        public void GetHandPoseIndexRight(Grabbable grabbed) {
            
            var netGrabbable = grabbed.GetComponent<NetworkGrabbable>();
            if (netGrabbable) {
                handPoseIndexRight = netGrabbable.RightHandPoseIndex;

                if (networkPlayer) {
                    networkPlayer.CmdSyncRightPose(handPoseIndexRight);

                    // For debug
                    networkPlayer.SetGrabbableID(grabbed.transform.root.gameObject);
                }

                // Fix for if grabbable is outside of trigger
                netGrabbable.PickUpEvent();

                // set grabbable int for respawning in new scene
                NetworkGrabbable netGrab = grabbed.GetComponent<NetworkGrabbable>();
                if (netGrab)
                {
                    rightGrabbableInt = netGrab.objectIndex;
                }
            }
        }

        public void ReleaseNetworkHandPoseLeft(Grabbable released) {
            if(networkPlayer) {
                networkPlayer.CmdReleaseLeftHandPose();
            }
        }
        public void ReleaseNetworkHandPoseRight(Grabbable released) {
            if(networkPlayer) {
                networkPlayer.CmdReleaseRightHandPose();
            }

            // set the ind back to -1 so no object spawns if we dropped it before changing scenes
        //    rightGrabbableInt = -1;
            
        }

        IEnumerator WaitToSetGrabbaleInt()
        {
            yield return new WaitForSeconds(3f);
            // set the ind back to -1 so no object spawns if we dropped it before changing scenes
            rightGrabbableInt = -1;
        }
    }
}
