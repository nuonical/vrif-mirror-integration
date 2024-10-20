using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace BNG
{
    public class NetworkGrenade : NetworkBehaviour
    {
        [SerializeField] float explosionRadius = 5f;
        [SerializeField] float explosionForce = 1000f;
        public GameObject explosionEffectPrefab;

        public AudioClip explosionSound;
        [SerializeField] float explosionVolume = 1f;


        [SerializeField] float damageAmount;

        [SerializeField] int explosionCountdown = 3;

        public AudioClip beepSound; // sound that plays with the count down like a beep on a frag grenade or tick on a bomb

        // synced so someone else can't pick it up and trigger the audio again etc..
        [SyncVar]
        bool countDownStarted = false;

        // for getting the current physics scene, needed for phyics subscenes for raycast etc..
        PhysicsScene currentPhysicsScene;

        // is this a custom physics scene/ if we are using physics subscenes
        bool isSubScene = false;

        private void Start()
        {
            currentPhysicsScene = gameObject.scene.GetPhysicsScene();
            isSubScene = gameObject.scene.GetPhysicsScene() != Physics.defaultPhysicsScene;
        }

        public void ControlInput()
        {
            
            if (isOwned && !countDownStarted)
            {
                ServerExplosion();
            }

        }

        //[Command]
        public void ServerExplosion()
        {
            countDownStarted = true;
            StartCoroutine(CountDown());
        }

        IEnumerator CountDown()
        {
            for (int i = 0; i < explosionCountdown; i++)
            {
                // Play beep sound
                if (beepSound)
                {
                    Debug.Log("beep started");
                    CmdPlayBeepSound();
                }

                // Wait for 1 second
                yield return new WaitForSeconds(1f);
            }
            
            ExplodeDefault();
            yield return null;
            

        }

        void ExplodeDefault()
        {
            // Play explosion sound
            // if using the emulator, this sound will trigger multiple times, but not in game headset
            CmdDoExplosiontFX();

            // Apply damage or other effects to objects within the explosion radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
           
            // hashset to keep track of the damage component so it only gets damaged once
            HashSet<NetworkDamageable> damagedPlayers = new HashSet<NetworkDamageable>();

            foreach (Collider collider in colliders)
            {
                Debug.Log(collider);
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Apply force to objects within the explosion radius
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }

                NetworkDamageable networkD = GetClosestNetworkDamageable(collider.transform);

                if (networkD != null && !damagedPlayers.Contains(networkD))
                {
                    networkD.CmdClientAuthorityTakeDamage(damageAmount);

                    damagedPlayers.Add(networkD);
                }
            }

        }

        public NetworkDamageable GetClosestNetworkDamageable(Transform currentTransform)
        {
            // Traverse upwards through the hierarchy
            while (currentTransform != null)
            {
                // Check if the current object has a NetworkIdentity
                NetworkDamageable networkDamageable = currentTransform.GetComponent<NetworkDamageable>();
                if (networkDamageable != null)
                {
                    return networkDamageable; // Return the first NetworkIdentity found
                }

                // Move up to the parent
                currentTransform = currentTransform.parent;
            }

            // Return null if no NetworkIdentity is found
            return null;
        }

        [Command]
        public void CmdPlayBeepSound()
        {
            RpcPlayBeepSound();
        }
      
        [ClientRpc]
        void RpcPlayBeepSound()
        {
            // Play beep sound on all clients
            if (beepSound)
            {
                AudioSource.PlayClipAtPoint(beepSound, transform.position);
            }
        }

        [Command]
        public void CmdDoExplosiontFX()
        {
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(explosionEffect);
            RpcPlayExplosionSound();
            
        }

        void RpcPlayExplosionSound()
        {
            // Play explosion sound on all clients
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
            Destroy(this.gameObject);
        }
    }
}

