using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    public class RaycastWeaponNetworkOveride : RaycastWeapon
    {
        public RaycastWeaponNetworkHandler networkHandler;

        private void Start()
        {
            networkHandler = GetComponent<RaycastWeaponNetworkHandler>();
        }

        public override void OnTrigger(float triggerValue)
        {
            // Sanitize for angles 
            // triggerValue = Mathf.Clamp01(triggerValue);
            
            // get the trigger value from the rayweaponnetworkhandler so it can only return a value if it is owned
            float ownedTriggerValue = networkHandler.OwnerTriggerValue(triggerValue);

            // Update trigger graphics
            if (TriggerTransform)
            {
                TriggerTransform.localEulerAngles = new Vector3(ownedTriggerValue * 15, 0, 0);
            }

            // Trigger up, reset values
            if (ownedTriggerValue <= 0.5)
            {
                readyToShoot = true;
                playedEmptySound = false;
            }

            // Fire gun if possible
            if (readyToShoot && ownedTriggerValue >= 0.75f)
            {
                Shoot();

                // Immediately ready to keep firing if 
                readyToShoot = FiringMethod == FiringType.Automatic;
            }

            // These are here for convenience. Could be called through GrabbableUnityEvents instead
            checkSlideInput(); //  had to make this public in the RaycastWeapon Component 
            checkEjectInput();//  had to make this public in the RaycastWeapon Component
            CheckReloadInput();

            updateChamberedBullet();//  had to make this public in the RaycastWeapon Component

            base.OnTrigger(triggerValue);
        }

        public override void Shoot()
        {
            // Has enough time passed between shots
            float shotInterval = Time.timeScale < 1 ? SlowMoRateOfFire : FiringRate;
            if (Time.time - lastShotTime < shotInterval) //  last shot time made public on raycast weapon so it can be accessed for override
            {
                return;
            }

            // Need to Chamber round into weapon
            if (!BulletInChamber && MustChamberRounds)
            {
                // Only play empty sound once per trigger down
                if (!playedEmptySound)
                {
                    VRUtils.Instance.PlaySpatialClipAt(EmptySound, transform.position, EmptySoundVolume, 0.5f);
                    playedEmptySound = true;
                }
                return;
            }

            // Need to release slide
            if (ws != null && ws.LockedBack)
            {
                VRUtils.Instance.PlaySpatialClipAt(EmptySound, transform.position, EmptySoundVolume, 0.5f);
                return;
            }

            // Create our own spatial clip
            VRUtils.Instance.PlaySpatialClipAt(GunShotSound, transform.position, GunShotVolume);

            // Haptics
            if (thisGrabber != null)
            {
                input.VibrateController(0.1f, 0.2f, 0.1f, thisGrabber.HandSide);
            }

            // Use projectile if Time has been slowed
            bool useProjectile = AlwaysFireProjectile || (FireProjectileInSlowMo && Time.timeScale < 1);
            if (useProjectile)
            {
                GameObject projectile = Instantiate(ProjectilePrefab, MuzzlePointTransform.position, MuzzlePointTransform.rotation) as GameObject;
                Rigidbody projectileRigid = projectile.GetComponentInChildren<Rigidbody>();
                projectileRigid.AddForce(MuzzlePointTransform.forward * ShotForce, ForceMode.VelocityChange);

                Projectile proj = projectile.GetComponent<Projectile>();
                // Convert back to raycast if Time reverts
                if (proj && !AlwaysFireProjectile)
                {
                    proj.MarkAsRaycastBullet();
                }

                if (proj && LaserGuided)
                {
                    if (LaserPoint == null)
                    {
                        LaserPoint = MuzzlePointTransform;
                    }
                    proj.MarkAsLaserGuided(MuzzlePointTransform);
                }

                // Make sure we clean up this projectile
                Destroy(projectile, 20);
            }
            else
            {
                // do raycast on the server via the RaycastWeaponNetworkhandler
                networkHandler.SendRayCastCommand();
            }

            // Apply recoil
            ApplyRecoil();

            // We just fired this bullet
            //BulletInChamber = false;
            networkHandler.CmdSyncBulletInChamber(false);
            // Try to load a new bullet into chamber         
            if (AutoChamberRounds)
            {
                chamberRound();
            }
            else
            {
                EmptyBulletInChamber = true;
            }

            // Unable to chamber bullet, force slide back
            if (!BulletInChamber)
            {
                // Do we need to force back the receiver?
                slideForcedBack = ForceSlideBackOnLastShot;

                if (slideForcedBack && ws != null)
                {
                    ws.LockBack();
                }
            }

            // Call Shoot Event
            if (onShootEvent != null)
            {
                onShootEvent.Invoke();
            }

            // Store our last shot time to be used for rate of fire
            lastShotTime = Time.time;

            // Stop previous routine
            if (shotRoutine != null)
            {
                MuzzleFlashObject.SetActive(false);
                StopCoroutine(shotRoutine);
            }

            if (AutoChamberRounds)
            {
                shotRoutine = animateSlideAndEject();
                StartCoroutine(shotRoutine);
            }
            else
            {
                shotRoutine = doMuzzleFlash();
                StartCoroutine(shotRoutine);
            }
        }
    

        public override void OnRaycastHit(RaycastHit hit)
        {
            return;
        }

        public override void chamberRound()
        {

            int currentBulletCount = GetBulletCount();

            if (currentBulletCount > 0)
            {
                // Remove the first bullet we find in the clip                
                RemoveBullet();

                // That bullet is now in chamber
                // BulletInChamber = true;
                networkHandler.CmdSyncBulletInChamber(true);
            }
            // Unable to chamber a bullet
            else
            {
                // BulletInChamber = false;
                networkHandler.CmdSyncBulletInChamber(false);
            }
        }
    }
}
