using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
// component used to network the door with handle
namespace BNG
{
    public class NetworkDoor : NetworkBehaviour
    {
        [Header("The Rigidbody of the door, not the handle")]
        public Rigidbody rb;
        [Header("The Handle Grabbable")]
        public Collider handleGrab;

        [SyncVar(hook = nameof(SetCanOpenDoor))]
        public bool canOpenDoor = true;

        // call this function from grabbable events on the door handle
        public void TakeDoorOwnership()
        {
            if (!isOwned)
            {
                CmdSetDoorOwner();
            }

                StartCoroutine(WaitForOwnerShip());
            
        }

        // Request authority if we don't already own it
        [Command(requiresAuthority = false)]
        public void CmdSetDoorOwner(NetworkConnectionToClient sender = null)
        {

            // Reset the velocity to zero
            // ResetInteractableVelocity();

            // Check if the sender already has authority
            if (sender != netIdentity.connectionToClient)
            {
                // Remove the current authority and assign it to the sender
                netIdentity.RemoveClientAuthority();
                netIdentity.AssignClientAuthority(sender);

            }


        }
        // wait till we own the door before setting the can grab status of the handle
        IEnumerator WaitForOwnerShip()
        {
            while(!isOwned)
            {
                yield return null;
            }

            CmdSetHandleGrab(false);
        }

        // do this to avoid physics issues when switching ownership
        public virtual void ResetInteractableVelocity()
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetHandleGrab(bool canGrabHandle)
        {
            canOpenDoor = canGrabHandle;
        }

        // set this to the release event on the door handle
        public void SetReleaseHandle()
        {
            CmdSetHandleGrab(true);
        }

        // ran from syncvar hook 
        void SetCanOpenDoor(bool oldAble, bool newAble)
        {
            Debug.Log("SetGrabRab" + newAble);

            if(!isOwned)
            {
                // all of the above works when making the Editor the server and connecting 2 clients.. is it perhabs the Host Losing focus causeing the above to lock up the handle
                // otherwise currently this will only work in dedicated server
                handleGrab.isTrigger = newAble;
            }
   
        }
    }
}
