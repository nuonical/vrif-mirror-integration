using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

namespace BNG
{
    public class BNGAdditiveSceneLoader : NetworkBehaviour
    {
        public UnityEngine.UI.Button loadSceneButton; // Assign this in the Unity Inspector
        public string sceneToLoad = "SceneName"; // Set the scene name in the inspector or code

        // SyncVar to keep track of the current additively loaded scene
        [SyncVar(hook = nameof(OnSceneChanged))]
        private string currentLoadedScene;

        void Start()
        {
            if (loadSceneButton != null)
                loadSceneButton.onClick.AddListener(OnLoadSceneClicked);
        }

        void OnLoadSceneClicked()
        {
            // Only the host (server) can load scenes
            if (isServer)
            {
                if (currentLoadedScene != sceneToLoad)
                {
                    CmdLoadScene(sceneToLoad);
                }
            }
            else
            {
                Debug.Log("You must be the host to load the scene.");
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdLoadScene(string sceneName)
        {
            if (isServer)
            {
                if (currentLoadedScene != sceneName)
                {
                    // Update the SyncVar to notify clients of the scene change
                    currentLoadedScene = sceneName;

                    // Load the scene additively on the server
                    StartCoroutine(LoadSceneAdditively(sceneName));
                }
            }
        }

        private IEnumerator LoadSceneAdditively(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Get all root objects in the scene
            Scene additiveScene = SceneManager.GetSceneByName(sceneName);
            if (additiveScene.IsValid())
            {
                GameObject[] rootObjects = additiveScene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    NetworkIdentity networkIdentity = obj.GetComponent<NetworkIdentity>();
                    if (networkIdentity != null && networkIdentity.gameObject.activeInHierarchy)
                    {
                        NetworkServer.Spawn(obj);  // Spawns scene objects for clients
                    }
                }
            }
        }

        // This is called on both the server and clients when the SyncVar is updated
        private void OnSceneChanged(string oldScene, string newScene)
        {
            // Load the scene on the client side
            StartCoroutine(LoadSceneAdditivelyOnClient(newScene));
        }

        private IEnumerator LoadSceneAdditivelyOnClient(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName) && !isServer)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }

                Debug.Log("Client loaded scene additively: " + sceneName);
            }
        }
    }
}
