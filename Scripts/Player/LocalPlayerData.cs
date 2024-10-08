using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// this class holds all the player settings and is do not destroy from scene to scene
// script saves data to playerprefs(change save system to your preference) and is recalled on restart
namespace BNG {
    public class LocalPlayerData : MonoBehaviour {

        public static LocalPlayerData Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<LocalPlayerData>();
                    if (_instance == null) {
                        _instance = new GameObject("LocalPlayerData").AddComponent<LocalPlayerData>();
                    }
                }
                return _instance;
            }
        }
        private static LocalPlayerData _instance;

        // Any local player data we may want to store for later
        public string PlayerName;
        public int playerPrefabIndex = 0;
        public int bodyTextureIndex = 0;
        public int propTextureIndex = 0;

        // menu input field to recall the player name
        public UnityEngine.UI.InputField playerNameInput;

        void Awake() {
            // Setup singletone so only one object exists at a time
            if (_instance != null && _instance != this) {
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);

            LoadPlayerSettings();
        }

        void LoadPlayerSettings() {
            if (PlayerPrefs.HasKey("PlayerName")) {
                PlayerName = PlayerPrefs.GetString("PlayerName");
                if (playerNameInput)
                {
                    playerNameInput.text = PlayerName;
                }
            }
            if(PlayerPrefs.HasKey("PrefabIndex"))
            {
                playerPrefabIndex = PlayerPrefs.GetInt("PrefabIndex");
            }

            if (PlayerPrefs.HasKey("PreTextIndex"))
            {
                bodyTextureIndex = PlayerPrefs.GetInt("BodyTextureIndex");
            }

            if (PlayerPrefs.HasKey("PropTextureIndex"))
            {
                propTextureIndex = PlayerPrefs.GetInt("PropTextureIndex");
            }

        }

        public void SetPlayerName(string playerName, bool savePrefs) {
            PlayerName = playerName;

            if(savePrefs) {
                PlayerPrefs.SetString("PlayerName", PlayerName);
            }
        }

        // set player prefab selection index from menu 
        public void SetPlayerPrefabIndex(int prefabIndex)
        {
            playerPrefabIndex = prefabIndex;
            PlayerPrefs.SetInt("PrefabIndex", prefabIndex);
        }

        public void SetBodyTextureIndex(int preBodyTextIndex)
        {
            bodyTextureIndex = preBodyTextIndex;
            PlayerPrefs.SetInt("BodyTextureIndex", preBodyTextIndex);
        }

        public void SetPropTextureIndex(int prePropTextIndex)
        {
            propTextureIndex = prePropTextIndex;
            PlayerPrefs.SetInt("PropTextureIndex", prePropTextIndex);
        }
    }
}
