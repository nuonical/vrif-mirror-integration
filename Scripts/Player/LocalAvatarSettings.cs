using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    // this script spawns a local player avatar on start and spawns a new local avatar on change 
    public class LocalAvatarSettings : MonoBehaviour
    {
        // get the local player data instance for saving the selection
        LocalPlayerData localPlayerData;

        [System.Serializable]
        public class AvatarPrefabSet
        {
            public GameObject avatarPrefab;
            // all possible textures
            public List<Texture2D> bodyTextures;
            // the body material on the prefab for comparison
            public Material bodyMaterial;
            // all possible textures
            public List<Texture2D> propTextures;
            // the prop material on the prefab for comparsison
            public Material propMaterial;
        }

        // the target transforms to assign to the spawned characterIk and CharacterIk Follow components
        public Transform iKLeftHandTarget;
        public Transform iKRightHandTarget;
        public Transform iKHeadTarget;
        public Transform centerEyeAnchor;
        public Transform playerController;

        // the parent we wish to spawn the avatar as a child of
        public Transform avatarParent;
        // list of the avatar prefabs that can be spawned
        public List<AvatarPrefabSet> avatarPrefabSets;
        // cache for all the renders 
        Renderer[] renderers;

        // cache the instantiated Avatar
        GameObject avatar;

        // localPlayer data settings
        int localCharacterIndex;
        int localTextureIndex;

        // Start is called before the first frame update
        void Start()
        {
            localPlayerData = LocalPlayerData.Instance;

            // get the prefab and texture index from the local player data
            if(localPlayerData)
            {
                localCharacterIndex = localPlayerData.playerPrefabIndex;
                localTextureIndex = localPlayerData.playerTextureIndex;
            }

            // spawn the starting character, this will spawn the last used character as the index is saved on change
            if (avatarPrefabSets.Count > 0)
            {
                LoadLocalPlayerDataAvatar();
            }

            else
            {
                Debug.LogError("No avatar prefab found");
            }
        }

        void LoadLocalPlayerDataAvatar()
        {
            avatar = Instantiate(avatarPrefabSets[localPlayerData.playerPrefabIndex].avatarPrefab, Vector3.zero, Quaternion.identity, avatarParent);
            CharacterIK characterIk = avatar.GetComponent<CharacterIK>();
            if (characterIk)
            {
                characterIk.FollowLeftController = iKLeftHandTarget;
                characterIk.FollowRightController = iKRightHandTarget;
                characterIk.FollowHead = iKHeadTarget;
            }

            CharacterIKFollow characterIkFollow = avatar.GetComponent<CharacterIKFollow>();
            if (characterIkFollow)
            {
                characterIkFollow.FollowTransform = centerEyeAnchor;
                characterIkFollow.PlayerTransform = playerController;
            }

            SetAvatarMaterials(localCharacterIndex, localTextureIndex);
        }

        void SetAvatarMaterials(int characterIndex, int textureIndex)
        {
            Debug.Log("Local Happened");
            AvatarPrefabSet prefabSet = avatarPrefabSets[characterIndex];

            // set the body materails
            if (textureIndex >= 0 && textureIndex < prefabSet.bodyTextures.Count)
            {
                Texture2D selectedTexture = prefabSet.bodyTextures[textureIndex];

                // Create a new material instance and apply the texture
                Material newBodyMaterial = new Material(prefabSet.bodyMaterial);
                newBodyMaterial.mainTexture = selectedTexture;

                // Apply the new material to the character renderers
                renderers = avatar.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        // compare the found material to the material in the prefab, if they match, change it
                        if (materials[i].name == prefabSet.bodyMaterial.name + " (Instance)")
                        {
                            materials[i] = newBodyMaterial;
                        }
                    }
                    renderer.materials = materials;
                }
            }
        }
       
        // this to be updated to allow for changing the props on the avatar
        public void SpawnNewAvatar(int newAvatarIndex)
        {
            if(avatar)
            {
                Destroy(avatar);
                avatar = Instantiate(avatarPrefabSets[newAvatarIndex].avatarPrefab,playerController.position, Quaternion.identity, avatarParent);
                CharacterIK characterIk = avatar.GetComponent<CharacterIK>();
                if (characterIk)
                {
                    characterIk.FollowLeftController = iKLeftHandTarget;
                    characterIk.FollowRightController = iKRightHandTarget;
                    characterIk.FollowHead = iKHeadTarget;
                }

                CharacterIKFollow characterIkFollow = avatar.GetComponent<CharacterIKFollow>();
                if (characterIkFollow)
                {
                    characterIkFollow.FollowTransform = centerEyeAnchor;
                    characterIkFollow.PlayerTransform = playerController;
                }

                SetAvatarMaterials(newAvatarIndex, newAvatarIndex);
            }
        }

        // to finish so you cant see your avatar falling in to place
        IEnumerator WaitToSeeAvatar()
        {
            yield return null;
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
}
