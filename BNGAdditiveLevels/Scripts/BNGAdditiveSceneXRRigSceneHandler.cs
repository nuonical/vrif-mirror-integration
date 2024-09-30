using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
// this script will teleport the player controller to a target in the scene on scene change
namespace BNG
{
    public class BNGAdditiveSceneXRRigSceneHandler : MonoBehaviour
    {
        public Transform playerController;
        CharacterController characterController;
        PlayerRotation playerRotation;
        public string targetTransformNameInNewScene; // The name of the Transform in the new scene


        private void Start()
        {
            characterController = playerController.GetComponent<CharacterController>();
            playerRotation = playerController.GetComponent<PlayerRotation>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // This is called when a new scene is loaded
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Find the target Transform by searching through all root objects in the scene
            Transform targetTransform = FindTargetTransform(scene);

            if (targetTransform != null)
            {
                // playerController.position = targetTransform.position; // Teleport the player controller to the target position
                StartCoroutine(TeleportPlayer(targetTransform));
            }
            else
            {
                Debug.LogError($"Target Transform '{targetTransformNameInNewScene}' not found in the scene '{scene.name}'.");
            }
        }

        IEnumerator TeleportPlayer(Transform target)
        {
            // diable player movement
            characterController.enabled = false;
            playerRotation.enabled = false;
            yield return null;
            // teleport the player
            playerController.SetPositionAndRotation(target.position, target.rotation);
            yield return null;
            // enable player movement
            characterController.enabled = true;
            playerRotation.enabled = true;

        }

        // Find the target Transform by iterating through root objects in the scene
        Transform FindTargetTransform(Scene scene)
        {
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                Transform foundTransform = rootObj.transform.Find(targetTransformNameInNewScene);
                if (foundTransform != null)
                {
                    return foundTransform;
                }
            }
            return null;
        }
    }
}
