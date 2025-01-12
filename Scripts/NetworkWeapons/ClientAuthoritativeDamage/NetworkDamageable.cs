using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.Events;

namespace BNG
{
    public class NetworkDamageable : NetworkBehaviour
    {
        public float maxHealth = 100;

        [SyncVar(hook = nameof(OnHealthChanged))]
        public float _currentHealth;

        [Tooltip("If true the object will be destroyed and removed from the scene. Set to false if you want to handle removal or resetting manuallyt via onDeathEvent")]
        public bool DestroyOnDeath = true;

        [Tooltip("Instantiate these Network Objects on Death")]
        public List<GameObject> InstantiateNetworkObjectsOnDeath;

        [Tooltip("Instantiate these UnNetworked GameObjects on Death")]
        public List<GameObject> InstantiateGameObjectsOnDeath;

        [Tooltip("Deactivate these GameObjects on Death")]
        public List<GameObject> DeactivateGameObjectsOnDeath;

        [Tooltip("Deactivate these Colliders on Death")]
        public List<Collider> DeactivateCollidersOnDeath;

        [SyncVar]
        public bool destroyed = false;

        private bool spawnedFlag = false;

        [Tooltip("Unity Event called by owner on death")]
        public UnityEvent onDeathEvent;

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Initialize health on the server only
            _currentHealth = maxHealth;
           
        }


        // This is called only on the server, and the current health is synced back to clients
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

        // For client-authoritative damage, call this instead of TakeDamage if doing client-authoritative damage
        [Command(requiresAuthority = false)]
        public void CmdClientAuthorityTakeDamage(float damageAmount)
        {
            if (destroyed || _currentHealth <= 0)
                return;

            _currentHealth -= damageAmount;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
            }
        }

        // Function to add health like from a pickup
        [Command(requiresAuthority = false)]
        public void AddHealth(float healthAmount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + healthAmount, 0, maxHealth);

            if(destroyed && _currentHealth > 0) {
                // Resurrected
                destroyed = false;
            }
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            // Handle health changes on clients
            if (_currentHealth <= 0f && !destroyed)
            {
                DoDeath();
            }
        }

        public void Resurrect() {
            AddHealth(maxHealth);
            // _currentHealth = maxHealth;
            
        }

        public void DoDeath() {

            // Already dead
            if(destroyed) {
                return;
            }

          //  destroyed = true;

            if (DestroyOnDeath) {
                DestroyThis();
            }
            
            // Call any local events
            onDeathEvent?.Invoke();
        }

        public void DestroyThis()
        {
            //destroyed = true;
            CmdSetDestroyed();

            // Spawn networked objects if needed
            CmdSpawnNetworkObject();

            // Instantiate unnetworked objects this will likely not be needed in multiplayer.. but I left it in
            foreach (var go in InstantiateGameObjectsOnDeath)
            {
                GameObject instantiatedObject = Instantiate(go, transform.position, transform.rotation);

                // Move the instantiated object to the current scene
                Scene currentScene = gameObject.scene;
                SceneManager.MoveGameObjectToScene(instantiatedObject, currentScene);
            }

            // Deactivate objects on clients
            foreach (var go in DeactivateGameObjectsOnDeath)
            {
                go.SetActive(false);
            }

            // Disable colliders on clients
            foreach (var col in DeactivateCollidersOnDeath)
            {
                col.enabled = false;
            }
        }

        [Command(requiresAuthority = false)]
        void CmdSpawnNetworkObject()
        {
            if (spawnedFlag)
                return;

            // Deactivate objects on the server
            foreach (var go in DeactivateGameObjectsOnDeath)
            {
                go.SetActive(false);
            }

            // deactivate colliders on the server
            foreach (var col in DeactivateCollidersOnDeath)
            {
                col.enabled = false;
            }

            // Instantiate and spawn networked objects
            foreach (var go in InstantiateNetworkObjectsOnDeath)
            {
                GameObject spawnedGo = Instantiate(go, transform.position, transform.rotation);
                            
                // Move to the current scene (important for subscenes)
                Scene currentScene = gameObject.scene;
                SceneManager.MoveGameObjectToScene(spawnedGo, currentScene);

                NetworkServer.Spawn(spawnedGo);
            }

            spawnedFlag = true;
        }

        [Command]
        public void CmdSetDestroyed()
        {
            destroyed = true;
        }

    }
}

