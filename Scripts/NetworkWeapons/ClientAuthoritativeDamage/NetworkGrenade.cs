using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        public void ControlInput()
        {
            
            if (isOwned && !countDownStarted)
            {
                CmdServerExplosion();
            }

        }

        [Command]
        public void CmdServerExplosion()
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
                    RpcPlayBeepSound();
                }

                // Wait for 1 second
                yield return new WaitForSeconds(1f);
            }

            // Trigger the explosion after the countdown
            Explode();
        }

        void Explode()
        {
            // Play explosion sound
            // if using the emulator, this sound will trigger multiple times, but not in game headset
            RpcPlayExplosionSound();

            // Spawn explosion effect on all clients
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(explosionEffect);

            // Apply damage or other effects to objects within the explosion radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            // hashset to keep track of the damage component so it only gets damaged once
            HashSet<NetworkDamageable> damagedPlayers = new HashSet<NetworkDamageable>();

            foreach (Collider collider in colliders)
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Apply force to objects within the explosion radius
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }

                NetworkDamageable networkD = collider.transform.root.GetComponentInChildren<NetworkDamageable>();

                if (networkD != null && !damagedPlayers.Contains(networkD))
                {
                    networkD.TakeDamage(damageAmount);

                    damagedPlayers.Add(networkD);
                }
            }

            // Destroy the grenade after exploding
            Destroy(this.gameObject);
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


        [ClientRpc]
        void RpcPlayExplosionSound()
        {
            // Play explosion sound on all clients
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
        }
    }
}

