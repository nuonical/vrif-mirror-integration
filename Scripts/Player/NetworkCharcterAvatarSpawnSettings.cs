using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkCharcterAvatarSpawnSettings : NetworkBehaviour
    {
        // struct to store the local player data settings on spawn
        [System.Serializable]
        public struct TextureIndexes
        {
            public int bodyTextureIndex;
            public int propTextureIndex;
        }

        [SyncVar(hook = nameof(OnTextureIndicesChanged))]
        public TextureIndexes syncedTextureIndices;

        [SyncVar(hook = nameof(OnCharacterChanged))]
        public int syncedCharacterIndex;

        [System.Serializable]
        public class CharacterTextureSet
        {
            [Header("All body textures available for the Avatar")]
            public List<Texture2D> bodyTextures;
            [Header("The body material on the avatar prefab")]
            public Material bodyMaterial;
            [Header("All prop textures available for the prefab")]
            public List<Texture2D> propTextures;
            [Header("The prop material on the avatar prefab ")]
            public Material propMaterial;
        }

        [Header("Add all Player Character Textures and Materials")]
        [Tooltip("This index should match the prefab index on NetworkCharacterReplacement, if character prefab is index 0, then the texture set should also be index 0")]
        public List<CharacterTextureSet> characterTextureSets;

        private void Start()
        {
            if (isOwned)
            {
                // Get the index of the player prefab from LocalPlayerData
                int playerIndex = LocalPlayerData.Instance.playerPrefabIndex;
                TextureIndexes textureIndices = new TextureIndexes
                {
                    bodyTextureIndex = LocalPlayerData.Instance.bodyTextureIndex,  // Locally stored body texture index
                    propTextureIndex = LocalPlayerData.Instance.propTextureIndex    // Locally stored prop texture index
                };

                // set the sync vars so late joiners get the change
                CmdSetCharacterTexture(playerIndex, textureIndices);
            }
            else if (!isOwned)
            {
                // set the materials and textures on other clients so they see our selected avatar
                ApplyMaterialsAndTextures(syncedCharacterIndex, syncedTextureIndices);
            }
        }

        private void OnTextureIndicesChanged(TextureIndexes oldIndices, TextureIndexes newIndices)
        {
            ApplyMaterialsAndTextures(syncedCharacterIndex, newIndices);
        }

        private void OnCharacterChanged(int oldCharacterIndex, int newCharacterIndex)
        {
            ApplyMaterialsAndTextures(newCharacterIndex, syncedTextureIndices);
        }

        private void ApplyMaterialsAndTextures(int characterIndex, TextureIndexes textureIndices)
        {
            if (characterIndex >= 0 && characterIndex < characterTextureSets.Count)
            {
                CharacterTextureSet textureSet = characterTextureSets[characterIndex];

                // Apply body material if valid texture is found
                if (textureIndices.bodyTextureIndex >= 0 && textureIndices.bodyTextureIndex < textureSet.bodyTextures.Count)
                {
                    ApplyMaterial(textureSet.bodyMaterial, textureSet.bodyTextures[textureIndices.bodyTextureIndex]);
                }

                // Apply prop material if valid texture is found
                if (textureIndices.propTextureIndex >= 0 && textureIndices.propTextureIndex < textureSet.propTextures.Count)
                {
                    ApplyMaterial(textureSet.propMaterial, textureSet.propTextures[textureIndices.propTextureIndex]);
                }
            }
        }

        // Method to apply a material with a new texture to renderers
        private void ApplyMaterial(Material baseMaterial, Texture2D selectedTexture)
        {
            // create a new material
            Material newMaterial = new Material(baseMaterial);
            // apply the texture to the new material
            newMaterial.mainTexture = selectedTexture;
            // get the renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // compare the avatar material to the new material and update if they match
                    if (materials[i].name == baseMaterial.name + " (Instance)")
                    {
                        materials[i] = newMaterial;
                    }
                }
                renderer.materials = materials;
            }
        }

        // Command to the server to set the indices so late joiners will get the indices from the SyncVar hooks
        [Command(requiresAuthority = false)]
        public void CmdSetCharacterTexture(int characterIndex, TextureIndexes textureIndices)
        {
            syncedCharacterIndex = characterIndex;
            syncedTextureIndices = textureIndices;
        }
    }
}

