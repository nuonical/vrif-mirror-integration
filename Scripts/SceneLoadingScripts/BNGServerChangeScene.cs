using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

namespace BNG
{
    public class BNGServerChangeScene : MonoBehaviour
    {
        public UnityEngine.UI.Button changeSceneButton; // Assign this in the Inspector
        public string sceneToLoad; // Specify the name of the scene to load

        void Start()
        {
            // Ensure the button is set up to call ChangeScene on click
            if (changeSceneButton != null)
            {
                changeSceneButton.onClick.AddListener(OnChangeSceneButtonClicked);
            }
        }

        private void OnChangeSceneButtonClicked()
        {
            // Only the server should change the scene
            if (NetworkServer.active)
            {
                ChangeScene();
            }
            else
            {
                Debug.LogWarning("Only the server can change the scene!");
            }
        }

        [Server]
        private void ChangeScene()
        {
            // Change the scene for all clients
            //  ServerChangeScene(sceneToLoad);
            NetworkManager.singleton.ServerChangeScene(sceneToLoad);
        }

    }
}
