using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkSnapZone : NetworkBehaviour
    {
        public SnapZone snapZone;

        public Grabbable snappedGrabbable;

        [SyncVar(hook = nameof(SyncSnapZoneEvent))]
        public bool snapStatus = false;
        private void Awake()
        {            
            snapZone.OnSnapEvent.AddListener(GrabbableSnapped);
            snapZone.OnDetachEvent.AddListener(GrabbableDetached);
        }

        void GrabbableSnapped(Grabbable grab)
        {       
            NetworkGrabbable nGrab = grab.GetComponent<NetworkGrabbable>();
            if(nGrab)
            {
                StartCoroutine(SnapCoroutine(nGrab));
            }
        }

        IEnumerator SnapCoroutine(NetworkGrabbable netGrab)
        {
            NetworkIdentity netId = netGrab.GetComponent<NetworkIdentity>();
            yield return new WaitForSeconds(0.5f);    
            CmdOnSnapped(true, netId);
        }

        [Command(requiresAuthority = false)]
        void CmdOnSnapped(bool snappedStatus, NetworkIdentity netID)
        {          
            snapStatus = snappedStatus;
        }

        void SyncSnapZoneEvent(bool oldStatus, bool newStatus)
        {
            Debug.Log("Snap Event Synced");
            if(snapZone)
            {
                Debug.Log("SnapZone Item Snapped and Synced");
                // run logic here for an on snap event
            }

            if (!snapStatus)
            {
                if (snapZone.HeldItem != null)
                {
                   // snapZone.HeldItem = null;
                }
                Debug.Log("SnapZone Item Detached and Synced");
                // run logic here for an on detach event
            }
        }

        //sync detach event
        void GrabbableDetached(Grabbable grab)
        {
            NetworkIdentity netId = grab.GetComponent<NetworkIdentity>();
            CmdOnSnapped(false, netId);
        }
    }
}
