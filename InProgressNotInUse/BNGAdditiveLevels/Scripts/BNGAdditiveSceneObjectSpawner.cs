using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace BNG
{
    public class BNGAdditiveSceneObjectSpawner : NetworkBehaviour
    {
        [Header("All Grabbables that can be Network Spawned")]
        public List<GameObject> objectsToSpawn;
        public string sceneName;

        private void Start()
        {         
            if (isServer)
            {
                Scene currentScene = SceneManager.GetSceneByName(sceneName);

                foreach (GameObject go in objectsToSpawn)
                {
                    GameObject newObject = Instantiate(go);
                    SceneManager.MoveGameObjectToScene(newObject, currentScene);
                    NetworkServer.Spawn(newObject);
                }
            }
        }

    }
}
