using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
namespace BNG
{
    public class BNGPortalAdditiveScene : NetworkBehaviour
    {
        [Scene, Tooltip("Which scene to send player from here")]
        public string destinationScene;

        [Tooltip("Where to spawn player in Destination Scene")]
        public Vector3 startPosition;

        [Tooltip("Reference to child TextMesh label")]
        public TextMesh label; // don't depend on TMPro. 2019 errors.

        [SyncVar(hook = nameof(OnLabelTextChanged))]
        public string labelText;

        public void OnLabelTextChanged(string _, string newValue)
        {
            label.text = labelText;
        }

        public override void OnStartServer()
        {
            labelText = Path.GetFileNameWithoutExtension(destinationScene).Replace("MirrorAdditiveLevels", "");

            // Simple Regex to insert spaces before capitals, numbers
            labelText = Regex.Replace(labelText, @"\B[A-Z0-9]+", " $0");
        }

        public override void OnStartClient()
        {
            if (label.TryGetComponent(out BNGAdditiveLookAtMainCamera lookAtMainCamera))
            {
                lookAtMainCamera.enabled = true;
            }

        }

        // Note that I have created layers called Player(6) and Portal(7) and set them
        // up in the Physics collision matrix so only Player collides with Portal.
        void OnTriggerEnter(Collider other)
        {
            if (!isServer)
                return;

            if (other.CompareTag("NetworkGrabbable"))
            {
                Debug.Log(other.name);
                NetworkGrabbable netGrab = other.GetComponent<NetworkGrabbable>();
                // check if the grabbable is being held, so it doesn't get destoryed just throughing grabbables into the trigger
                if (netGrab && netGrab.holdingStatus)
                {
                    NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
                    netId.RemoveClientAuthority();

                    NetworkServer.Destroy(other.gameObject);
                }

            }

            // tag check in case you didn't set up the layers and matrix as noted above
            if (other.CompareTag("NetworkPlayer"))
            {

                if (other.CompareTag("NetworkPlayer"))
                {
                    StartCoroutine(SendPlayerToNewScene(other.gameObject));
                }
            }

        }

        [ServerCallback]
        IEnumerator SendPlayerToNewScene(GameObject player)
        {
            if (player.TryGetComponent(out NetworkIdentity identity))
            {
                yield return null;
                NetworkConnectionToClient conn = identity.connectionToClient;
                if (conn == null) yield break;

                // Loop through all NetworkIdentity objects in the scene
                foreach (var networkIdentity in NetworkServer.spawned.Values)
                {
                    // Ensure we do not remove authority from the player object itself
                    if (networkIdentity.connectionToClient == conn && networkIdentity != conn.identity)
                    {
                        // Remove authority
                        networkIdentity.RemoveClientAuthority();
                        Debug.Log($"Removed authority from {networkIdentity.gameObject.name}.");
                    }
                }
                yield return null;
                // Tell client to unload previous subscene with custom handling (see NetworkManager::OnClientChangeScene).
                conn.Send(new SceneMessage { sceneName = gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });

                // wait for fader to complete
                // yield return new WaitForSeconds(BNGAdditiveLevelNetworkManager.singleton.fadeInOut.GetDuration());

                // Remove player after fader has completed
                NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Unspawn);

                // Move player to new subscene.
                SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(destinationScene));

                // Tell client to load the new subscene with custom handling (see NetworkManager::OnClientChangeScene).
                conn.Send(new SceneMessage { sceneName = destinationScene, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });

                // Player will be spawned after destination scene is loaded
                NetworkServer.AddPlayerForConnection(conn, player);


            }
        }
    }

}


