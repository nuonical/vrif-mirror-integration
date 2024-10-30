using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkWeaponAttachement : NetworkBehaviour
    {
        SnapZone snapZone;

        public Grabbable rootGrabbable;
        public NetworkIdentity rootNetId;

        [SyncVar(hook = nameof(SetCanRemoveSnapped))]
        public bool canRemove;

        [SyncVar(hook = nameof(AssignSnapped))]
        public NetworkIdentity snappedId;

        public GameObject snapImposterObject;

        public List<MeshRenderer> meshRenderers = new();

        private void Awake()
        {
            snapZone = GetComponent<SnapZone>();
            rootNetId = rootGrabbable.GetComponent<NetworkIdentity>();
        }

        public void OnEnable()
        {
            snapZone.OnSnapEvent.AddListener(SetSnapped);
            snapZone.OnDetachEvent.AddListener(SetDetached);
        }

        public void OnDisable()
        {
            snapZone.OnSnapEvent.RemoveListener(SetSnapped);
            snapZone.OnDetachEvent.RemoveListener(SetDetached);
        }

        // set from unity grabbable events on grab
        public void RootGabbableGrab()
        {
            StartCoroutine(AwaitOwnerShip());
        }

        public IEnumerator AwaitOwnerShip()
        {
            while(!isOwned)
            {
                yield return null;
            }

            CmdSetRootStatus();
        }

        [Command]
        void CmdSetRootStatus()
        {
            canRemove = !canRemove;
            if(snappedId != null && snappedId.connectionToClient != rootNetId.connectionToClient)
            {
                Rigidbody rb = snappedId.GetComponent<Rigidbody>();
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                snappedId.RemoveClientAuthority();
                snappedId.AssignClientAuthority(rootNetId.connectionToClient);
            }
        }

        public void SetCanRemoveSnapped(bool oldCanRemove, bool newCanRemove)
        {
            if (!isOwned)
            {
                snapZone.CanRemoveItem = false;
            }

            else if (isOwned && snapZone.CanRemoveItem == false)
            {
                snapZone.CanRemoveItem = true;
            }
        }

        public void SetSnapped(Grabbable grab)
        {
            if (isOwned)
            {
                NetworkIdentity snappedId = grab.GetComponent<NetworkIdentity>();
                CmdSetHeldItem(snappedId);
            }
        }

        [Command]
        void CmdSetHeldItem(NetworkIdentity heldNetId)
        {
            snappedId = heldNetId;
        }

        public void AssignSnapped(NetworkIdentity oldNetId, NetworkIdentity newNetId)
        {

            if (snappedId == null)
            {
                
                snapImposterObject.SetActive(false);     

                for (int i = meshRenderers.Count - 1; i >= 0; i--)
                {
                    meshRenderers[i].enabled = true;
                    meshRenderers.RemoveAt(i);
                }

                snapZone.ReleaseAll();
            }

            else if (snapZone.HeldItem == null)
            {                
                Grabbable grab = snappedId.GetComponent<Grabbable>();
                snapZone.GrabGrabbable(grab);
            }

            if (snappedId != null)
            {
                snapImposterObject.SetActive(true);

                meshRenderers.AddRange(snapZone.HeldItem.GetComponentsInChildren<MeshRenderer>());
                
                foreach (MeshRenderer mesh in meshRenderers)
                {
                    mesh.enabled = false;
                }
            }

        }

        //sync detach event
        void SetDetached(Grabbable grab)
        {
            if (isOwned)
            {
                CmdSetHeldItem(null);
            }
        }
    }
}
