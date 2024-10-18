using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    // script is on the player prefab to send player data to the BNG GameManager
    public class BNGLocalPlayerManager : NetworkBehaviour
    {
        private LocalPlayerData localPlayerData;


        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
           
            localPlayerData = LocalPlayerData.Instance;
            if (localPlayerData != null)
            {
                string pName = localPlayerData.PlayerName;
                // Debug.Log($"Local player name is: {pName}");

                // Call command on server to add this player
                SendPlayerDataToServer(pName);
            }
            else
            {
                Debug.LogError("LocalPlayerData not initialized.");
            }
        }

        private void SendPlayerDataToServer(string playerName)
        {      
            // Find the game manager and send the player data
            if (BNGGameManager.Instance != null)
            {
                BNGGameManager.Instance.CmdAddPlayer(playerName);
            }
        }
    }
}
