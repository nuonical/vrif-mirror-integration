using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

// a keypad script to unlock a door or trigger some other function when the correct code is entered
namespace BNG
{
    public class NetworkDoorKeypad : NetworkBehaviour
    {
        [SerializeField] private string correctPasscode = "1234";
        private string inputCode = "";
        public NetworkDoorHelper networkDoorHelper;

        public Text codeText;

        [SyncVar(hook = nameof(SyncUnlockDoor))]
        private bool doorLocked = true;

        private void Start()
        {            
            UpdateDoorLockState(doorLocked); 
        }

        public void EnterNumber(string number)
        {
            if (inputCode.Length < 4)
            {
                inputCode += number;
                codeText.text = inputCode;
            }

            if (inputCode.Length == 4)
            {
                if (inputCode == correctPasscode)
                {
                    CmdUnlockDoor();
                }
                else
                {
                    Debug.Log("Incorrect Passcode");
                    inputCode = ""; // Reset code on incorrect input
                    codeText.text = inputCode;
                }
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdUnlockDoor()
        {
            if (doorLocked) // Prevent redundant unlocking
            {
                doorLocked = false;
            }
        }

        public void ResetCode()
        {
            inputCode = "";
            codeText.text = inputCode;
        }

        void SyncUnlockDoor(bool oldLock, bool newLock)
        {
            UpdateDoorLockState(newLock);
        }

        private void UpdateDoorLockState(bool isLocked)
        {
            networkDoorHelper.DoorIsLocked = isLocked;
        }
    }
}

