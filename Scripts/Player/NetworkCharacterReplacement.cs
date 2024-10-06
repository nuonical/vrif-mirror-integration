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
        //  [Header("Add all Player Prefabs to this list and Make sure they are added as Spawnable objects on the Network Manager")]
        //  public List<GameObject> networkCharacters;
        //  [Header("Add all body Textures availble for the model")]
        //  public List<Texture2D> bodyTexures;

        [System.Serializable]
        public class CharacterPrefabSet
        {
            public GameObject characterPrefab;
        }

        [Header("Add all Player Character Prefabs")]
        public List<CharacterPrefabSet> characterPrefabSets;

        [SerializeField] bool useOfflineCharacterSelect = true;

        private void Start()
        {
            if (!useOfflineCharacterSelect || !isLocalPlayer)
                return;

            // Get the index of the player prefab from LocalPlayerData
            int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
            int textureIndex = LocalPlayerData.Instance.playerTextureIndex; // Locally stored texture index

            // Send the prefab and texture index to the server to replace the character
            CmdReplaceCharacter(playerIndex, textureIndex);
        }

        [Command]
        public void CmdReplaceCharacter(int characterIndex, int textureIndex, NetworkConnectionToClient conn = null)
        {
            if (conn == null)
            {
                conn = connectionToClient;
            }

            if (characterIndex >= 0 && characterIndex < characterPrefabSets.Count)
            {
                // Get the current player object
                GameObject emptyPlayerObject = gameObject;

                // Destroy the empty player object on the server
                NetworkServer.Destroy(emptyPlayerObject);

                // Instantiate the selected character on the server
                GameObject newCharacter = Instantiate(characterPrefabSets[characterIndex].characterPrefab, emptyPlayerObject.transform.position, emptyPlayerObject.transform.rotation);

                // Assign ownership of the new character to the client's connection
                NetworkServer.ReplacePlayerForConnection(conn, newCharacter, true);

                // Pass the character and texture index to the newly spawned character
              //  NetworkCharcterAvatarSpawnSettings networkCharacter = newCharacter.GetComponent<NetworkCharcterAvatarSpawnSettings>();
              //  if (networkCharacter != null)
              //  {
                 //   networkCharacter.CmdSetCharacterTexture(characterIndex, textureIndex);
               // }
            }
            else
            {
                Debug.LogError("Invalid character index: " + characterIndex);
            }
        }
    }
}

