using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace BNG
{
    public class BNGSceneClientGrabbableSpawner : NetworkBehaviour
    {
        public List<GameObject> objectsToSpawn;

        public int rightObjectIndex;
        public int leftObjectIndex;

        public XRLocalRig localRig;

        public Grabber rightGrabber;
        public Grabber leftGrabber;

        public string activeSceneName;
        void Start()
        {
            if (isOwned)
            {
                // we need the active subscene so the server can spawn the object then move it to the active scene
                Scene activeScene = SceneManager.GetActiveScene();
                Debug.Log("Active Scene: " + activeScene.name);

                // Iterate through all loaded scenes to find subscenes (additive loaded)
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    // Check if the scene is loaded additively and not the active scene
                    if (scene.isLoaded && scene != activeScene)
                    {
                        activeSceneName = scene.name;                       
                    }
                }
                // get the local xr rig instance
                localRig = XRLocalRig.Instance;

                if (localRig)
                {
                    leftGrabber = localRig.GrabberLeft;
                    rightGrabber = localRig.GrabberRight;

                    rightObjectIndex = localRig.rightGrabbableInt;
                    if (rightObjectIndex != -1)
                    {
                        Vector3 objectPos = rightGrabber.transform.position;
                        StartCoroutine(SpawnObjectAfterDelay(rightObjectIndex, objectPos, activeSceneName));
                    }
                }
            }
        }

        IEnumerator SpawnObjectAfterDelay(int objectIndex, Vector3 position, string sceneName)
        {
            // wait a frame to make sure
            yield return null;

            // Call the command to spawn on the server
            CmdSpawnClientOwnedObject(objectIndex, position, sceneName);
        }

        [Command(requiresAuthority = false)]
        void CmdSpawnClientOwnedObject(int objectIndex, Vector3 position, string sceneName, NetworkConnectionToClient sender = null)
        {

            // Instantiate the object and move it to the correct scene
            GameObject go = Instantiate(objectsToSpawn[objectIndex], position,Quaternion.identity);

            // Move the object to the specified scene
            SceneManager.MoveGameObjectToScene(go, SceneManager.GetSceneByName(sceneName));

            // Spawn the object on the server and assign it to the client
            NetworkServer.Spawn(go, sender);
            // rpc back to the sender to assign the object to the grabber
            TargetAssignGrabbableToGrabber(sender, objectIndex, go.GetComponent<NetworkIdentity>());
        }

        [TargetRpc]
        public void TargetAssignGrabbableToGrabber(NetworkConnection target, int objectIndex, NetworkIdentity netObjectId)
        {
            GameObject grabbableGo = netObjectId.gameObject;
            Grabbable grab = grabbableGo.GetComponent<Grabbable>();
            rightGrabber.GrabGrabbable(grab);
            rightGrabber.onGrabEvent.Invoke(grab);
            //localRig.rightGrabbableInt = objectIndex;
        }

    }
}
