using BNG;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using UnityEngine;
public class BNGNetworkManager : NetworkManager {
    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn) {

        // Reset any objects belonging to disconnected clients
        NetworkIdentity[] copyOfOwnedObjects = conn.owned.ToArray();

        int moveCount = 0;
        // RemoveClientAuthority on everything but player object
        for (int x = 0; x < copyOfOwnedObjects.Length; x++) {
            if (copyOfOwnedObjects[x] != conn.identity) {
                copyOfOwnedObjects[x].RemoveClientAuthority();

                //NetworkGrabbable netGrab = copyOfOwnedObjects[x].GetComponent<NetworkGrabbable>();
                //if (netGrab != null) {
                //    netGrab.ResetInteractableVelocity();
                //}
                moveCount++;
            }
        }

        Debug.Log("Transferred ownership for " + moveCount);

        base.OnServerDisconnect(conn);
    }
}