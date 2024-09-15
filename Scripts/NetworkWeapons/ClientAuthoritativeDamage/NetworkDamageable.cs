using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkDamageable : NetworkBehaviour
    {
        public float maxHealth = 100;
        [SyncVar(hook = nameof(OnHealthChanged))]
        public float _currentHealth;

        [Tooltip("Activate these GameObjects on Death")]
        public List<GameObject> ActivateGameObjectsOnDeath;

        [Tooltip("Deactivate these GameObjects on Death")]
        public List<GameObject> DeactivateGameObjectsOnDeath;

        [Tooltip("Deactivate these Colliders on Death")]
        public List<Collider> DeactivateCollidersOnDeath;

        bool destroyed = false;

        private void Start()
        {
            _currentHealth = maxHealth;
        }

        // this is called only on the server and the currenthealth is synced back to clients   
        public void TakeDamage(float damageAmount)
        {
            if (destroyed)
                return;
            _currentHealth -= damageAmount;

            if (_currentHealth <= 0f)
            {               
                _currentHealth = 0f;
            }
        }

        // for client authoritative damage
        [Command(requiresAuthority = false)]
        public void CmdClientAuthorityTakeDamage(float damageAmount)
        {
            if (destroyed)
                return;
            _currentHealth -= damageAmount;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
            }
        }

        // function to add health like from a pickup
        [Command]
        public void Addhealth(float healthAmount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + healthAmount, 0, maxHealth);
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            // place any effects we want here like explosions etc

            // set destroyed on all clients
            if (_currentHealth <= 0f && !destroyed)
            {
                DestroyThis();
            }

            // add here to respawn or trigger player teleport on death etc
        }

        public void DestroyThis()
        {
            destroyed = true;

            // Activate
            foreach (var go in ActivateGameObjectsOnDeath)
            {
                go.SetActive(true);
            }

            // Deactivate
            foreach (var go in DeactivateGameObjectsOnDeath)
            {
                go.SetActive(false);
            }

            // Colliders
            foreach (var col in DeactivateCollidersOnDeath)
            {
                col.enabled = false;
            }
        }
    }
}

