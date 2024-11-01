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
            // the following problem is only in host/client, may be because the host looses focus in testing? Otherwise, works fine when running one build as server only and connecting clients to it//
            // need to figure out here, how to make the handle not grabbable while someone else is holding it.. I've tried disableing the grabbable, disabling the gameobject of the grabbable, and the collider of the grabbable as well as ..
            // making the collider a trigger, all lock up the handle after reenable like there is something else going on in other code that is loosing the connection when this happens, if I don't disable anything, then all works, but...
            // there is the possibility of someone else grabbing the handle at the same time

            if(!isOwned)
            {
                // all of the above works when making the Editor the server and connecting 2 clients.. is it perhabs the Host Losing focus causeing the above to lock up the handle
                // otherwise currently this will only work in dedicated server
                handleGrab.isTrigger = newAble;
            }
   
        }
    }
}
