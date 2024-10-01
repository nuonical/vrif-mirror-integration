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
           if(label.TryGetComponent(out BNGAdditiveLookAtMainCamera lookAtMainCamera))
            {
                lookAtMainCamera.enabled = true;
            }
              
        }

        // Note that I have created layers called Player(6) and Portal(7) and set them
        // up in the Physics collision matrix so only Player collides with Portal.
        void OnTriggerEnter(Collider other)
        {
            // tag check in case you didn't set up the layers and matrix as noted above
            //  if (other.CompareTag("NetworkPlayer") || other.CompareTag("NetworkGrabbable"))
            // {
            if (other.tag == "NetworkGrabbable" && !isServer)
            {
                Debug.Log("grab collision");
                NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
                SendGrabbableToNewScene(netId);
            }

            if (!isServer)
                return;

            if (other.CompareTag("NetworkPlayer"))
            {
                StartCoroutine(SendPlayerToNewScene(other.gameObject));
            }
            // this does not work / causes grabbable and player to be visiable in both scenes and eventually on scene change, the scene no longer loads, scene is deactivated
           

        }

        [ServerCallback]
        IEnumerator SendPlayerToNewScene(GameObject player)
        {
            if (player.TryGetComponent(out NetworkIdentity identity))
            {
                yield return null;
                NetworkConnectionToClient conn = identity.connectionToClient;
                if (conn == null) yield break;

                // Tell client to unload previous subscene with custom handling (see NetworkManager::OnClientChangeScene).
                conn.Send(new SceneMessage { sceneName = gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });

                // wait for fader to complete
               // yield return new WaitForSeconds(BNGAdditiveLevelNetworkManager.singleton.fadeInOut.GetDuration());

                // Remove player after fader has completed
                NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Unspawn);

                // reposition player on server and client
                player.transform.position = startPosition;

                // Rotate player to face center of scene
                // Player is 2m tall with pivot at 0,1,0 so we need to look at
                // 1m height to not tilt the player down to look at origin
                player.transform.LookAt(Vector3.up);

                // Move player to new subscene.
                SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(destinationScene));

                // Tell client to load the new subscene with custom handling (see NetworkManager::OnClientChangeScene).
                conn.Send(new SceneMessage { sceneName = destinationScene, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });

                // Player will be spawned after destination scene is loaded
                NetworkServer.AddPlayerForConnection(conn, player);

                // host client playerController would have been disabled by OnTriggerEnter above
                // Remote client players are respawned with playerController already enabled
              //  if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.TryGetComponent(out PlayerController playerController))
                  // playerController.enabled = true;
            }
        }

        [Command(requiresAuthority = false)]
        void SendGrabbableToNewScene(NetworkIdentity grabbable)
        {
            GameObject GoGrab = grabbable.gameObject;
           // if (grabbable.TryGetComponent(out NetworkIdentity identity))
           // {
                // Rigidbody rb = identity.GetComponent<Rigidbody>();
                // rb.velocity = Vector3.zero;
                // rb.angularVelocity = Vector3.zero;
                // identity.RemoveClientAuthority();            
                // SceneManager.MoveGameObjectToScene(GoGrab, SceneManager.GetSceneByName(destinationScene));
                NetworkServer.Destroy(GoGrab);
            //}
        }
    }
}

