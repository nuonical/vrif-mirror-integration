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
            if (!useOfflineCharacterSelect)
                return;
            // Find the local player object
            var player = NetworkClient.localPlayer;

            if (player != null && player.isOwned)
            {
                // get the index of the player selected on the menu
                int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
                // spawn the player object at that index of networkCharacters
                CmdReplaceCharacter(playerIndex);
            }
        }

        [Command(requiresAuthority = false)] // command to the server to replace the character;
        public void CmdReplaceCharacter(int characterIndex)
        {            
            // Get the current player object
            GameObject emptyPlayerObject = gameObject;

            // Find the player's connection
            NetworkConnectionToClient conn = connectionToClient;
            // Destroy the empty character on the server
            NetworkServer.Destroy(emptyPlayerObject);
            // Instantiate the new character on the server and replace the player for connection 
            GameObject newCharacter = Instantiate(networkCharacters[characterIndex], emptyPlayerObject.transform.position, emptyPlayerObject.transform.rotation);         
            NetworkServer.ReplacePlayerForConnection(conn, newCharacter, true);           
        }
    }
}
