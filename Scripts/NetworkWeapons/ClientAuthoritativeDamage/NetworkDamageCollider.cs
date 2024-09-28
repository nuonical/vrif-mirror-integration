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

        private void Start()
        {
            if (ColliderRigidbody == null)
            {
                ColliderRigidbody = GetComponent<Rigidbody>();
            }

            thisNetworkDamageable = GetComponent<NetworkDamageable>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!this.isActiveAndEnabled || !isOwned || hasDealtDamage)
            {
                return;
            }

            OnCollisionEvent(collision);
        }

        public virtual void OnCollisionEvent(Collision collision)
        {           
            LastDamageForce = collision.impulse.magnitude;
            LastRelativeVelocity = collision.relativeVelocity.magnitude;

            if (LastDamageForce >= MinForce)
            {
                // Can we damage what we hit?
                NetworkDamageable nD = collision.gameObject.GetComponent<NetworkDamageable>();

                if (nD && nD._currentHealth > 0)
                {                   
                    nD.CmdClientAuthorityTakeDamage(Damage);
                    hasDealtDamage = true; // Ensure that damage is only applied once per collision
                }
                // Otherwise, can we take damage ourselves from this collision?
                if (TakeCollisionDamage && thisNetworkDamageable != null && nD)
                {
                    thisNetworkDamageable.CmdClientAuthorityTakeDamage(CollisionDamage);
                    hasDealtDamage = true; // Ensure that damage is only applied once per collision
                }
            }
        }

        // Reset the flag when the collision ends
        private void OnCollisionExit(Collision collision)
        {
            hasDealtDamage = false;
        }
    }
}

