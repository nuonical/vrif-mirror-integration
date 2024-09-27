using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
// this script will move the XRRig, the EventSystem and the Line Renderer that spawns from the rig to the active subscene as scenes are changed
namespace BNG
{
    public class BNGAdditiveSceneXRRigSceneHandler : NetworkBehaviour
    {
        public GameObject XRRig;
        public GameObject EventSystem;
        public GameObject LineRenderer;

        public ScreenFader fader;

        private bool isProcessingSceneChange = false; // State variable

        // Subscribe to the scene loaded event
        private void OnEnable()
        {           
            BNGAdditiveLevelNetworkManager.singleton.OnClientSceneChangedEvent += HandleClientSceneChanged;
        }

        private void OnDisable()
        {           
            BNGAdditiveLevelNetworkManager.singleton.OnClientSceneChangedEvent -= HandleClientSceneChanged;
        }

       

        private void HandleClientSceneChanged(string scene)
        {
            // Remove the ".unity" extension from the scene name if it exists
            if (scene.EndsWith(".unity"))
            {
                scene = scene.Substring(0, scene.Length - ".unity".Length);
            }

            // Get the Scene object using the cleaned scene name
            Scene sceneToLoad = SceneManager.GetSceneByName(scene);

            // Only process if not currently handling a scene change
            if (!isProcessingSceneChange)
            {
                isProcessingSceneChange = true; // Set state to processing
                StartCoroutine(GetAndMoveObjects(sceneToLoad));
            }
        }

        private IEnumerator GetAndMoveObjects(Scene scene)
        {
            yield return null;

            if (LineRenderer == null)
            {
                LineRenderer = GameObject.Find("LineRenderer");
            }

            yield return null;
            MoveToSubScene(scene);

            // Reset state variable after processing
            isProcessingSceneChange = false;
        }

        // Move objects to the active scene
        private void MoveToSubScene(Scene subScene)
        {
           
            SceneManager.MoveGameObjectToScene(XRRig, subScene);
            SceneManager.MoveGameObjectToScene(EventSystem, subScene);
            SceneManager.MoveGameObjectToScene(this.gameObject, subScene);
            // If LineRenderer is not parented to XRRig, move it as well
            if (LineRenderer != null && LineRenderer.transform.parent == null)
            {
                SceneManager.MoveGameObjectToScene(LineRenderer, subScene);
            }

            // Uncomment to call the screen fader if necessary
            // fader.DoFadeOut();
        }
    }
}
