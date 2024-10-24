using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkServerSnapZone : NetworkBehaviour
    {
        GrabbablesInTrigger grabInTrigger;

        public Transform GrabbableTargetOffset;

        NetworkIdentity snapNetId;

        // Start is called before the first frame update
        void Start()
        {
            grabInTrigger = GetComponent<GrabbablesInTrigger>();
            snapNetId = GetComponent<NetworkIdentity>();
        }

        void Update()
        {           
            if(grabInTrigger.ClosestGrabbable != null)
            {
                Grabbable grab = grabInTrigger.ClosestGrabbable;
                NetworkGrabbable netGrab = grab.GetComponent<NetworkGrabbable>();
                NetworkIdentity netId = grab.GetComponent<NetworkIdentity>();

                if(netGrab != null)
                {
                    if(netGrab.holdingStatus == false && isOwned)
                    {
                        netGrab.transform.position = GrabbableTargetOffset.transform.position;
                        netGrab.transform.rotation = GrabbableTargetOffset.transform.rotation;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
           if (!isServer) return;  // Ensure this runs on the server
            if (other.gameObject.layer != 10)
                return;
           NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
            // assign owner of the snap point to the client that owns the object
           snapNetId.RemoveClientAuthority();
           snapNetId.AssignClientAuthority(netId.connectionToClient);
    
        }
    }
}
