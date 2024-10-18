using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class NetworkPlayerHealth : NetworkBehaviour {

        [SerializeField]
        NetworkPlayer networkPlayer;

        public NetworkDamageable nD;

        // Spawn on death
        public GameObject PlayerRagdollPrefab;

        // Copy transforms from here to ragdoll
        public Transform LocalPlayerRig;

        PlayerTeleport playerTeleport;

        GameObject spawnedRagdoll;

        Grabber grabberLeft;
        Grabber grabberRight;

        public void SpawnPlayerRagdoll() {
            if (!isServer) return;
            RpcSpawnRagdoll();
        }

        public void OnPlayerDeath() {

            SpawnPlayerRagdoll();

            if (isOwned) {
                // Drop any items
                if (networkPlayer != null && networkPlayer.hardwareRig != null) {
                    networkPlayer.hardwareRig.GrabberLeft?.TryRelease();
                    networkPlayer.hardwareRig.GrabberRight?.TryRelease();
                }

                // Teleport player back to a random spawn point
                TeleportDestination[] spawnPoints = FindObjectsOfType<TeleportDestination>();
                if (spawnPoints.Length > 0) {
                    TeleportDestination tp = spawnPoints[Random.Range(0, spawnPoints.Length)];

                    if (playerTeleport == null) {
                        playerTeleport = FindObjectOfType<PlayerTeleport>();
                    }

                    if (tp != null && playerTeleport != null) {
                        playerTeleport.TeleportPlayerToTransform(tp.transform);
                    }
                }

                // Reset player health
                nD.Resurrect();
            }
        }

        [ClientRpc(includeOwner = true)]
        void RpcSpawnRagdoll() {
            // Instantiate PlayerRagdoll here, pose it, then sync to clients
            spawnedRagdoll = Instantiate(PlayerRagdollPrefab, transform.position + new Vector3(0, -0.875f,0), transform.rotation);
            //spawnedRagdoll = Instantiate(PlayerRagdollPrefab, transform.position, transform.rotation);


            // Loop through and match all of the bones / rotations to match the player's last rotations
            MatchRagdollBones(LocalPlayerRig, spawnedRagdoll.transform);

            // Destroy the object, or disable it to reuse later
            Destroy(spawnedRagdoll, 5f);
        }


        // This is called recursively to match all transform local position / rotations
        void MatchRagdollBones(Transform sourceRig, Transform ragdollRig) {
            for (int i = 0; i < sourceRig.childCount; i++) {
                Transform sourceBone = sourceRig.GetChild(i);
                Transform ragdollBone = ragdollRig.Find(sourceBone.name);

                if (ragdollBone != null) {
                    ragdollBone.localPosition = sourceBone.localPosition;
                    ragdollBone.localRotation = sourceBone.localRotation;

                    // Check if the bone has a MeshRenderer and copy the material if found
                    SkinnedMeshRenderer sourceRenderer = sourceBone.GetComponent<SkinnedMeshRenderer>();
                    SkinnedMeshRenderer ragdollRenderer = ragdollBone.GetComponent<SkinnedMeshRenderer>();

                    if (sourceRenderer != null && ragdollRenderer != null) {
                        ragdollRenderer.material = sourceRenderer.material;
                    }

                    // Check if rigidbody exists and add backwards force
                    Rigidbody rb = ragdollBone.GetComponent<Rigidbody>();
                    if(rb) {
                        rb.AddForce(-transform.forward * 5f, ForceMode.Impulse);
                    }

                    // Recursive call to handle all children of this bone
                    MatchRagdollBones(sourceBone, ragdollBone);
                }
            }
        }
    }
}
