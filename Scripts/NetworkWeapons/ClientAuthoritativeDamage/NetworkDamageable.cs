using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace BNG
{
    public class NetworkDamageable : NetworkBehaviour
    {
        public float maxHealth = 100;
        [SyncVar(hook = nameof(OnHealthChanged))]
        public float _currentHealth;

        [Tooltip("Instantiate these Network Objects  on Death")]
        public List<GameObject> InstantiateNetworkObjectsOnDeath;

        [Tooltip("Instantiate these UnNetworked GameObjects on Death")]
        public List<GameObject> InstantiateGameObjectsOnDeath;

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

        // for client authoritative damage, call this instead of Take Damage if doing Client Authoritative Damage
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
            // set destroyed on all clients
            if (_currentHealth <= 0f && !destroyed)
            {
                DestroyThis();
            }

            // add here to respawn or trigger player teleport on death etc 
            // recommend making player death and teleport its own Class, as it is not networked and is handled on the Local XRRig Player
        }

        public void DestroyThis()
        {
            destroyed = true;
            // Instantiate network objects
            if (isServer)
            {
                SpawnNetworkObject();
            }
            // Instantiate unnetworked objects like destroyed objects that don't need networked
            foreach (var go in InstantiateGameObjectsOnDeath)
            {
                // Instantiate the object at the current position and rotation
                GameObject instantiatedObject = Instantiate(go, transform.position, transform.rotation);

                // this is needed for physics subscenes
                Scene currentScene = gameObject.scene;
                // Move the instantiated object to the current scene
                SceneManager.MoveGameObjectToScene(instantiatedObject, currentScene);
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

        [Server]
        void SpawnNetworkObject()
        {
            foreach (var go in InstantiateNetworkObjectsOnDeath)
            {
                GameObject spawnedGo = Instantiate(go, transform.position, transform.rotation);

                // needed for if we are using subscenes so the object is spawned in the current scene
                Scene currentScene = gameObject.scene; 
                SceneManager.MoveGameObjectToScene(spawnedGo, currentScene);

                NetworkServer.Spawn(spawnedGo);
                
            }
        }
    }
}

