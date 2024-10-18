using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG
{
    // this script spawns a local player avatar on start and spawns a new local avatar on change 
    public class LocalAvatarSettings : MonoBehaviour
    {
        // get the local player data instance for saving the selection
        LocalPlayerData localPlayerData;
        XRLocalRig xrLocalRig;

        [System.Serializable]
        public class AvatarPrefabSet
        {
            [Header("Avatar Prefab")]
            public GameObject avatarPrefab;
            // image to be used for avatar on menu
            [Header("Image to be used on the Menu for Avatar Selected")]
            public Texture avatarImage;
            // all possible textures
            [Header("All textures that can be used for the avatar body")]
            public List<Texture2D> bodyTextures;
            // the body material on the prefab for comparison
            [Header("The body material that is on the Avatar Prefab")]
            public Material bodyMaterial;
            // all possible textures
            [Header("All textures that can be used for Avatar Props")]
            public List<Texture2D> propTextures;
            // the prop material on the prefab for comparsison
            [Header("The material on the Avatar prefab props")]
            public Material propMaterial;
            [Header("Name of the Avatars Hands to find the Hand Controllers")]
            public string leftHandName;
            public string rightHandName;
        }

        // the target transforms to assign to the spawned characterIk and CharacterIk Follow components
        [Header("The target transforms to apply to the spawned avatar")]
        public Transform iKLeftHandTarget;
        public Transform iKRightHandTarget;
        public Transform iKHeadTarget;
        public Transform centerEyeAnchor;
        public Transform playerController;

        // the parent we wish to spawn the avatar as a child of
        [Header("The GameObject to parent the avatar to")]
        public Transform avatarParent;
        // list of the avatar prefabs that can be spawned
        public List<AvatarPrefabSet> avatarPrefabSets;
        // cache for all the renders 
        Renderer[] renderers;

        [Header("Assign Avatar root if not spawning an avatar on start")]
        public GameObject avatar;

        // localPlayer data settings
        public int localCharacterIndex;
        public int localBodyTextureIndex;
        public int localPropTextureIndex;

        // textures on Menu to indicate selection
        [Header("Images on the Menu that change with selection")]
        public RawImage menuAvatarImage;
        public RawImage menuUniformImage;
        public RawImage menuPropImage;

        // set true if you want to replace and spawn a new character
        [Header("Select false if not spawning an avatar on start to the local rig")]
        public bool spawnLocalAvatar = false;

        [Header("Set true if you want to get the assign avatar Character Ik transforms on start from " +
            "Ik Target transforms on this component")]
        public bool assignTransformsOnStart = false;

        void Start()
        {
            // get the local player data instance
            localPlayerData = LocalPlayerData.Instance;
            xrLocalRig = XRLocalRig.Instance;

            

            // spawn the starting character, this will spawn the last used character as the index is saved on change
            if (avatarPrefabSets.Count > 0)
            {
               StartCoroutine(LoadLocalPlayerDataAvatar());
            }
            else
            {
                Debug.LogError("No avatar prefab found");
            }
        }

        IEnumerator LoadLocalPlayerDataAvatar()
        {
            yield return null;
            //Debug.Log("Textures started to apply");
            // get the prefab and texture index from the local player data
            if (localPlayerData)
            {
                localCharacterIndex = localPlayerData.playerPrefabIndex;
                localBodyTextureIndex = localPlayerData.bodyTextureIndex;
                localPropTextureIndex = localPlayerData.propTextureIndex;
            }
            if (spawnLocalAvatar)
            {             // spawn the avatar prefab
                avatar = Instantiate(avatarPrefabSets[localPlayerData.playerPrefabIndex].avatarPrefab, Vector3.zero, Quaternion.identity, avatarParent);
            }
            if (avatar && assignTransformsOnStart)
            {
                // get the characterIk and assign targets 
                CharacterIK characterIk = avatar.GetComponent<CharacterIK>();
                if (characterIk)
                {
                    characterIk.FollowLeftController = iKLeftHandTarget;
                    characterIk.FollowRightController = iKRightHandTarget;
                    characterIk.FollowHead = iKHeadTarget;
                }
                //get the character ik follow and assign targets
                CharacterIKFollow characterIkFollow = avatar.GetComponent<CharacterIKFollow>();
                if (characterIkFollow)
                {
                    characterIkFollow.FollowTransform = centerEyeAnchor;
                    characterIkFollow.PlayerTransform = playerController;
                }
                // start with all renderers on the avatar disabled so we don't see it flicker in then enable after a short period
                StartCoroutine(WaitToSeeAvatar());
                // get the hand contollers on the avatar and assign the grabbers to the hand controllers, assign the hand pose blenders to the XRLocalRig
                string leftHandName = avatarPrefabSets[localPlayerData.playerPrefabIndex].leftHandName;

                Transform avatarLeftHand = FindInChildren(avatar.transform, leftHandName);
                if (avatarLeftHand != null)
                {
                    HandController leftHandController = avatarLeftHand.GetComponent<HandController>();
                    HandPoseBlender leftHandPoseBlender = avatarLeftHand.GetComponent<HandPoseBlender>();
                    if (leftHandController)
                    {
                        leftHandController.grabber = xrLocalRig.GrabberLeft;
                    }

                    if (leftHandPoseBlender)
                    {
                        xrLocalRig.LeftHandPoseBlender = leftHandPoseBlender;
                    }
                }

                string rightHandName = avatarPrefabSets[localPlayerData.playerPrefabIndex].rightHandName;
                Transform avatarRightHand = FindInChildren(avatar.transform, rightHandName);
                if (avatarRightHand != null)
                {
                    Debug.Log(avatarRightHand.name);
                    HandController rightHandController = avatarRightHand.GetComponent<HandController>();
                    HandPoseBlender rightHandPoseBlender = avatarRightHand.GetComponent<HandPoseBlender>();
                    if (rightHandController)
                    {
                        rightHandController.grabber = xrLocalRig.GrabberRight;
                    }

                    if (rightHandPoseBlender)
                    {
                        xrLocalRig.RightHandPoseBlender = rightHandPoseBlender;
                    }
                }


                // change image on the menu for the avatar
                if (menuAvatarImage)
                {
                    menuAvatarImage.texture = avatarPrefabSets[localPlayerData.playerPrefabIndex].avatarImage;
                }    
            }

            // Apply materials and textures for the avatar after it has been instantiated
            ApplyLocalMaterialsAndTextures(localCharacterIndex, localBodyTextureIndex, localPropTextureIndex);

        }

        // function to find the left and right hands in the children of the AVatar
        private Transform FindInChildren(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                Transform found = FindInChildren(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
        private void ApplyLocalMaterialsAndTextures(int characterIndex, int bodyTextureIndex, int propTextureIndex)
        {
            if (characterIndex >= 0 && characterIndex < avatarPrefabSets.Count)
            {
                AvatarPrefabSet prefabSet = avatarPrefabSets[characterIndex];

                // Apply body material if valid texture is found
                if (bodyTextureIndex >= 0 && bodyTextureIndex < prefabSet.bodyTextures.Count)
                {
                    ApplyLocalMaterial(prefabSet.bodyMaterial, prefabSet.bodyTextures[bodyTextureIndex], avatar);
                    // set the menu image 
                    if (menuUniformImage)
                    {
                        menuUniformImage.texture = prefabSet.bodyTextures[bodyTextureIndex];
                    }
                }

                // Apply prop material if valid texture is found
                if (propTextureIndex >= 0 && propTextureIndex < prefabSet.propTextures.Count)
                {
                    ApplyLocalMaterial(prefabSet.propMaterial, prefabSet.propTextures[propTextureIndex], avatar);
                    // set the menu image 
                    if (menuPropImage)
                    {
                        menuPropImage.texture = prefabSet.propTextures[propTextureIndex];
                    }
                }
            }
        }

        // Method to apply a material with a new texture to renderers
        private void ApplyLocalMaterial(Material baseMaterial, Texture2D selectedTexture, GameObject avatar)
        {
            // create a new material
            Material newMaterial = new Material(baseMaterial);
            // assign the texture to the new material
            newMaterial.mainTexture = selectedTexture;

            // Get the renderers from the instantiated avatar
            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // compare the material found to the material on the avatar, if they match replace the material with the new material
                    if (materials[i].name == baseMaterial.name + " (Instance)")
                    {
                        materials[i] = newMaterial;
                    }
                }
                
                renderer.materials = materials;
            }
            
        }

        // destroy the current avatar and replace it with a new one
        public void SpawnNewAvatar(int newAvatarIndex)
        {
            if (avatar)
            {
                // desroty the current avatar
                Destroy(avatar);
                // spawn the new avatar
                avatar = Instantiate(avatarPrefabSets[newAvatarIndex].avatarPrefab, playerController.position, Quaternion.identity, avatarParent);
                // start with all renderers on the avatar disabled so we don't see it flicker in then enable after a short period
                StartCoroutine(WaitToSeeAvatar());
                // get the hand contollers on the avatar and assign the grabbers to the hand controllers, assign the hand pose blenders to the XRLocalRig
                string leftHandName = avatarPrefabSets[newAvatarIndex].leftHandName;

                Transform avatarLeftHand = FindInChildren(avatar.transform, leftHandName);
                if (avatarLeftHand != null)
                {
                    HandController leftHandController = avatarLeftHand.GetComponent<HandController>();
                    HandPoseBlender leftHandPoseBlender = avatarLeftHand.GetComponent<HandPoseBlender>();
                    if (leftHandController)
                    {
                        leftHandController.grabber = xrLocalRig.GrabberLeft;
                    }

                    if (leftHandPoseBlender)
                    {
                        xrLocalRig.LeftHandPoseBlender = leftHandPoseBlender;
                    }
                }

                string rightHandName = avatarPrefabSets[newAvatarIndex].rightHandName;
                Transform avatarRightHand = FindInChildren(avatar.transform, rightHandName);
                if (avatarRightHand != null)
                {
                    Debug.Log(avatarRightHand.name);
                    HandController rightHandController = avatarRightHand.GetComponent<HandController>();
                    HandPoseBlender rightHandPoseBlender = avatarRightHand.GetComponent<HandPoseBlender>();
                    if (rightHandController)
                    {
                        rightHandController.grabber = xrLocalRig.GrabberRight;
                    }

                    if (rightHandPoseBlender)
                    {
                        xrLocalRig.RightHandPoseBlender = rightHandPoseBlender;
                    }
                }

                // get the characterIK component and assign the targets
                CharacterIK characterIk = avatar.GetComponent<CharacterIK>();
                if (characterIk)
                {
                    characterIk.FollowLeftController = iKLeftHandTarget;
                    characterIk.FollowRightController = iKRightHandTarget;
                    characterIk.FollowHead = iKHeadTarget;
                }
                // get the characterIKfollow component and assign the targets
                CharacterIKFollow characterIkFollow = avatar.GetComponent<CharacterIKFollow>();
                if (characterIkFollow)
                {
                    characterIkFollow.FollowTransform = centerEyeAnchor;
                    characterIkFollow.PlayerTransform = playerController;
                }

                // Apply the new textures and materials after spawning the new avatar/ defaulting to 0 an all textures
                ApplyLocalMaterialsAndTextures(newAvatarIndex, newAvatarIndex, newAvatarIndex);
                if (menuAvatarImage)
                {
                    menuAvatarImage.texture = avatarPrefabSets[newAvatarIndex].avatarImage;
                }
            }
        }

        IEnumerator WaitToSeeAvatar()
        {
            yield return new WaitForSeconds(0.2f);
            List<Renderer> renderers = new();
            renderers.AddRange(avatar.GetComponentsInChildren<Renderer>());
            foreach(Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
            yield break;
        }

        // Method to change individual textures on the loaded avatar
        public void ChangeAvatarBodyTexture(int bodyTextureIndex)
        {
            if (avatarPrefabSets.Count > localCharacterIndex)
            {
                AvatarPrefabSet prefabSet = avatarPrefabSets[localCharacterIndex];

                // Change body texture if valid texture index is provided
                if (bodyTextureIndex >= 0 && bodyTextureIndex < prefabSet.bodyTextures.Count)
                {
                    ApplyLocalMaterial(prefabSet.bodyMaterial, prefabSet.bodyTextures[bodyTextureIndex], avatar);
                    localPlayerData.SetBodyTextureIndex(bodyTextureIndex);
                    // set the menu image 
                    if (menuUniformImage)
                    {
                        menuUniformImage.texture = prefabSet.bodyTextures[bodyTextureIndex];
                    }
                }
            }
        }

        public void ChangeAvatarPropTexture(int propTextureIndex)
        {
            if (avatarPrefabSets.Count > localCharacterIndex)
            {
                AvatarPrefabSet prefabSet = avatarPrefabSets[localCharacterIndex];
                // Change prop texture if valid texture index is provided
                if (propTextureIndex >= 0 && propTextureIndex < prefabSet.propTextures.Count)
                {
                    ApplyLocalMaterial(prefabSet.propMaterial, prefabSet.propTextures[propTextureIndex], avatar);
                    localPlayerData.SetPropTextureIndex(propTextureIndex);
                    // set the menu image 
                    if (menuPropImage)
                    {
                        menuPropImage.texture = prefabSet.propTextures[propTextureIndex];
                    }
                }
            }
        }

    }
}
