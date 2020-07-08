using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Windows.Speech;

public class ObjectPlacer : MonoBehaviour
{
    public GameObject objToPlaceRef;
    private GameObject objToPlace = null;
    private bool isPlacing = false;

    //XR Inputs
    public delegate void InvokeCommand();
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string,InvokeCommand> speechCommands = new Dictionary<string, InvokeCommand>();

    //Container for all instantiated objects/holograms
    private List<GameObject> HoloObjects =  new List<GameObject>();

    void Start()
    {
        speechCommands.Add("place", new InvokeCommand( () =>
        {
            Place();
        }));

        speechCommands.Add("spray", new InvokeCommand( () =>
        {
            Spray();
        }));

        List<string> keywordsList = new List<string>();

        foreach(KeyValuePair<string,InvokeCommand> command in speechCommands)
        {
            keywordsList.Add(command.Key);
        }

        keywordRecognizer = new KeywordRecognizer(keywordsList.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string arg = args.text;
        Debug.Log("ObjectPlacer.OnPhraseRecognized: Phrase '" + arg + "' recognized");

        InvokeCommand cmd = speechCommands[arg];

        if(cmd != null)
        {
            cmd();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Place an Object
        if(Input.GetKeyDown(KeyCode.C))
        {
            Place();
        }

        //Spray primitives
        if(Input.GetKeyDown(KeyCode.V))
        {
            Spray();
        }

        UpdateObjPos();
    }

    private void Place()
    {
        if(!isPlacing)
        {
            StartPlacing();
        }
        else
        {
            FinishPlacing();
        }
        isPlacing = !isPlacing;
    }

    private void StartPlacing()
    {
        objToPlace = Instantiate<GameObject>(objToPlaceRef, Vector3.zero, Quaternion.identity);

        //Add object to the list
        HoloObjects.Add(objToPlace);

        //Disable collider for base object if it has any
        Collider ParentCollider = objToPlace.GetComponent<Collider>();
        if(ParentCollider != null)
        {
            ParentCollider.enabled = false;
        }

        //Disable colliders for any child objects if any exists
        foreach(Transform child in objToPlace.transform)
        {
            Collider childCollider = child.GetComponent<Collider>();

            if(childCollider != null)
            {
                childCollider.enabled = false;
            }
        }
    }

    private void FinishPlacing()
    {
        //Enable collider for base object if it has any
        Collider ParentCollider = objToPlace.GetComponent<Collider>();
        if(ParentCollider != null)
        {
            ParentCollider.enabled = true;
        }

        //Enable colliders for any child objects if any exists
        foreach(Transform child in objToPlace.transform)
        {
            Collider childCollider = child.GetComponent<Collider>();

            if(childCollider != null)
            {
                childCollider.enabled = true;
            }
        }

        objToPlace = null;
    }

    private void UpdateObjPos()
    {
        if(objToPlace == null)
        {
            return;
        }

        Vector3 newObjPosition = GetObjPos();
        objToPlace.transform.position = newObjPosition;
    }

    private Vector3 GetObjPos()
    {
        RaycastHit hit;
        bool hasTarget = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit);
        
        Vector3 newObjPos = Vector3.zero;
        if(hasTarget)
        {
            Vector3 selectedObjFacingTowards = -hit.transform.forward.normalized;
            newObjPos = Vector3.Dot(Camera.main.transform.TransformDirection(Vector3.forward), selectedObjFacingTowards) < 0 ? hit.point + (selectedObjFacingTowards * 0.3f) : hit.point - (selectedObjFacingTowards * 0.3f);
        }
        else
        {
            newObjPos = Camera.main.transform.position + (Camera.main.transform.forward * 2.0f);
        }

        return newObjPos;
    }

    private void Spray()
    {
        StartCoroutine(SprayCoroutine());
    }

    IEnumerator SprayCoroutine()
    {
        Material mat = Resources.Load<Material>("Materials/SceneObjectMesh");
        yield return null;

        for(int i=0; i<10; i++)
        {
            PrimitiveType pt = i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere;
            
            //Init
            GameObject tempgbj = GameObject.CreatePrimitive(pt);
            tempgbj.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
            tempgbj.GetComponent<MeshRenderer>().material = mat;
            tempgbj.AddComponent<Rigidbody>();

            //Set Pos and add force
            tempgbj.transform.position = Camera.main.transform.position + (Camera.main.transform.forward * 1.0f);
            tempgbj.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * 3.0f, ForceMode.Impulse);

            //Add primitive to list
            HoloObjects.Add(tempgbj);

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void OnLoad()
    {
        //When the scene loads/reloads delete all holograms/placed objects
        foreach(GameObject obj in HoloObjects)
        {
            Destroy(obj);
        }
    }
}
