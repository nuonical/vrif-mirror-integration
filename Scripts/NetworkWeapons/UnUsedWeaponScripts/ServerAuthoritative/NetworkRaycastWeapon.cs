using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

namespace BNG { 
public class NetworkRaycastWeapon : NetworkBehaviour
{
        [Header("General : ")]
        /// <summary>
        /// How far we can shoot in meters
        /// </summary>
        public float MaxRange = 25f;

        /// <summary>
        /// How much damage to apply to "Damageable" on contact
        /// </summary>
        public float Damage = 25f;

        /// <summary>
        /// Semi requires user to press trigger repeatedly, Auto to hold down
        /// </summary>
        [Tooltip("Semi requires user to press trigger repeatedly, Auto to hold down")]
        public NetworkFiringType FiringMethod = NetworkFiringType.Semi;

        /// <summary>
        /// How does the user reload once the Clip is Empty
        /// </summary>
        public NetworkReloadType ReloadMethod = NetworkReloadType.InfiniteAmmo;

        /// <summary>
        /// Ex : 0.2 = 5 Shots per second
        /// </summary>
        [Tooltip("Ex : 0.2 = 5 Shots per second")]
        public float FiringRate = 0.2f;
        float lastShotTime;

        [Tooltip("Amount of force to apply to a Rigidbody once damaged")]
        public float BulletImpactForce = 1000f;

        /// <summary>
        /// Maximum amount of internal ammo this weapon can hold. Does not account for attached clips.  For example, a shotgun has internal ammo
        /// </summary>
        [Tooltip("Current Internal Ammo if you are keeping track of ammo yourself. Firing will deduct from this number. Reloading will cause this to equal MaxInternalAmmo.")]
        public float InternalAmmo = 0;

        /// <summary>
        /// Maximum amount of internal ammo this weapon can hold. Does not account for attached clips.  For example, a shotgun has internal ammo
        /// </summary>
        [Tooltip("Maximum amount of internal ammo this weapon can hold. Does not account for attached clips.  For example, a shotgun has internal ammo")]
        public float MaxInternalAmmo = 10;

        /// <summary>
        /// Set true to automatically chamber a new round on fire. False to require charging. Example : Bolt-Action Rifle does not auto chamber.  
        /// </summary>
        [Tooltip("Set true to automatically chamber a new round on fire. False to require charging. Example : Bolt-Action Rifle does not auto chamber. ")]
        public bool AutoChamberRounds = true;

        /// <summary>
        /// Does it matter if rounds are chambered or not. Does the user have to charge weapon as soon as ammo is inserted
        /// </summary>
        [Tooltip("Does it matter if rounds are chambered or not. Does the user have to charge weapon as soon as ammo is inserted")]
        public bool MustChamberRounds = false;

        [Header("Projectile Settings : ")]

        [Tooltip("If true a projectile will always be used instead of a raycast")]
        public bool AlwaysFireProjectile = false;

        [Tooltip("If true the ProjectilePrefab will be instantiated during slowmo instead of using a raycast.")]
        public bool FireProjectileInSlowMo = true;

        [Tooltip("How fast to fire the weapon during slowmo. Keep in mind this is affected by Time.timeScale")]
        public float SlowMoRateOfFire = 0.3f;

        [Tooltip("Amount of force to apply to Projectile")]
        public float ShotForce = 10f;

        [Tooltip("Amount of force to apply to the BulletCasingPrefab object")]
        public float BulletCasingForce = 3f;

        [Header("Laser Guided Projectile : ")]

        [Tooltip("If true the projectile will be marked as Laser Guided and will follow a point from the ejection point")]
        public bool LaserGuided = false;

        [Tooltip("If specified the projectile will try to turn towards this object in world space. Otherwise will use a point from the muzzle of the raycast object")]
        public Transform LaserPoint;

        [Header("Recoil : ")]
        /// <summary>
        /// How much force to apply to the tip of the barrel
        /// </summary>
        [Tooltip("How much force to apply to the tip of the barrel")]
        public Vector3 RecoilForce = Vector3.zero;


        [Tooltip("Time in seconds to allow the gun to be springy")]
        public float RecoilDuration = 0.3f;

        Rigidbody weaponRigid;

        [Header("Raycast Options : ")]
        public LayerMask ValidLayers;

        [Header("Weapon Setup : ")]
        /// <summary>
        /// Transform of trigger to animate rotation of
        /// </summary>
        [Tooltip("Transform of trigger to animate rotation of")]
        public Transform TriggerTransform;

        /// <summary>
        /// Move this back on fire
        /// </summary>
        [Tooltip("Animate this back on fire")]
        public Transform SlideTransform;

        /// <summary>
        /// Where our raycast or projectile will spawn from
        /// </summary>
        [Tooltip("Where our raycast or projectile will start from.")]
        public Transform MuzzlePointTransform;

        /// <summary>
        /// Where to eject a bullet casing (optional)
        /// </summary>
        [Tooltip("Where to eject a bullet casing (optional)")]
        public Transform EjectPointTransform;

        /// <summary>
        /// Transform of Chambered Bullet. Hide this when no bullet is chambered
        /// </summary>
        [Tooltip("Transform of Chambered Bullet inside the weapon. Hide this when no bullet is chambered. (Optional)")]
        public Transform ChamberedBullet;

        /// <summary>
        /// Make this active on fire. Randomize scale / rotation
        /// </summary>
        [Tooltip("Make this active on fire. Randomize scale / rotation")]
        public GameObject MuzzleFlashObject;

        /// <summary>
        /// Eject this at EjectPointTransform (optional)
        /// </summary>
        [Tooltip("Eject this at EjectPointTransform (optional)")]
        public GameObject BulletCasingPrefab;

        /// <summary>
        /// If time is slowed this object will be instantiated instead of using a raycast
        /// </summary>
        [Tooltip("If time is slowed this object will be instantiated at muzzle point instead of using a raycast")]
        public GameObject ProjectilePrefab;

        /// <summary>
        /// Hit Effects spawned at point of impact
        /// </summary>
        [Tooltip("Hit Effects spawned at point of impact")]
        public GameObject HitFXPrefab;

        /// <summary>
        /// Play this sound on shoot
        /// </summary>
        [Tooltip("Play this sound on shoot")]
        public AudioClip GunShotSound;

        [Tooltip("Volume to play the GunShotSound clip at. Range 0-1")]
        [Range(0.0f, 1f)]
        public float GunShotVolume = 0.75f;

        /// <summary>
        /// Play this sound if no ammo and user presses trigger
        /// </summary>
        [Tooltip("Play this sound if no ammo and user presses trigger")]
        public AudioClip EmptySound;

        [Tooltip("Volume to play the EmptySound clip at. Range 0-1")]
        [Range(0.0f, 1f)]
        public float EmptySoundVolume = 1f;

        [Header("Slide Configuration : ")]
        /// <summary>
        /// How far back to move the slide on fire
        /// </summary>
        [Tooltip("How far back to move the slide on fire")]
        public float SlideDistance = -0.028f;

        /// <summary>
        /// Should the slide be forced back if we shoot the last bullet
        /// </summary>
        [Tooltip("Should the slide be forced back if we shoot the last bullet")]
        public bool ForceSlideBackOnLastShot = true;

        [Tooltip("How fast to move back the slide on fire. Default : 1")]
        public float slideSpeed = 1;

        /// <summary>
        /// How close to the origin is considered valid.
        /// </summary>
        float minSlideDistance = 0.001f;

        [Header("Inputs : ")]
        [Tooltip("Controller Input used to eject clip")]
        public List<GrabbedControllerBinding> EjectInput = new List<GrabbedControllerBinding>() { GrabbedControllerBinding.Button2Down };

        [Tooltip("Controller Input used to release the charging mechanism.")]
        public List<GrabbedControllerBinding> ReleaseSlideInput = new List<GrabbedControllerBinding>() { GrabbedControllerBinding.Button1Down };

        [Tooltip("Controller Input used to release reload the weapon if ReloadMethod = InternalAmmo.")]
        public List<GrabbedControllerBinding> ReloadInput = new List<GrabbedControllerBinding>() { GrabbedControllerBinding.Button2Down };

        [Header("Shown for Debug : ")]
        /// <summary>
        /// Is there currently a bullet chambered and ready to be fired
        /// </summary>
        [Tooltip("Is there currently a bullet chambered and ready to be fired")]
        public bool BulletInChamber = false;

        /// <summary>
        /// Is there currently a bullet chambered and that must be ejected
        /// </summary>
        [Tooltip("Is there currently a bullet chambered and that must be ejected")]
        public bool EmptyBulletInChamber = false;

        [Header("Events")]

        [Tooltip("Unity Event called when Shoot() method is successfully called")]
        public UnityEvent onShootEvent;

        [Tooltip("Unity Event called when something attaches ammo to the weapon")]
        public UnityEvent onAttachedAmmoEvent;

        [Tooltip("Unity Event called when something detaches ammo from the weapon")]
        public UnityEvent onDetachedAmmoEvent;

        [Tooltip("Unity Event called when the charging handle is successfully pulled back on the weapon")]
        public UnityEvent onWeaponChargedEvent;

        [Tooltip("Unity Event called when weapon damaged something")]
        public FloatEvent onDealtDamageEvent;

        [Tooltip("Passes along Raycast Hit info whenever a Raycast hit is successfully detected. Use this to display fx, add force, etc.")]
        public RaycastHitEvent onRaycastHitEvent;

        /// <summary>
        /// Is the slide / receiver forced back due to last shot
        /// </summary>
        protected bool slideForcedBack = false;

        protected WeaponSlide ws;

        protected bool readyToShoot = true;

        // added for conversion
        public Grabbable grab;
        public Grabber thisGrabber;

        void Start()
        {
            weaponRigid = GetComponent<Rigidbody>();
            grab = GetComponent<Grabbable>(); // added for Networking conversion

            if (MuzzleFlashObject)
            {
                MuzzleFlashObject.SetActive(false);
            }

            ws = GetComponentInChildren<WeaponSlide>();

            updateChamberedBullet();
        }

        // added for Networking
        public void OnGrab()
        {
            thisGrabber = grab.GetPrimaryGrabber();
        }

        private float TriggerValueCheck()
        {
            float triggerValue = InputBridge.Instance.RightTrigger; //  place new input structer here
            return triggerValue;
        }

        private void Update()
        {
            if (isOwned && grab.BeingHeld)
            {
                OnTrigger();
            }

        }

        // the input // had to change the input because we needed to derive from NetworkBehaviour not GrabbableEvents
        public void OnTrigger()
        {
            float triggerValue = TriggerValueCheck();
            if (triggerValue == 0)
            {
                return;
            }

            // Sanitize for angles 
            triggerValue = Mathf.Clamp01(triggerValue);

            // Update trigger graphics
            if (TriggerTransform)
            {
                TriggerTransform.localEulerAngles = new Vector3(triggerValue * 15, 0, 0);
            }

            // Trigger up, reset values
            if (triggerValue <= 0.5)
            {
                readyToShoot = true;
                playedEmptySound = false;
            }

            // Fire gun if possible
            if (readyToShoot && triggerValue >= 0.75f)
            {
                Shoot();

                // Immediately ready to keep firing if 
                readyToShoot = FiringMethod == NetworkFiringType.Automatic;
            }

            // These are here for convenience. Could be called through GrabbableUnityEvents instead
            checkSlideInput();
            checkEjectInput();
            CheckReloadInput();

            updateChamberedBullet();

            // base.OnTrigger(triggerValue);
        }

        void checkSlideInput()
        {

            if (!thisGrabber)
                return;
            // Check for bound controller button to release the charging mechanism
            for (int x = 0; x < ReleaseSlideInput.Count; x++)
            {

                if (InputBridge.Instance.GetGrabbedControllerBinding(ReleaseSlideInput[x], thisGrabber.HandSide))
                {
                    UnlockSlide();
                    break;
                }
            }
        }

        void checkEjectInput()
        {
            if (!thisGrabber)
                return;
            // Check for bound controller button to eject magazine
            for (int x = 0; x < EjectInput.Count; x++)
            {
                if (InputBridge.Instance.GetGrabbedControllerBinding(EjectInput[x], thisGrabber.HandSide))
                {
                    EjectMagazine();
                    break;
                }
            }
        }

        public virtual void CheckReloadInput()
        {
            if (!thisGrabber)
                return;
            if (ReloadMethod == NetworkReloadType.InternalAmmo)
            {
                // Check for Reload input(s)
                for (int x = 0; x < ReloadInput.Count; x++)
                {
                    if (InputBridge.Instance.GetGrabbedControllerBinding(EjectInput[x], thisGrabber.HandSide))
                    {
                        Reload();
                        break;
                    }
                }
            }
        }

        public virtual void UnlockSlide()
        {
            if (ws != null)
            {
                ws.UnlockBack();
            }
        }

        public virtual void EjectMagazine()
        {
            MagazineSlide ms = GetComponentInChildren<MagazineSlide>();
            if (ms != null)
            {
                ms.EjectMagazine();
            }
        }

        protected bool playedEmptySound = false;

        public virtual void Shoot()
        {

            // Has enough time passed between shots
            float shotInterval = Time.timeScale < 1 ? SlowMoRateOfFire : FiringRate;
            if (Time.time - lastShotTime < shotInterval)
            {
                return;
            }

            // Need to Chamber round into weapon
            if (!BulletInChamber && MustChamberRounds)
            {
                // Only play empty sound once per trigger down
                if (!playedEmptySound)
                {
                    VRUtils.Instance.PlaySpatialClipAt(EmptySound, transform.position, EmptySoundVolume, 0.5f); //  add to an rpc so others can hear it
                    CmdSyncDamageAndEffects(false, 0);
                    playedEmptySound = true;
                }

                return;
            }

            // Need to release slide
            if (ws != null && ws.LockedBack)
            {
                VRUtils.Instance.PlaySpatialClipAt(EmptySound, transform.position, EmptySoundVolume, 0.5f); // add to rpc so others can hear it
                CmdSyncDamageAndEffects(false, 0);
                return;
            }

            // Create our own spatial clip
            VRUtils.Instance.PlaySpatialClipAt(GunShotSound, transform.position, GunShotVolume); //  add to RPC to other clients so they can hear it

            // Haptics
            if (thisGrabber != null)
            {
                InputBridge.Instance.VibrateController(0.1f, 0.2f, 0.1f, thisGrabber.HandSide);
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
                if (isOwned) 
                {
                    CmdSyncDamageAndEffects(true, 1); // only do damage on the server and send int to switch for audio to play(Gunshot)
                }
                // Raycast to hit
                //  RaycastHit hit;
                //  if (Physics.Raycast(MuzzlePointTransform.position, MuzzlePointTransform.forward, out hit, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore))
                // {
                //  OnRaycastHit(hit);
                // }
            }

            // Apply recoil
            ApplyRecoil();

            // We just fired this bullet
            BulletInChamber = false;

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
        // added for server only hit damage and syncing audio and visual effects to other clients
        [Command]
        public void CmdSyncDamageAndEffects(bool doRaycast, int effectToPlay)
        {
            if (doRaycast)
            {
                RaycastHit hit;
                if (Physics.Raycast(MuzzlePointTransform.position, MuzzlePointTransform.forward, out hit, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore))
                {
                    OnRaycastHit(hit);
                }

                RpcSyncAudioAndEffects(effectToPlay);
            }

            else
            {
                RpcSyncAudioAndEffects(effectToPlay);
            }
        }
        // sync relevant information like audio to other clients excluding the owner because this is done locally for instant effects
        [ClientRpc(includeOwner = false)]
        void RpcSyncAudioAndEffects(int effectsToPlay)
        {
            switch (effectsToPlay)
            {
                case 0:
                    // Play the gunshot sound on other clients
                    VRUtils.Instance.PlaySpatialClipAt(EmptySound, transform.position, EmptySoundVolume, 0.5f);                    
                    break;

                case 1:
                    VRUtils.Instance.PlaySpatialClipAt(GunShotSound, transform.position, GunShotVolume);
                    StartCoroutine(doMuzzleFlash());
                    break;

                default:
                    Debug.LogWarning("Invalid soundToPlay value: " + effectsToPlay);
                    break;
            }

        }

        // Apply recoil by requesting sprinyness and apply a local force to the muzzle point
        public virtual void ApplyRecoil()
        {
            if (weaponRigid != null && RecoilForce != Vector3.zero)
            {

                // Make weapon springy for X seconds
                grab.RequestSpringTime(RecoilDuration);

                // Apply the Recoil Force
                weaponRigid.AddForceAtPosition(MuzzlePointTransform.TransformDirection(RecoilForce), MuzzlePointTransform.position, ForceMode.VelocityChange);
            }
        }

        // Hit something without Raycast. Apply damage, apply FX, etc.
        // this is called only on on the server from the the Shoot() function
        public virtual void OnRaycastHit(RaycastHit hit)
        {
            // get the hit gameobject
            GameObject rootObject = hit.collider.transform.root.gameObject;
            // get the hit network id to pass to the rpc
            NetworkIdentity netId = rootObject.GetComponent<NetworkIdentity>();
         // get the child path to the hit collider so we can pass the index to the rpc
            string childPath = GetChildPath(hit.collider.transform, rootObject.transform);
            
            //ApplyParticleFX(hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal), hit.collider); // move to rpc
            ApplyParticleFX(hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal), netId, childPath); 
                                                                                                             
            Rigidbody hitRigid = hit.collider.attachedRigidbody;
            if (hitRigid != null)
            {
                hitRigid.AddForceAtPosition(BulletImpactForce * MuzzlePointTransform.forward, hit.point);
            }

            // Damage if possible
            NetworkDamageable d = hit.collider.GetComponent<NetworkDamageable>();
            if (d)
            {
                // d.DealDamage(Damage, hit.point, hit.normal, true, gameObject, hit.collider.gameObject);
                d.TakeDamage(Damage);

                if (onDealtDamageEvent != null)
                {
                    onDealtDamageEvent.Invoke(Damage);
                }
            }

            // Call event
            if (onRaycastHitEvent != null)
            {
                onRaycastHitEvent.Invoke(hit);
            }
        }

        // function to get the child path to the hit object so it can be passed to an rpc as a string
        string GetChildPath(Transform child, Transform root)
        {
            string path = child.name;

            while (child.parent != null && child.parent != root)
            {
                child = child.parent;
                path = child.name + "/" + path;
            }

            return path;
        }

        //public virtual void ApplyParticleFX(Vector3 position, Quaternion rotation, Collider attachTo)
        public virtual void ApplyParticleFX(Vector3 position, Quaternion rotation, NetworkIdentity netId, string childPath)
        {
            if (HitFXPrefab)
            {
                // spawn the hit effects on all clients
                GameObject impact = Instantiate(HitFXPrefab, position, rotation) as GameObject;
                NetworkIdentity impactNetId = impact.GetComponent<NetworkIdentity>();
                NetworkServer.Spawn(impact);

                // pass relative info to the clients to attach the prefab
                RpcAttachBulletHole(position, rotation, netId, impactNetId, childPath);
            }
        }
        
        Collider attachTo;

        [ClientRpc]
        public void RpcAttachBulletHole(Vector3 position, Quaternion rotation, NetworkIdentity netId, NetworkIdentity impactNetId, string childPath)
        {
            // if hit object has a network identity
            if (netId)
            {
                Debug.Log("Position: " + position + ", Rotation: " + rotation + ", NetId: " + netId.name + ", Child Path: " + childPath);

                //  Collider attachTo = null;
                GameObject impact = netId.gameObject;
                Transform childTransform = impact.transform.root.Find(childPath);
                // if the hit collider is a child transform
                if (childTransform != null)
                {
                    Debug.Log(childTransform.name);
                    attachTo = childTransform.GetComponent<Collider>();
                }
                // if the hit has no child colliders attach to the root collider
                else
                {
                    attachTo = impact.GetComponent<Collider>();
                }

                BulletHole hole = impactNetId.gameObject.GetComponent<BulletHole>();

                if (hole)
                {
                    Debug.Log("HoleisNotNull");
                    hole.TryAttachTo(attachTo);
                }
            }

            // if the hit object does not have a net id
            else if (!netId)
            {
                // we can't pass a game object or a collider to a client from the server without a Network ID, so the bullet hole may
                // need rewritten into a network Bullet hole to handle scale or put it here to handle that
                return;
            }
        }

        /// <summary>
        /// Something attached ammo to us
        /// </summary>
        public virtual void OnAttachedAmmo()
        {

            // May have ammo loaded
            updateChamberedBullet();

            if (onAttachedAmmoEvent != null)
            {
                onAttachedAmmoEvent.Invoke();
            }
        }

        // Ammo was detached from the weapon
        public virtual void OnDetachedAmmo()
        {
            // May have ammo loaded / unloaded
            updateChamberedBullet();

            if (onDetachedAmmoEvent != null)
            {
                onDetachedAmmoEvent.Invoke();
            }
        }

        public virtual int GetBulletCount()
        {

            if (ReloadMethod == NetworkReloadType.InfiniteAmmo)
            {
                return 9999;
            }
            else if (ReloadMethod == NetworkReloadType.InternalAmmo)
            {
                return (int)InternalAmmo;
            }
            else if (ReloadMethod == NetworkReloadType.ManualClip)
            {
                return GetComponentsInChildren<Bullet>(false).Length;
            }

            // Default to bullet count
            return GetComponentsInChildren<Bullet>(false).Length;

        }

        public virtual void RemoveBullet()
        {

            // Don't remove bullet here
            if (ReloadMethod == NetworkReloadType.InfiniteAmmo)
            {
                return;
            }

            else if (ReloadMethod == NetworkReloadType.InternalAmmo)
            {
                InternalAmmo--;
            }
            else if (ReloadMethod == NetworkReloadType.ManualClip)
            {
                Bullet firstB = GetComponentInChildren<Bullet>(false);
                // Deactivate gameobject as this bullet has been consumed
                if (firstB != null)
                {
                    Destroy(firstB.gameObject);
                }
            }

            // Whenever we remove a bullet is a good time to check the chamber
            updateChamberedBullet();
        }


        public virtual void Reload()
        {
            InternalAmmo = MaxInternalAmmo;
        }

        void updateChamberedBullet()
        {
            if (ChamberedBullet != null)
            {
                ChamberedBullet.gameObject.SetActive(BulletInChamber || EmptyBulletInChamber);
            }
        }

        [Command]
        void chamberRound()
        {
            RpcChamberRound();

        }

        [ClientRpc]
        void RpcChamberRound()
        {
            int currentBulletCount = GetBulletCount();

            if (currentBulletCount > 0)
            {
                // Remove the first bullet we find in the clip                
                RemoveBullet();

                // That bullet is now in chamber
                BulletInChamber = true;
            }
            // Unable to chamber a bullet
            else
            {
                BulletInChamber = false;
            }
        }

        protected IEnumerator shotRoutine;

        // Randomly scale / rotate to make them seem different
        void randomizeMuzzleFlashScaleRotation()
        {
            MuzzleFlashObject.transform.localScale = Vector3.one * Random.Range(0.75f, 1.5f);
            MuzzleFlashObject.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 90f));
        }

        public virtual void OnWeaponCharged(bool allowCasingEject)
        {

            // Already bullet in chamber, eject it
            if (BulletInChamber && allowCasingEject)
            {
                ejectCasing();
            }
            else if (EmptyBulletInChamber && allowCasingEject)
            {
                ejectCasing();
                EmptyBulletInChamber = false;
            }

            chamberRound();

            // Slide is no longer forced back if weapon was just charged
            slideForcedBack = false;

            if (onWeaponChargedEvent != null)
            {
                onWeaponChargedEvent.Invoke();
            }
        }

        protected virtual void ejectCasing()
        {
            GameObject shell = Instantiate(BulletCasingPrefab, EjectPointTransform.position, EjectPointTransform.rotation) as GameObject;
            Rigidbody rb = shell.GetComponentInChildren<Rigidbody>();

            if (rb)
            {
                rb.AddRelativeForce(Vector3.right * BulletCasingForce, ForceMode.VelocityChange);
            }

            // Clean up shells
            GameObject.Destroy(shell, 5);
        }

        protected virtual IEnumerator doMuzzleFlash()
        {
            MuzzleFlashObject.SetActive(true);
            yield return new WaitForSeconds(0.05f);

            randomizeMuzzleFlashScaleRotation();
            yield return new WaitForSeconds(0.05f);

            MuzzleFlashObject.SetActive(false);
        }

        // Animate the slide back, eject casing, pull slide back
        protected virtual IEnumerator animateSlideAndEject()
        {

            // Start Muzzle Flash
            MuzzleFlashObject.SetActive(true);

            int frames = 0;
            bool slideEndReached = false;
            Vector3 slideDestination = new Vector3(0, 0, SlideDistance);

            if (SlideTransform)
            {
                while (!slideEndReached)
                {


                    SlideTransform.localPosition = Vector3.MoveTowards(SlideTransform.localPosition, slideDestination, Time.deltaTime * slideSpeed);
                    float distance = Vector3.Distance(SlideTransform.localPosition, slideDestination);

                    if (distance <= minSlideDistance)
                    {
                        slideEndReached = true;
                    }

                    frames++;

                    // Go ahead and update muzzleflash in sync with slide
                    if (frames < 2)
                    {
                        randomizeMuzzleFlashScaleRotation();
                    }
                    else
                    {
                        slideEndReached = true;
                        MuzzleFlashObject.SetActive(false);
                    }

                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                yield return new WaitForEndOfFrame();
                randomizeMuzzleFlashScaleRotation();
                yield return new WaitForEndOfFrame();

                MuzzleFlashObject.SetActive(false);
                slideEndReached = true;
            }

            // Set Slide Position
            if (SlideTransform)
            {
                SlideTransform.localPosition = slideDestination;
            }

            yield return new WaitForEndOfFrame();
            MuzzleFlashObject.SetActive(false);

            // Eject Shell
            ejectCasing();

            // Pause for shell to eject before returning slide
            yield return new WaitForEndOfFrame();


            if (!slideForcedBack && SlideTransform != null)
            {
                // Slide back to original position
                frames = 0;
                bool slideBeginningReached = false;
                while (!slideBeginningReached)
                {

                    SlideTransform.localPosition = Vector3.MoveTowards(SlideTransform.localPosition, Vector3.zero, Time.deltaTime * slideSpeed);
                    float distance = Vector3.Distance(SlideTransform.localPosition, Vector3.zero);

                    if (distance <= minSlideDistance)
                    {
                        slideBeginningReached = true;
                    }

                    if (frames > 2)
                    {
                        slideBeginningReached = true;
                    }

                    yield return new WaitForEndOfFrame();
                }
            }
        }
    }

    public enum NetworkFiringType
    {
        Semi,
        Automatic
    }

    public enum NetworkReloadType
    {
        InfiniteAmmo,
        ManualClip,
        InternalAmmo
    }
}

