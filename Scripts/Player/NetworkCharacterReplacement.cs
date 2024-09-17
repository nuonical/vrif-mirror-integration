using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    public class NetworkCharacterReplacement : NetworkBehaviour
    {
        public List<GameObject> networkCharacters;

        [Command] // Ensure this runs on the server
        public void CmdReplaceCharacter(int characterIndex)
        {
            if (!isServer) return; // Make sure this only runs on the server

            // Get the current player object
            GameObject currentPlayer = gameObject;

            // Find the player's connection
            NetworkConnectionToClient conn = connectionToClient;

            // Destroy the current character on the server
            NetworkServer.Destroy(currentPlayer);

            // Instantiate the new character on the server
            GameObject newCharacter = Instantiate(networkCharacters[characterIndex], currentPlayer.transform.position, currentPlayer.transform.rotation);

            // Spawn the new character on the server
            NetworkServer.Spawn(newCharacter, conn);

            // Optionally: Transfer ownership to the new character
            newCharacter.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
        }
    }
}
