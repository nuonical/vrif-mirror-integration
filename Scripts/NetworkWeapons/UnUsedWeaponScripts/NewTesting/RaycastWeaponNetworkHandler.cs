using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    public class RaycastWeaponNetworkHandler : NetworkBehaviour
    {
        RaycastWeapon raycastWeapon;
        Transform _MuzzlePointTransform;
        float _maxRange;        
        LayerMask _ValidLayers;
        float _Damage;
        float _BulletImpactForce;
        GameObject _HitFXPrefab;
        private void Start()
        {
            raycastWeapon = GetComponent<RaycastWeapon>();
            // get all info from the raycast weapon override so it doesn't need filled out again
            _MuzzlePointTransform = raycastWeapon.MuzzlePointTransform;
            _maxRange = raycastWeapon.MaxRange;
            _ValidLayers = raycastWeapon.ValidLayers;
            _Damage = raycastWeapon.Damage;
            _BulletImpactForce = raycastWeapon.BulletImpactForce;
            _HitFXPrefab = raycastWeapon.HitFXPrefab;
        }
        // return a trigger value to the RaycastWeaponNetworkOveride OnTrigger override
        public float OwnerTriggerValue(float value)
        {
            // return a trigger value only if the object is owned
            if (!isOwned)
            {
                return 0f;
            }
            else
            {
                return Mathf.Clamp01(value);
            }
        }

        public void SendRayCastCommand()
        {
            if(isOwned)
            {
                CmdWeaponRaycast();
            }
        }

        // do raycast on the server only
        [Command]
        public void CmdWeaponRaycast()
        {
            // Raycast to hit
            RaycastHit hit;
            if (Physics.Raycast(_MuzzlePointTransform.position, _MuzzlePointTransform.forward, out hit, _maxRange, _ValidLayers, QueryTriggerInteraction.Ignore))
            {
                // raycastWeapon.OnRaycastHit(hit);
                // Damageable d = hit.collider.GetComponent<Damageable>();
                //  Debug.Log(hit.collider.name);
                //  if (d)
                // {
                //  d.GetComponent<NetworkSyncDamage>().SyncNetworkDamage(_Damage);                   
                // }
                OnRaycastHit(hit);
            }
        }

        public virtual void OnRaycastHit(RaycastHit hit)
        {
            // get the hit gameobject
            GameObject rootObject = hit.collider.transform.root.gameObject;
            // get the hit network id to pass to the rpc
            NetworkIdentity netId = rootObject.GetComponent<NetworkIdentity>();
            // get the child path to the hit collider so we can pass the index to the rpc
            string childPath = GetChildPath(hit.collider.transform, rootObject.transform);

            //ApplyParticleFX(hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal), hit.collider); // move to rpc
            ApplyParticleFX(hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal), netId, childPath);

            Rigidbody hitRigid = hit.collider.attachedRigidbody;
            if (hitRigid != null)
            {
                hitRigid.AddForceAtPosition(_BulletImpactForce * _MuzzlePointTransform.forward, hit.point);
            }

            // Damage if possible
            Damageable d = hit.collider.GetComponent<Damageable>();
            if (d)
            {
                // d.DealDamage(Damage, hit.point, hit.normal, true, gameObject, hit.collider.gameObject);              
                d.GetComponent<NetworkSyncDamage>().SyncNetworkDamage(_Damage);               
            }
        }

        // function to get the child path to the hit object so it can be passed to an rpc as a string
        string GetChildPath(Transform child, Transform root)
        {
            string path = child.name;

            while (child.parent != null && child.parent != root)
            {
                child = child.parent;
                path = child.name + "/" + path;
            }

            return path;
        }

        //public virtual void ApplyParticleFX(Vector3 position, Quaternion rotation, Collider attachTo)
        public virtual void ApplyParticleFX(Vector3 position, Quaternion rotation, NetworkIdentity netId, string childPath)
        {
            if (_HitFXPrefab)
            {
                // spawn the hit effects on all clients
                GameObject impact = Instantiate(_HitFXPrefab, position, rotation) as GameObject;
                NetworkIdentity impactNetId = impact.GetComponent<NetworkIdentity>();
                NetworkServer.Spawn(impact);

                // pass relative info to the clients to attach the prefab
                RpcAttachBulletHole(position, rotation, netId, impactNetId, childPath);
            }
        }

        Collider attachTo;

        [ClientRpc]
        public void RpcAttachBulletHole(Vector3 position, Quaternion rotation, NetworkIdentity netId, NetworkIdentity impactNetId, string childPath)
        {
            // if hit object has a network identity
            if (netId)
            {
                Debug.Log("Position: " + position + ", Rotation: " + rotation + ", NetId: " + netId.name + ", Child Path: " + childPath);

                //  Collider attachTo = null;
                GameObject impact = netId.gameObject;
                Transform childTransform = impact.transform.root.Find(childPath);
                // if the hit collider is a child transform
                if (childTransform != null)
                {
                    Debug.Log(childTransform.name);
                    attachTo = childTransform.GetComponent<Collider>();
                }
                // if the hit has no child colliders attach to the root collider
                else
                {
                    attachTo = impact.GetComponent<Collider>();
                }

                BulletHole hole = impactNetId.gameObject.GetComponent<BulletHole>();

                if (hole)
                {
                    Debug.Log("HoleisNotNull");
                    hole.TryAttachTo(attachTo);
                }
            }

            // if the hit object does not have a net id
            else if (!netId)
            {
                // we can't pass a game object or a collider to a client from the server without a Network ID, so the bullet hole may
                // need rewritten into a network Bullet hole to handle scale or put it here to handle that
                return;
            }
        }

        // sync the bullet in chamber bool on the raycast weapon
        [SyncVar(hook = nameof(SyncBulletInChamber))]
        bool _bulletInChamber;

        void SyncBulletInChamber(bool oldStutus, bool newStatus)
        {
            raycastWeapon.BulletInChamber = newStatus;
        }

        [Command]
        public void CmdSyncBulletInChamber(bool chamberStatus)
        {
            _bulletInChamber = chamberStatus;
        }
    }
}
