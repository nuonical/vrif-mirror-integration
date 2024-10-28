using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    public class NetworkDamageCollider : NetworkBehaviour
    {
        public float Damage = 25f;
        public Rigidbody ColliderRigidbody;
        public float MinForce = 0.1f;
        public float LastRelativeVelocity = 0;
        public float LastDamageForce = 0;
        public bool TakeCollisionDamage = false;
        public float CollisionDamage = 5;

        private NetworkDamageable thisNetworkDamageable;

        // Keeps track of when damage was last applied to avoid repeated hits in the same collision
        private bool hasDealtDamage = false;

        private NetworkGrabbable netGrab;

        private void Start()
        {
            if (ColliderRigidbody == null)
            {
                ColliderRigidbody = GetComponent<Rigidbody>();
            }

            thisNetworkDamageable = GetComponent<NetworkDamageable>();
            netGrab = GetComponent<NetworkGrabbable>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!this.isActiveAndEnabled || !isOwned || hasDealtDamage)
            {
                return;
            }
            if (ColliderRigidbody.velocity.magnitude > 0.1f)
            {
                OnCollisionEvent(collision);
            }
        }

        public virtual void OnCollisionEvent(Collision collision)
        {
            Debug.Log(collision.collider.name);

            LastDamageForce = collision.impulse.magnitude;
            LastRelativeVelocity = collision.relativeVelocity.magnitude;

            // Get the NetworkIdentity of the local player (weapon holder)
            NetworkIdentity localPlayerIdentity = NetworkClient.connection.identity;


            // First check for a hitbox to send the info up to
            NetworkHitbox hb = collision.collider.GetComponent<NetworkHitbox>();
           
            NetworkIdentity collidedNetworkIdentity = GetClosestNetworkIdentity(collision.transform);
            
            float multiplier = 1f;

            if (hb != null && collidedNetworkIdentity == null) {
                collidedNetworkIdentity = hb.parentDamageable.GetComponent<NetworkIdentity>();
                multiplier = hb.DamageMultiplier;
            }

            if(collidedNetworkIdentity == null)
            {
                return;
            }

            // Check if the object we collided with has a NetworkIdentity and compare netId
            if (collidedNetworkIdentity != null && localPlayerIdentity != null)
            {
                if (collidedNetworkIdentity.netId == localPlayerIdentity.netId)
                {
                    // Don't apply damage to ourselves (the local player)
                    Debug.Log("Avoiding self-damage.");
                    return;
                }
            }

            if (LastDamageForce >= MinForce)
            {
                // Can we damage what we hit?
                NetworkDamageable nD = collidedNetworkIdentity.transform.GetComponentInChildren<NetworkDamageable>();

                if (nD && nD._currentHealth > 0)
                {
                    nD.CmdClientAuthorityTakeDamage(Damage * multiplier);
                    hasDealtDamage = true; // Ensure that damage is only applied once per collision
                }

                // Otherwise, can we take damage ourselves from this collision?
                if (TakeCollisionDamage && thisNetworkDamageable != null && nD)
                {
                    thisNetworkDamageable.CmdClientAuthorityTakeDamage(CollisionDamage * multiplier);
                    hasDealtDamage = true; // Ensure that damage is only applied once per collision
                }
            }
        }

        // function to traverse up the hierchy to find the first Network ID, this will allow for parenting all damagable objects to one item parent
        public NetworkIdentity GetClosestNetworkIdentity(Transform currentTransform)
        {
            // Traverse upwards through the hierarchy
            while (currentTransform != null)
            {
                // Check if the current object has a NetworkIdentity
                NetworkIdentity networkIdentity = currentTransform.GetComponent<NetworkIdentity>();
                if (networkIdentity != null)
                {
                    return networkIdentity; // Return the first NetworkIdentity found
                }

                // Move up to the parent
                currentTransform = currentTransform.parent;
            }

            // Return null if no NetworkIdentity is found
            return null;
        }

        // Reset the flag when the collision ends
        private void OnCollisionExit(Collision collision)
        {
            hasDealtDamage = false;
        }
    }
}
