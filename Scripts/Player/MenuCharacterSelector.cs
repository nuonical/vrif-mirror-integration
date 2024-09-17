using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace BNG
{
    public class MenuCharacterSelector : MonoBehaviour
    {
        public UnityEngine.UI.Button replaceCharacterButton; // Assign this in the Unity Inspector
        public int characterIndex; // Assign new character prefab here

        void Start()
        {
            // Hook the button click event to trigger character replacement
            replaceCharacterButton.onClick.AddListener(OnReplaceCharacterClicked);
        }

        void OnReplaceCharacterClicked()
        {
            // Find the local player object
            var player = NetworkClient.localPlayer;

            if (player != null && player.isOwned)
            {
                // Call the function on the player to request character replacement
                player.GetComponent<NetworkCharacterReplacement>().CmdReplaceCharacter(characterIndex);
            }
            else
            {
                Debug.LogError("Local player not found.");
            }
        }
    }
}
