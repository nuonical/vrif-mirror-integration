using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public class NetworkMagazineSlide : NetworkBehaviour {

        // An optional GameObject that is activated when a magazine is inserted, and disabled when removed
        public GameObject MagazineInsertedGraphics;

        [Tooltip("Clip transform name must contain this to be considered valid")]
        public string AcceptableMagazineName = "Clip";

        [SyncVar(hook = nameof(UpdateMagazine))]
        public bool MagInserted = false;

        [SerializeField]
        RaycastWeaponNetworked parentRaycastWeapon;


        public bool OffsetMagOnEject = true;
        public Vector3 MagEjectOffset = new Vector3(0, -0.1f, 0);

        public GameObject MagazinePrefab;

        public AudioClip ClipAttachSound;
        public AudioClip ClipDettachSound;

        float lastEjectTime;

        public float EjectForce = 1f;

        public MagazineSlide.EjectDirectionOption EjectDirection = MagazineSlide.EjectDirectionOption.Down;


        void Start() {
            parentRaycastWeapon.raycastWeapon.onEjectMagazineEvent.AddListener(OnEjectMagazine);

            // Using internal ammo for easier network simulation
            parentRaycastWeapon.raycastWeapon.ReloadMethod = ReloadType.InternalAmmo;
        }

        void OnEjectMagazine() {
            if (isOwned) {
                EjectMagazine();
            }
        }

        public virtual void EjectMagazine() {
            if(!MagInserted) {
                return;
            }            

            lastEjectTime = Time.time;

            int remainingInternalAmmo = (int)parentRaycastWeapon.raycastWeapon.InternalAmmo;


            CmdEjectMagazine(remainingInternalAmmo);

            // Update local magazine status
            CmdUpdateMagSlide(false);
        }

        [Command]
        public virtual void CmdEjectMagazine(int remainingAmmo) {
            if (!MagInserted) return;

            // Set internal ammo to 0 now that we've ejected the magazine
            parentRaycastWeapon.SetInternalAmmoCount(0);

            // Instantiate a new magazine over the network with the remaining bullets
            GameObject ejectedMagazine = Instantiate(MagazinePrefab, transform.position, transform.rotation);

            // Apply force before spawning to avoid losing momentum
            Rigidbody ejectRigid = ejectedMagazine.GetComponent<Rigidbody>();
            if (ejectedMagazine != null && ejectRigid != null) {

                // Move clip down before we eject it
                if (OffsetMagOnEject) {
                    ejectedMagazine.transform.parent = transform;
                    ejectedMagazine.transform.localPosition = MagEjectOffset;
                }

                // Eject with physics force before spawning
                ejectedMagazine.transform.parent = null;
                //ejectRigid.velocity = Vector3.zero;

                if (EjectForce != 0) {
                    Vector3 ejectDirection = Vector3.zero;

                    if (EjectDirection == MagazineSlide.EjectDirectionOption.Down) {
                        ejectDirection = -ejectedMagazine.transform.up;
                    } 
                    else if (EjectDirection == MagazineSlide.EjectDirectionOption.Back) {
                        ejectDirection = -ejectedMagazine.transform.forward;
                    }

                    ejectRigid.AddForce(ejectDirection * EjectForce, ForceMode.VelocityChange);
                }
            }

            // Spawn the ejected magazine on the network so all clients see it
            NetworkServer.Spawn(ejectedMagazine);

            // Update the bullet count on the ejected magazine based on the remaining ammo before we transfer ownership
            NetworkMagazine ejectedNetMagazine = ejectedMagazine.GetComponent<NetworkMagazine>();
            if (ejectedNetMagazine != null) {
                ejectedNetMagazine.SetBulletCount(remainingAmmo);
            }

            // Optionally? transfer authority to the client after the spawn and force application
            NetworkIdentity netID = ejectedMagazine.GetComponent<NetworkIdentity>();
            if (netID != null && connectionToClient != null) {
                netID.AssignClientAuthority(connectionToClient);  // Transfer authority to the client
            }

            // Reset the magazine state
            MagInserted = false;

            // Hide the magazine inserted graphics
            if (MagazineInsertedGraphics != null) {
                MagazineInsertedGraphics.SetActive(false);
            }
        }

        // Called on MagInserted Change
        void UpdateMagazine(bool oldValue, bool newValue) {

            // Show / Hide Graphics
            if (MagazineInsertedGraphics != null) {
                MagazineInsertedGraphics.SetActive(newValue);
            }
        }

        [Command(requiresAuthority = false)]
        void CmdUpdateMagSlide(bool magazineInsertedStatus) {
            MagInserted = magazineInsertedStatus;
        }
        
        void OnTriggerEnter(Collider other) {
            if (!isOwned || MagInserted)
                return;

            // Too soon to reinsert
            if (Time.time - lastEjectTime < 1f) {
                return;
            }

            NetworkMagazine netMagazine = other.GetComponent<NetworkMagazine>();

            if (netMagazine != null) {
                if (netMagazine.transform.name.StartsWith(AcceptableMagazineName)) {

                    // Drop the magazine and add it to the weapon
                    int ammoToAdd = netMagazine.CurrentBulletCount;
                    parentRaycastWeapon.SetInternalAmmoCount(ammoToAdd);

                    // Drop the bullet and add ammo to gun
                    NetworkGrabbable networkGrab = other.GetComponent<NetworkGrabbable>();
                    networkGrab.DropAndDestroyItem();

                    CmdUpdateMagSlide(networkGrab.NetIdentity);

                    // Play Sound
                    if (ClipAttachSound != null && Time.timeSinceLevelLoad > 0.1f) {
                        VRUtils.Instance.PlaySpatialClipAt(ClipAttachSound, transform.position, 0.5f);
                    }
                }
            }
        }
    }
}

