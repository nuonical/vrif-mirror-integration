using UnityEngine;
using UnityEngine.UI;

namespace BNG
{
    public class NetworkAmmoDisplay : MonoBehaviour
    {
        public NetworkRaycastWeapon Weapon;
        public Text AmmoLabel;

        void OnGUI()
        {
            string loadedShot = Weapon.BulletInChamber ? "1" : "0";
            AmmoLabel.text = loadedShot + " / " + Weapon.GetBulletCount();
        }
    }
}
