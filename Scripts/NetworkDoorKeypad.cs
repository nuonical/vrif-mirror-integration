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
        [Header("The code that unlocks the door")]

        [Tooltip("Set true to generate a random code on start")]
        [SerializeField] bool GenerateRandomCode = true;

        [SerializeField] string correctPasscode = "1234";

        [Tooltip("Pass Code will be displayed here on start, does not need a Network Identity")]
        public Text PassCodeText;

        private int codeLength;

        private string inputCode = "";

        [Header("The Network Door Helper Component on the Door")]
        public NetworkDoorHelper networkDoorHelper;

        [Header("The Text UI to display the entered text")]
        public Text codeText;

        [Header("The Text to show on start and when incorrect code is entered")]
        [SerializeField] string startText = "Locked";

        [Header("Audio Clips to play on click and Unlocking")]
        public AudioClip buttonPressClip;
        public AudioClip unlockClip;

        // synk the locked status
        [SyncVar(hook = nameof(SyncUnlockDoor))]
        private bool doorLocked = true;

        [SyncVar(hook = nameof(SyncRandomPassCode))]
        private string randomPassCode;
        private void Start()
        {    
            // make sure the starting state is locked
            UpdateDoorLockState(doorLocked);

            if (doorLocked)
            {
                codeText.text = startText;
            }
            else
            {
                codeText.text = "Unlocked";
            }

            if(!GenerateRandomCode && PassCodeText)
            {
                PassCodeText.text = "Code:" + correctPasscode;
            }
            // set the code length referencing the correctPassCode length
            codeLength = correctPasscode.Length;

            // generate random 4 digit code
            if (isServer && GenerateRandomCode)
            {
                randomPassCode = GenerateRandomDigits();              
            }
        }

        // generate random pass code 
        string GenerateRandomDigits()
        {
            string result = "";
            for (int i = 0; i < 4; i++)
            {
                int randomDigit = Random.Range(0, 10); // Generates a number between 0 and 9
                result += randomDigit.ToString();
            }
            return result;
        }

        // sync the random pass code and show it on a UI
        void SyncRandomPassCode(string oldPass, string NewPass)
        {
            Debug.Log(NewPass);
            correctPasscode = NewPass;
            if (PassCodeText)
            {
                PassCodeText.text = "Code:" + correctPasscode;
            }
        }


        // function called from buttons to enter characters for the code
        public void EnterNumber(string number)
        {
            PlayButtonClip();
            // if the door is unlocked do not recieve key input
            if (!doorLocked)
                return;
            // if the current enterd code length is less than the needed characters, this runs to add the character and update the text field
            if (inputCode.Length < codeLength)
            {
                inputCode += number;
                codeText.text = inputCode;
            }

            // if the current entered code length is equal to the number of characters, unlock the door if its the correct passcode, if not, reset to locked
            if (inputCode.Length == codeLength)
            {
                if (inputCode == correctPasscode)
                {
                    CmdUnlockDoor();
                    PlayUnlockedClip();
                }
                else
                {                   
                    inputCode = ""; // Reset code 
                    codeText.text = startText; // show start text on incorrect entry
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

        // optional clear function
        public void ResetCode()
        {
            inputCode = "";
            codeText.text = startText;
        }

        // called from syncvar hook to sync the locked status 
        void SyncUnlockDoor(bool oldLock, bool newLock)
        {
            UpdateDoorLockState(newLock);           
        }

        // set the locked status
        private void UpdateDoorLockState(bool isLocked)
        {
            networkDoorHelper.DoorIsLocked = isLocked;
            // codeText.text = isLocked ? startText : "Unlocked";
            codeText.text = "Unlocked";
        }

        void PlayButtonClip()
        {
            if (buttonPressClip)
            {
                VRUtils.Instance.PlaySpatialClipAt(buttonPressClip, transform.position, 1f, 1f);
            }
        }

        void PlayUnlockedClip()
        {
            if(unlockClip)
            {
                VRUtils.Instance.PlaySpatialClipAt(unlockClip, transform.position, 1f, 1f);
            }
        }
    }
}

