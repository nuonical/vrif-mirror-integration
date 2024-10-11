using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    // scritp to save and load data from playerprefs
    public class SaveLoadData : MonoBehaviour
    {
        public static SaveLoadData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveLoadData>();
                    if (_instance == null)
                    {
                        _instance = new GameObject("SaveLoadData").AddComponent<SaveLoadData>();
                    }
                }
                return _instance;
            }
        }
        private static SaveLoadData _instance;

        LocalPlayerData localPlayerData;

        [Header("Player Name Input field on the Menu")]
        public UnityEngine.UI.InputField playerNameInputField;
   
        void Start()
        {
            localPlayerData = LocalPlayerData.Instance;
            LoadFromPrefs();
        }

        void LoadFromPrefs()
        {   
            // load saved player name and load it menu input and local player data
            string playerName = (string)LoadPlayerPref("PlayerName", "Unknown");
            localPlayerData.PlayerName = playerName;
            playerNameInputField.text = playerName;
            int playerPrefab = (int)LoadPlayerPref("PrefabIndex", 0);
            localPlayerData.playerPrefabIndex = playerPrefab;
            int playerBodyTexture = (int)LoadPlayerPref("BodyTextureIndex", 0);
            localPlayerData.bodyTextureIndex = playerBodyTexture;
            int playerPropTexture = (int)LoadPlayerPref("PropTextureIndex", 0);
            localPlayerData.propTextureIndex = playerPropTexture;
        }

        // function called from LoadFromPrefs 
        public object LoadPlayerPref(string key, object defaultValue)
        {
            if (defaultValue is int)
            {
                return PlayerPrefs.GetInt(key, (int)defaultValue);
            }
            else if (defaultValue is float)
            {
                return PlayerPrefs.GetFloat(key, (float)defaultValue);
            }
            else if (defaultValue is string)
            {
                return PlayerPrefs.GetString(key, (string)defaultValue);
            }
            else
            {
                Debug.LogError("Unsupported type for PlayerPrefs");
                return null;
            }
        }

        // call this function to save a pref
        public void SavePlayerPref(string key, object value)
        {
            if (value is int)
            {
                PlayerPrefs.SetInt(key, (int)value);
            }
            else if (value is float)
            {
                PlayerPrefs.SetFloat(key, (float)value);
            }
            else if (value is string)
            {
                PlayerPrefs.SetString(key, (string)value);
            }
            else
            {
                Debug.LogError("Unsupported type for PlayerPrefs");
                return;
            }

            PlayerPrefs.Save(); 
        }
    }
}
