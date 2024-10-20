using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class BNGAdditiveLookAtMainCamera : MonoBehaviour
{
    // This will be enabled by Portal script in OnStartClient
    void OnValidate()
    {
        this.enabled = false;
    }

    // LateUpdate so that all camera updates are finished.
    [ClientCallback]
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
