using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    // add this component to the raycast weapon to send network damage via raycast hit, client authoritative
    public class RaycastWeaponNetworked : NetworkBehaviour
    {
        public RaycastWeapon raycastWeapon;
        public float damage;

        // Start is called before the first frame update
        void Start()
        {
            raycastWeapon = GetComponent<RaycastWeapon>();
            damage = raycastWeapon.Damage;
            raycastWeapon.onRaycastHitEvent.AddListener(DoNetworkDamage);        
            raycastWeapon.onShootEvent.AddListener(OnShoot);
            
        }


        [Command]
        public void CmdSyncCharge(bool setCharged)
        {
            RpcSyncCharge(setCharged);
        }

        [ClientRpc(includeOwner = false)]
        void RpcSyncCharge(bool _setCharged)
        {
            raycastWeapon.OnWeaponCharged(_setCharged);
        }

        void OnShoot()
        {
            if (isOwned)
            {
                CmdSyncShoot();
            }
        }

        [Command]
        void CmdSyncShoot()
        {
            RpcSyncShoot();
        }
        [ClientRpc(includeOwner = false)]
        void RpcSyncShoot()
        {
            raycastWeapon.Shoot();
        }

        void DoNetworkDamage(RaycastHit hit)
        {
            if (!isOwned)
                return;

            // look at the hit collider to try to get a network Id
            NetworkIdentity hitNetID = hit.collider.GetComponent<NetworkIdentity>();

            // if the hit collider does not have a net id look in the children
            if (hitNetID == null)
            {
                hitNetID = hit.collider.transform.root.GetComponentInChildren<NetworkIdentity>();
            }

            if (hitNetID)
            {
                CmdSyncNetworkDamage(hitNetID);
            }
        }

        [Command]
        void CmdSyncNetworkDamage(NetworkIdentity _netId)
        {
 
            NetworkDamageable nD = _netId.gameObject.GetComponent<NetworkDamageable>();

            if (nD == null)
            {
                nD = _netId.transform.root.GetComponentInChildren<NetworkDamageable>();
            }

            if (nD)
            {
                nD.TakeDamage(damage);
            }
        }  
    }
}
