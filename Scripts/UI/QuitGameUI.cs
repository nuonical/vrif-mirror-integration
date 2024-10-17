using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class QuitGameUI : NetworkBehaviour
    {

        public UnityEngine.UI.Button quitButton;

        void Start()
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        
        void QuitGame()
        {
            if (isClient)
            {
                NetworkManager.singleton.StopClient();
            }

            if (isServer)
            {
                NetworkManager.singleton.StopHost();
            }
        }
    }
}
