using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkBulletInsert : NetworkBehaviour
    {
        /// <summary>
        /// The weapon we will be adding Bullets to
        /// </summary>
        public RaycastWeapon Weapon;

        /// <summary>
        /// Only transforms that contains this name will be accepted as bullets
        /// </summary>
        public string AcceptBulletName = "Bullet";

        public AudioClip InsertSound;

        [SyncVar(hook = nameof(UpdateBulletCount))]
        bool countChanged = false;

        void OnTriggerEnter(Collider other)
        {
            if (!isOwned)
                return;
            Grabbable grab = other.GetComponent<Grabbable>();
            if (grab != null)
            {
                if (grab.transform.name.Contains(AcceptBulletName))
                {

                    // Weapon is full
                    if (Weapon.GetBulletCount() >= Weapon.MaxInternalAmmo)
                    {
                        return;
                    }

                    // Drop the bullet and add ammo to gun
                    grab.DropItem(false, true);
                    grab.transform.parent = null;
                    // GameObject.Destroy(grab.gameObject);
                    NetworkIdentity netId = grab.GetComponent<NetworkIdentity>();
                    CmdUdpateBullet(netId);


                    // Play Sound
                    if (InsertSound)
                    {
                        VRUtils.Instance.PlaySpatialClipAt(InsertSound, transform.position, 1f, 0.5f);
                    }
                }
            }
        }

        [Command]
        void CmdUdpateBullet(NetworkIdentity _netId)
        {
            // Up Ammo Count
            countChanged = !countChanged;
            NetworkServer.Destroy(_netId.gameObject);
        }

        // I don't think this will update to late joiners past adding a count might change to int sync var and move to a for each to get accurate count for late joiners
        void UpdateBulletCount(bool oldBool, bool newBool)
        {
            GameObject b = new GameObject();
            b.AddComponent<Bullet>();
            b.transform.parent = Weapon.transform;
        }
    }
}

