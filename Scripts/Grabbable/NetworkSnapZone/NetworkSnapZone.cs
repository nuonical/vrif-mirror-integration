using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkSnapZone : NetworkBehaviour
    {
        private SnapZone snapZone;

        private NetworkIdentity snapZoneId;

        [SyncVar(hook = nameof(SyncSnapZoneEvent))]
        public NetworkIdentity snappedID;

        private void Awake()
        {
            snapZone = GetComponent<SnapZone>();

            if (snapZone)
            {
                snapZoneId = snapZone.GetComponent<NetworkIdentity>();
            }
        }

        private void Start()
        {           
            if(snappedID != null)
            {
                Grabbable grab = snappedID.GetComponent<Grabbable>();
                if(grab)
                {
                    snapZone.GrabGrabbable(grab);
                }
            }
        }

        public void OnEnable()
        {
            snapZone.OnSnapEvent.AddListener(GrabbableSnapped);
            snapZone.OnDetachEvent.AddListener(GrabbableDetached);
        }

        public void OnDisable()
        {
            snapZone.OnSnapEvent.RemoveListener(GrabbableSnapped);
            snapZone.OnDetachEvent.RemoveListener(GrabbableDetached);
        }

        void GrabbableSnapped(Grabbable grab)
        {                   
            NetworkIdentity nId = grab.GetComponent<NetworkIdentity>();
            
            if (nId && nId.isOwned)
            {
                CmdOnSnapped(nId);
                StartCoroutine(SetHeldStatus(grab));
            }
        }

        IEnumerator SetHeldStatus(Grabbable grab)
        {
            yield return new WaitForSeconds(0.5f);
            grab.GetComponent<NetworkGrabbable>().SnapZoneSetHoldingStatus(false);
        }

        [Command(requiresAuthority = false)]
        void CmdOnSnapped(NetworkIdentity netID)
        {          
            snappedID = netID;            
        }

        void SyncSnapZoneEvent(NetworkIdentity oldID, NetworkIdentity newID)
        {
            if(snappedID == null)
            {
                snapZone.ReleaseAll();
            }

            else if(snapZone.HeldItem == null)
            {
                Debug.Log(snappedID.name);
                Grabbable grab = snappedID.GetComponent<Grabbable>();
                snapZone.GrabGrabbable(grab);
                
            }
        }

 
        //sync detach event
        void GrabbableDetached(Grabbable grab)
        {           
            CmdOnSnapped(null);
        }
    }
}
