using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    // script is used to apply force to objects in the scene when you hit them and don't own the object
    public class NetworkCollisionForce : NetworkBehaviour
    {
        void OnCollisionEnter(Collision collision)
        {
            if (!isOwned)
                return;
            NetworkIdentity netId = collision.gameObject.GetComponent<NetworkIdentity>();

            // Check if the object has a NetworkIdentity and is not controlled by this client
            if (netId != null && netId.isOwned == false)
            {
                // Calculate the collision force using relative velocity and the mass of the other object
                Rigidbody otherRigidbody = collision.rigidbody;

                if (otherRigidbody != null)
                {
                    // Calculate force: mass * relative velocity
                    Vector3 collisionForce = otherRigidbody.mass * collision.relativeVelocity;

                    // Call a Command to apply force on the server
                    CmdApplyForce(collisionForce, netId.netId);
                }
                else
                {
                    Debug.LogWarning("The other object does not have a Rigidbody.");
                }
            }
        }

        [Command(requiresAuthority = false)]
        void CmdApplyForce(Vector3 force, uint objectNetId)
        {
            // Find the object by its network ID on the server
            NetworkIdentity targetNetId = NetworkServer.spawned[objectNetId];

            if (targetNetId != null)
            {
                // Get the Rigidbody of the object and apply force
                Rigidbody targetRigidbody = targetNetId.GetComponent<Rigidbody>();

                if (targetRigidbody != null)
                {
                    targetRigidbody.AddForce(force, ForceMode.Impulse);
                }
                else
                {
                    Debug.LogWarning("Target object does not have a Rigidbody.");
                }
            }
            else
            {
                Debug.LogWarning("NetworkIdentity not found on the server.");
            }
        }
    }
}
