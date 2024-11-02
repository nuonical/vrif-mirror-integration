using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkWeaponAttachement : NetworkBehaviour
    {
        private SnapZone snapZone;
        private NetworkIdentity rootNetId;

        [Header("The root game object that has the grabbable component")]
        public Grabbable rootGrabbable;

        // hook to sync the can remove status to clients, also late joiners
        [SyncVar(hook = nameof(SetCanRemoveSnapped))]
        public bool canRemove;
        // hook to sync the network identity of the snapped object to all clients
        [SyncVar(hook = nameof(AssignSnapped))]
        public NetworkIdentity snappedId;

        [SerializeField]
        private List<ImposterGameObject> imposterGameObjects = new();

        // Dictionary to quickly access Imposter GameObjects by name
        public Dictionary<string, GameObject> objectDictionary = new Dictionary<string, GameObject>();

        // cache the snapped object Name so it can be used to disable the object after unsnapping the object
        public string snappedObjectName;

        public List<MeshRenderer> meshRenderers = new();

        private void Awake()
        {
            // get the snapZone Component
            snapZone = GetComponent<SnapZone>();
            // get the Network Identity of root weapon, so we can use it to assign ownership of the snapped object
            rootNetId = rootGrabbable.GetComponent<NetworkIdentity>();

            // Populate the dictionary with entries from the imposterGameObjects list
            foreach (ImposterGameObject entry in imposterGameObjects)
            {
                if (entry.imposterObject != null && !objectDictionary.ContainsKey(entry.nameReference))
                {
                    objectDictionary[entry.nameReference] = entry.imposterObject;
                    //Debug.Log(entry.nameReference);
                }
                else
                {
                    Debug.LogWarning($"Duplicate or missing GameObject entry for {entry.nameReference}");
                }
            }
        }


        public void OnEnable()
        {
            snapZone.OnSnapEvent.AddListener(SetSnapped);
            snapZone.OnDetachEvent.AddListener(SetDetached);
        }

        public void OnDisable()
        {
            snapZone.OnSnapEvent.RemoveListener(SetSnapped);
            snapZone.OnDetachEvent.RemoveListener(SetDetached);
        }

        // set from unity grabbable events on grab
        public void RootGabbableGrab()
        {
            StartCoroutine(AwaitOwnerShip());
        }

        // wait till we own the root grabbable to prevent firing of logic before we own the root object
        public IEnumerator AwaitOwnerShip()
        {
            while(!isOwned)
            {
                yield return null;
            }

            CmdSetRootStatus();
        }

        [Command]
        void CmdSetRootStatus()
        {
            // set syncvar hook bool to run logic on clients
            canRemove = !canRemove;
            // if we do not own the snapped object, assign the ownership of the object to the owner of the root grabable 
            if(snappedId != null && snappedId.connectionToClient != rootNetId.connectionToClient)
            {
                // reset the rigidbody velocity to avoid odd physics behavior
                Rigidbody rb = snappedId.GetComponent<Rigidbody>();
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // assign the authority to the new owner
                snappedId.RemoveClientAuthority();
                snappedId.AssignClientAuthority(rootNetId.connectionToClient);
            }
        }

        // set the snapzones CanRemoveItem depending on if we own the object, we take ownership when we pick up the weapon, so the runs and sets that bool
        public void SetCanRemoveSnapped(bool oldCanRemove, bool newCanRemove)
        {
            // the object is not owned by the local player, then set the CanRemoveItem to false, so another client can't grab it off the weapon
            if (!isOwned)
            {
                snapZone.CanRemoveItem = false;
            }
            // if we do own it and it is currently set to false, set it to true
            else if (isOwned && snapZone.CanRemoveItem == false)
            {
                snapZone.CanRemoveItem = true;
            }
        }

        // ran from on snap event listener
        public void SetSnapped(Grabbable grab)
        {
            // we only want to run this if we are the owner 
            if (isOwned)
            {
                NetworkIdentity snappedId = grab.GetComponent<NetworkIdentity>();
                CmdSetHeldItem(snappedId);
            }
        }

        // set the syncvar networkIdenty of the item to sync the network identity
        [Command]
        void CmdSetHeldItem(NetworkIdentity heldNetId)
        {
            snappedId = heldNetId;
        }

        public void AssignSnapped(NetworkIdentity oldNetId, NetworkIdentity newNetId)
        {
            if (snappedId == null)
            {
                // set the imposter game object, this is the one that is visible for one to one movement to false making it invisible, this is a child object of the weapon       
                DisableObjectImposter(snappedObjectName);
                snappedObjectName = "";
                // enable the meshrenders on the snapped object 
                for (int i = meshRenderers.Count - 1; i >= 0; i--)
                {
                    meshRenderers[i].enabled = true;
                    //remove the mesh render from the list, we no longer need it, it needs removed so the list doesn't stack
                    meshRenderers.RemoveAt(i);
                }
                // release the object on all clients because if the snapped id is null then so should the held object
                snapZone.ReleaseAll();
            }

            // this seems redundent of the above, but when you snap an object locally, the only one that is null is the other clients, so this only runs on remote clients, not locally
            else if (snapZone.HeldItem == null)
            {              
                // assign the grabbable to other clients
                Grabbable grab = snappedId.GetComponent<Grabbable>();
                snapZone.GrabGrabbable(grab);
            }
            // if the snapped id does is not null then we have a snapped object and need disable the mesh renders on the snapped object and enable the imposter object to give a visual one to one movement
            if (snappedId != null)
            {
                // enable the imposter object for visuals, this is a child of the weapon
                //snapImposterObject.SetActive(true);
                snappedObjectName = CleanName(snappedId.name);
                EnableObjectImposter(snappedObjectName);

                // disable the mesh renders of the snapped object
                meshRenderers.AddRange(snapZone.HeldItem.GetComponentsInChildren<MeshRenderer>());
                
                foreach (MeshRenderer mesh in meshRenderers)
                {
                    mesh.enabled = false;
                }
            }

        }

        //sync detach event
        void SetDetached(Grabbable grab)
        {
            // if we own the object and we detach the snapped object send the command to set the item to null to reverse all the snap logic
            if (isOwned)
            {
                CmdSetHeldItem(null);
            }
        }

        // Method to enable a GameObject by its custom name
        public void EnableObjectImposter(string objectName)
        {

            if (objectDictionary.TryGetValue(objectName, out GameObject obj))
            {
                obj.SetActive(true);
                Debug.Log($"Enabled {objectName}");
            }
            else
            {
                Debug.LogWarning($"No GameObject found with the name {objectName}");
            }
        }

        // Method to disable a GameObject by its custom name
        public void DisableObjectImposter(string objectName)
        {
            if (objectDictionary.TryGetValue(objectName, out GameObject obj))
            {
                obj.SetActive(false);
                Debug.Log($"Disabled {objectName}");
            }
            else
            {
                Debug.LogWarning($"No GameObject found with the name {objectName}");
            }
        }

        // Method to clean up duplicate names that end with (1) etc
        private string CleanName(string name)
        {
            // Find the index of '(' and remove everything after it
            int index = name.IndexOf('(');
            if (index != -1)
            {
                return name.Substring(0, index).Trim(); // Trim to remove any extra spaces
            }

            return name;
        }
    }

    [System.Serializable]
    public class ImposterGameObject
    {
        public string nameReference; // Custom name to use as the key
        public GameObject imposterObject; // Reference to the GameObject
    }
}
