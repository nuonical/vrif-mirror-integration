
using UnityEngine;

namespace BNG {
    public class NetworkGrabbableEvents : GrabbableEvents {

        NetworkGrabbable netGrabbable;

        void Start() {
            netGrabbable = GetComponent<NetworkGrabbable>();
        }

        public override void OnGrab(Grabber grabber) {
            base.OnGrab(grabber);
        }

        public override void OnRelease() {
            netGrabbable.DropEventHoldFalse();
            base.OnRelease();
        }

        public override void OnSnapZoneEnter() {
            if (netGrabbable != null) { 
                netGrabbable.SnapZoneSetHoldingStatus(true);
            }
            
            Debug.Log("Set holding status");

            base.OnSnapZoneEnter();
        }
    }
}

