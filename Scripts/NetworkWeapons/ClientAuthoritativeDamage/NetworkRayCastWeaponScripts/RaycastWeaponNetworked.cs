using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG {
    // add this component to the raycast weapon to send network damage via raycast hit, client authoritative
    public class RaycastWeaponNetworked : NetworkBehaviour {
        public RaycastWeapon raycastWeapon;
        public float damage;

        [SyncVar(hook = nameof(InternalAmmoCountChanged))]
        public int InternalAmmoCount = 0;

        // Start is called before the first frame update
        void Start() {
            raycastWeapon = GetComponent<RaycastWeapon>();
            damage = raycastWeapon.Damage;
            raycastWeapon.onRaycastHitEvent.AddListener(DoNetworkDamage);
            raycastWeapon.onShootEvent.AddListener(OnShoot);
            raycastWeapon.onWeaponChargedEvent.AddListener(OnWeaponCharged);

        }

        [Command]
        public void CmdSyncCharge(bool setCharged) {
            RpcSyncCharge(setCharged);
        }

        [ClientRpc(includeOwner = false)]
        void RpcSyncCharge(bool _setCharged) {
            raycastWeapon.OnWeaponCharged(_setCharged);
        }

        void OnWeaponCharged() {
            SetInternalAmmoCount((int)raycastWeapon.InternalAmmo);
        }

        void OnShoot() {

            // Update Server with our new value
            SetInternalAmmoCount((int)raycastWeapon.InternalAmmo);

            // Tell others we shot so they can play a sound, muzzle flash, etc
            if (isOwned) {
                CmdSyncShoot();
            }
        }

        [Command]
        void CmdSyncShoot() {

            InternalAmmoCount = (int)raycastWeapon.InternalAmmo;

            RpcSyncShoot();
        }

        [ClientRpc(includeOwner = false)]
        void RpcSyncShoot() {
            raycastWeapon.Shoot();
            raycastWeapon.DoMuzzleFlash();
        }

        public void SetInternalAmmoCount(int ammoCount) {
            if (isServer) {
                raycastWeapon.InternalAmmo = ammoCount;

                // SyncVar for syncing to clients
                InternalAmmoCount = ammoCount;
            }
            else if (isOwned) {
                // Send command to the server to update ammo
                CmdSetInternalAmmoCount(ammoCount);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetInternalAmmoCount(int ammoCount) {
            raycastWeapon.InternalAmmo = ammoCount;
            InternalAmmoCount = ammoCount;  // SyncVar will sync this to clients
        }

        // Called when InternalAmmoCount has been updated
        public void InternalAmmoCountChanged(int oldValue, int newValue) {

            // Update our client data
            raycastWeapon.InternalAmmo = newValue;
        }

        void DoNetworkDamage(RaycastHit hit) {
            if (!isOwned)
                return;

            // First check for a hitbox to send the info up to
            NetworkHitbox hb = hit.collider.GetComponent<NetworkHitbox>();

            // First check hitbox
            if (hb != null) {
                // look at the hit collider to try to get a network Id
                //hitNetID = hb.parentDamageable.GetComponent<NetworkIdentity>();
                // NetworkDamageable nD = hit.collider.GetComponent<NetworkDamageable>();
                NetworkDamageable nD = hb.parentDamageable.GetComponent<NetworkDamageable>();

                if (nD && nD._currentHealth > 0) {
                    nD.CmdClientAuthorityTakeDamage(damage * hb.DamageMultiplier);
                }
            }
            else {
                NetworkIdentity hitNetID = hit.collider.GetComponent<NetworkIdentity>();

                // if the hit collider does not have a net id look in the children
                if (hitNetID == null) {
                    hitNetID = hit.collider.transform.root.GetComponentInChildren<NetworkIdentity>();
                }

                if (hitNetID) {
                    CmdSyncNetworkDamage(hitNetID);
                }
            }
        }

        [Command]
        void CmdSyncNetworkDamage(NetworkIdentity _netId) {

            NetworkDamageable nD = _netId.gameObject.GetComponent<NetworkDamageable>();

            if (nD == null) {
                nD = _netId.transform.root.GetComponentInChildren<NetworkDamageable>();
            }

            if (nD) {
                nD.TakeDamage(damage);
            }
        }
    }
}
