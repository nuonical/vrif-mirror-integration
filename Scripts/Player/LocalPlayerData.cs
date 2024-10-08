using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        public int playerTextureIndex = 0;
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
            }
            if(PlayerPrefs.HasKey("PrefabIndex"))
            {
                playerPrefabIndex = PlayerPrefs.GetInt("PrefabIndex");
            }

            if (PlayerPrefs.HasKey("PreTextIndex"))
            {
                playerTextureIndex = PlayerPrefs.GetInt("PreTextIndex");
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

        public void SetPlayerTextureIndex(int preTextIndex)
        {
            playerTextureIndex = preTextIndex;
            PlayerPrefs.SetInt("PreTextIndex", preTextIndex);
        }
    }
}
