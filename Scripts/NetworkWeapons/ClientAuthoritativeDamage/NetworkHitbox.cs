using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class NetworkHitbox : MonoBehaviour {

        [SerializeField]
        public NetworkDamageable parentDamageable;

        public float DamageMultiplier = 1.0f;
    }
}

