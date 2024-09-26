using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// this script will move the XRRig, the EventSystem and the Line Renderer that spawns from the rig to the active subscene as scenes are changed
namespace BNG
{
    public class BNGAdditiveSceneXRRigSceneHandler : MonoBehaviour
    {
        public GameObject XRRig;
        public GameObject EventSystem;
        public GameObject LineRenderer;

        public ScreenFader fader;

        // subscribe to the scene loaded event
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // when the scene is changed, we do this in a coroutine so we have time to get the line renderer
            StartCoroutine(GetAndMoveObjects(scene));
        }

        IEnumerator GetAndMoveObjects(Scene scene)
        {
            yield return null;
            LineRenderer = GameObject.Find("LineRenderer");
            yield return null;
            MoveToSubScene(scene);
          //  yield return new WaitForSeconds(fader.FadeOutSpeed);
           // fader.DoFadeOut(); // fader is causing issues with scene change.. tried just disabling it, but had to remove it.. will look into it..
        }

        // move objects to the active scene
        private void MoveToSubScene(Scene subScene)
        {
            SceneManager.MoveGameObjectToScene(XRRig, subScene);
            SceneManager.MoveGameObjectToScene(EventSystem, subScene);
            SceneManager.MoveGameObjectToScene(LineRenderer, subScene);
            // call the screen fader to fade in .. without this it stays blacked out on change?
           //fader.DoFadeOut();

        }

    }
}
