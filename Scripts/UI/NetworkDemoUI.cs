using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {
    public class NetworkDemoUI : NetworkBehaviour {

        public InputField PlayerNameInput;

        public GameObject ConnectButton;
        public GameObject HostButton;

        public GameObject DisconnectButton;

        public TMP_Text DisplayText;

        public List<GameObject> DisableGameObjects;

        // Shown for Debug
        public bool ClientConnected;

        public override void OnStartServer() {
            Debug.Log("OnStartServer");
            // New text line
            DisplayText.text = "Server started.\n";

            // Hide the connect buttons
            ShowDisconnectButton();
        }

        public override void OnStartClient() {
            Debug.Log("OnStartClient");

            ClientConnected = true;
            // New text line
            DisplayText.text += "Client started.\n";

            // Hide the connect buttons
            ShowDisconnectButton();
        }

        public void OnConnectButton() {

            DisplayText.text = "Connecting..\n";

            NetworkManager.singleton.StartClient();
            SaveLocalPlayersData();
            // Hide the connect buttons
            ShowDisconnectButton();
        }

        public void OnHostButton() {
            NetworkManager.singleton.StartHost();

            SaveLocalPlayersData();
            // Hide the connect buttons
            ShowDisconnectButton();
        }

        public void OnDisconnectButton() {
            if(ClientConnected) {
                NetworkManager.singleton.StopClient();
            }
            else {
                NetworkManager.singleton.StopHost();
            }

            if(isServer)
            {
                NetworkManager.singleton.StopHost();
            }
            
            HideDisconnectButton();
        }

        public override void OnStopClient() {

            Debug.Log("OnStopClient");
            DisplayText.text += "Client stopped.\n";

            ClientConnected = false;

            HideDisconnectButton();
        }

        public void ShowDisconnectButton() {
            if (DisconnectButton) {
                DisconnectButton.SetActive(true);
            }

            if (ConnectButton) {
                ConnectButton.SetActive(false);
            }
            if (HostButton) {
                HostButton.SetActive(false);
            }

            foreach(GameObject go in DisableGameObjects)
            {
                go.SetActive(false);
            }
        }

        public void HideDisconnectButton() {
            if (DisconnectButton) {
                DisconnectButton.SetActive(false);
            }
            if (ConnectButton) {
                ConnectButton.SetActive(true);
            }
            if (HostButton) {
                HostButton.SetActive(true);
            }
        }

       public void SaveLocalPlayersData()
        {
            // save player data
            SaveLoadData saveLoadData = SaveLoadData.Instance;
            LocalPlayerData localPlayerData = LocalPlayerData.Instance;
            // assign the localplayerdata the player name
            localPlayerData.SetPlayerName(PlayerNameInput.text);
            // save player player data from local player data
            saveLoadData.SavePlayerPref("PlayerName", localPlayerData.PlayerName);
            saveLoadData.SavePlayerPref("PrefabIndex", localPlayerData.playerPrefabIndex);
            saveLoadData.SavePlayerPref("BodyTextureIndex", localPlayerData.bodyTextureIndex);
            saveLoadData.SavePlayerPref("PropTextureIndex", localPlayerData.propTextureIndex);
        }
    }
}

