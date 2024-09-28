using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace BNG
{
    // this script is placed on the empty player object to hold all the possible player prefabs, when this spawns as the player it is
    // sent an index / int from the menu  for the index of the prefab the player wants
    public class NetworkCharacterReplacement : NetworkBehaviour
    {
        [Header("Add all Player Prefabs to this list and Make sure they are added as Spawnable objects on the Network Manager")]
        public List<GameObject> networkCharacters;

        [SerializeField] bool useOfflineCharacterSelect = true;

        private void Start()
        {
            if (!useOfflineCharacterSelect || !isLocalPlayer) // Ensure only local player initiates this process
                return;

            // Get the index of the player selected in the menu
            int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
            CmdReplaceCharacter(playerIndex);
        }

        [Command] // Send command to the server to replace the character for this connection
        public void CmdReplaceCharacter(int characterIndex, NetworkConnectionToClient conn = null)
        {
            // Default to this player's connection if none provided
            if (conn == null)
            {
                conn = connectionToClient;
            }

            // Get the current player object
            GameObject emptyPlayerObject = gameObject;

            // Check if the character index is valid
            if (characterIndex >= 0 && characterIndex < networkCharacters.Count)
            {
                // Destroy the empty player object on the server
                NetworkServer.Destroy(emptyPlayerObject);

                // Instantiate the selected character on the server
                GameObject newCharacter = Instantiate(networkCharacters[characterIndex], emptyPlayerObject.transform.position, emptyPlayerObject.transform.rotation);

                // Assign ownership of the new character to the client's connection
                NetworkServer.ReplacePlayerForConnection(conn, newCharacter, true);
            }
            else
            {
                Debug.LogError("Invalid character index: " + characterIndex);
            }
        }
    }
}

