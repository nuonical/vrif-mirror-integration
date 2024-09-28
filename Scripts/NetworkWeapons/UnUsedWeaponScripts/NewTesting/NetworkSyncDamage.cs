using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    public class NetworkSyncDamage : NetworkBehaviour
    {
        public Damageable damageable;

        [SyncVar(hook = nameof(SyncDamage))]
        public float damage;

        // Start is called before the first frame update
        void Start()
        {
            damageable = GetComponent<Damageable>();
        }

        public void SyncNetworkDamage(float _damage)
        {
            damage = _damage;
        }

        public void SyncDamage(float oldDamage, float newDamage)
        {
            if(damageable)
            {
                damageable.DealDamage(newDamage);
            }
        }
 
    }
}
