using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG {
    public class NetworkMagazine : NetworkBehaviour {

        [SyncVar(hook = nameof(BulletCountChanged))]
        public int CurrentBulletCount = 0;

        public int MaxBulletCount = 0;

        public Transform BulletGraphicsParent;

        public void AddBullet() {
            if (MagazineIsFull()) {
                return;
            }

            CurrentBulletCount++;
        }

        public void SetBulletCount(int bulletCount) {
            if (isServer) {
                CurrentBulletCount = bulletCount;
            } 
            else if (isOwned) {
                // Send command to the server to update count
                CmdSetBulletCount(bulletCount);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetBulletCount(int bulletCount) {
            if (bulletCount > MaxBulletCount) {
                bulletCount = MaxBulletCount;
            }

            CurrentBulletCount = bulletCount;
        }

        // CurrentBulletCount updated
        public void BulletCountChanged(int prevValue, int newValue) {
            UpdateAllBulletGraphics();
        }

        public bool MagazineIsFull() {
            return CurrentBulletCount >= MaxBulletCount;
        }

        public virtual void UpdateAllBulletGraphics() {
            for (int x = 0; x < MaxBulletCount; x++) {
                if (BulletGraphicsParent != null && BulletGraphicsParent.childCount > x) {
                    BulletGraphicsParent.GetChild(x).gameObject.SetActive(x < CurrentBulletCount);
                }
            }
        }
    }
}

