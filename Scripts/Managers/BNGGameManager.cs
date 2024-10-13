using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    // use a struct for cleaner code handling, doing this, you can have one dictionary key with several stats
    public struct PlayerGameStats
    {
        public string name;
        public int killCount;
        public int deathCount;
    }

    // an example of controlling and tracking all players name, scores and progress in the game
    // this class can control everything in the game if needed as a single source of control, like timers etc 
    public class BNGGameManager : NetworkBehaviour
    { 
        public static BNGGameManager Instance;

        // dictionary for hold player stats on the server
        public readonly SyncDictionary<string, PlayerGameStats> playerStats = new();

        private void Awake()
        {
            Instance = this;
        }

        // These commands are called from the client player to update the player stats when they change

        // add the player to the manager on start of the game
        [Command(requiresAuthority = false)]
        public void CmdAddPlayer(string playerName, NetworkConnectionToClient sender = null)
        {
            string dictionaryKey = sender.ToString();           
            playerStats.Add(dictionaryKey, new PlayerGameStats { name = playerName, killCount = 0, deathCount = 0 });
        }

        // update the death count of the player everytime they die
        [Command(requiresAuthority = false)]
        public void UpdateDeathCount(int playerDeathCount, NetworkConnectionToClient sender = null)
        {
            string dictionaryKey = sender.ToString();
            if (playerStats.ContainsKey(dictionaryKey))
            {
                PlayerGameStats stats = playerStats[dictionaryKey];
                stats.deathCount = playerDeathCount;
                playerStats[dictionaryKey] = stats;
            }
        }

        // update the kill count every time a kill is made
        [Command(requiresAuthority = false)]
        public void UpdateKillCount(int playerKillCount, NetworkConnectionToClient sender = null)
        {
            string dictionaryKey = sender.ToString();

            if (playerStats.ContainsKey(dictionaryKey))
            {
                PlayerGameStats stats = playerStats[dictionaryKey];
                stats.deathCount = playerKillCount;
                playerStats[dictionaryKey] = stats;
            }
        }
    }
}
