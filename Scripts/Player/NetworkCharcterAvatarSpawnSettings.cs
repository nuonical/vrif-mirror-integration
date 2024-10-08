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
            public List<Texture2D> propTextures;
            public Material propMaterial;
        }

        [Header("Add all Player Character Textures and Materials")]
        [Tooltip("This index should match the prefab index on NetworkCharacterReplacement, if character prefab is index 0, then the texture set should also be index 0")]
        public List<CharacterTextureSet> characterTextureSets;

        private void Start()
        {

            if(isOwned)
            {
                // Get the index of the player prefab from LocalPlayerData
                int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
                int textureIndex = LocalPlayerData.Instance.playerTextureIndex; // Locally stored texture index
         
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

                // set the body materails
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
                            // compare the found material to the material in the prefab, if they match, change it
                            if (materials[i].name == textureSet.bodyMaterial.name + " (Instance)")
                            {
                                materials[i] = newBodyMaterial;
                            }
                        }
                        renderer.materials = materials;
                    }
                }

                // set the prob materials
                if (textureIndex >= 0 && textureIndex < textureSet.propTextures.Count)
                {
                    Texture2D selectedTexture = textureSet.propTextures[textureIndex];

                    // Create a new material instance and apply the texture
                    Material newPropMaterial = new Material(textureSet.propMaterial);
                    newPropMaterial.mainTexture = selectedTexture;

                    // Apply the new material to the character renderers
                    Renderer[] renderers = GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.materials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            // compare the found material to the material in the prefab, if they match, change it
                            if (materials[i].name == textureSet.propMaterial.name + " (Instance)")
                            {
                                materials[i] = newPropMaterial;
                            }
                        }
                        renderer.materials = materials;
                    }
                }

            }
        }

        // command to the server to set the index so late joiners will get the index form the synvar hooks
        [Command(requiresAuthority = false)]
        public void CmdSetCharacterTexture(int characterIndex, int textureIndex)
        {
            syncedCharacterIndex = characterIndex;
            syncedTextureIndex = textureIndex;
        }
    }
}
