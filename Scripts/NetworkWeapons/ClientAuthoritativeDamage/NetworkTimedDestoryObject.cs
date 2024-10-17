using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
// use this on spawned objects that need destoryed after time like particle effects
namespace BNG
{
    public class NetworkTimedDestoryObject : NetworkBehaviour
    {
        [SerializeField] float delayTime = 3f;
        
       // [Server]
        void Start()
        {
            if (!NetworkServer.active)
                return;
            if (isServer)
            {
                StartCoroutine(DestoryObjectDelay());
            }
        }

        IEnumerator DestoryObjectDelay()
        {
            yield return new WaitForSeconds(delayTime);
            NetworkServer.Destroy(this.gameObject);
        }
    }
}
