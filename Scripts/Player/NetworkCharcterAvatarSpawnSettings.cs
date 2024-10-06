using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkCharcterAvatarSpawnSettings : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnTextureChanged))]
        public int syncedTextureIndex;

        [SyncVar(hook = nameof(OnCharacterChanged))]
        public int syncedCharacterIndex;

        [System.Serializable]
        public class CharacterTextureSet
        {
            public List<Texture2D> bodyTextures;
            public Material bodyMaterial;
        }

        [Header("Add all Player Character Textures and Materials")]
        public List<CharacterTextureSet> characterTextureSets;

        private void Start()
        {
          //  if (isServer)
           // {
                // Initialize default texture/character index
             //   syncedTextureIndex = 0;
             //   syncedCharacterIndex = 0;
           // }

           // if (isClient)
           // {
                // Apply textures and materials when the client starts
              //  ApplyMaterialsAndTextures(syncedCharacterIndex, syncedTextureIndex);
           // }

            if(isOwned)
            {
                // Get the index of the player prefab from LocalPlayerData
                int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
                int textureIndex = LocalPlayerData.Instance.playerTextureIndex; // Locally stored texture index

                // ApplyMaterialsAndTextures(playerIndex, textureIndex);
                CmdSetCharacterTexture(playerIndex, textureIndex);
            }

            else if(!isOwned)
            {
               ApplyMaterialsAndTextures(syncedCharacterIndex, syncedTextureIndex);
            }
        }

        private void OnTextureChanged(int oldTextureIndex, int newTextureIndex)
        {
            ApplyMaterialsAndTextures(syncedCharacterIndex, newTextureIndex);
        }

        private void OnCharacterChanged(int oldCharacterIndex, int newCharacterIndex)
        {
            ApplyMaterialsAndTextures(newCharacterIndex, syncedTextureIndex);
        }

        private void ApplyMaterialsAndTextures(int characterIndex, int textureIndex)
        {
            if (characterIndex >= 0 && characterIndex < characterTextureSets.Count)
            {
                CharacterTextureSet textureSet = characterTextureSets[characterIndex];

                if (textureIndex >= 0 && textureIndex < textureSet.bodyTextures.Count)
                {
                    Texture2D selectedTexture = textureSet.bodyTextures[textureIndex];

                    // Create a new material instance and apply the texture
                    Material newBodyMaterial = new Material(textureSet.bodyMaterial);
                    newBodyMaterial.mainTexture = selectedTexture;

                    // Apply the new material to the character renderers
                    Renderer[] renderers = GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.materials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i].name == textureSet.bodyMaterial.name + " (Instance)")
                            {
                                materials[i] = newBodyMaterial;
                            }
                        }
                        renderer.materials = materials;
                    }
                }

                
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetCharacterTexture(int characterIndex, int textureIndex)
        {
            syncedCharacterIndex = characterIndex;
            syncedTextureIndex = textureIndex;
        }
    }
}
