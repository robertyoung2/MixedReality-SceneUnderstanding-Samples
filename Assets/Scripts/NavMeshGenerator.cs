using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshGenerator : MonoBehaviour
{
    public NavMeshSurface navmeshSurf;
    public GameObject gbjRoot;

    private GameObject gbjNavAgent;

    public Material SceneObjectMesh;

    public Mesh mshPawn;

    public Microsoft.MixedReality.SceneUnderstanding.Samples.Unity.InputManager inputmnger;

    // Start is called before the first frame update
    void Start()
    {
        gbjNavAgent = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BakeMesh()
    {
        UpdateNavMeshSettingsForObjsUnderRoot();
        navmeshSurf.BuildNavMesh();
        CreateNavMeshAgent();
    }

    void CreateNavMeshAgent()
    {
        if(gbjNavAgent != null)
        {
            return;
        }
        
        //Setup NavMesh Agent Settings
        gbjNavAgent = new GameObject("NavAgent");
        NavMeshAgent nva = gbjNavAgent.AddComponent<NavMeshAgent>();
        nva.updateUpAxis = false;
        nva.updateRotation = false;
        nva.baseOffset = 1.7f;

        //Setup the rest
        gbjNavAgent.transform.tag = "NavAgent";
        gbjNavAgent.transform.name = "NavAgent";
        gbjNavAgent.transform.position = new Vector3(0.0f,-0.5f,-3.0f);
        gbjNavAgent.transform.rotation = Quaternion.Euler(270.0f,0.0f,0.0f);
        gbjNavAgent.transform.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
        gbjNavAgent.AddComponent<MeshFilter>().sharedMesh = mshPawn;
        gbjNavAgent.AddComponent<MeshRenderer>().material = SceneObjectMesh;

        //The agent itself shoudln't be considered as part of the enviroment, Layer 8 is set to Ignore NavMesh
        gbjNavAgent.layer = 8;
    }

    void UpdateNavMeshSettingsForObjsUnderRoot ()
    {
        //Iterate all the Scene Objects
        foreach(Transform SceneObjContainer in gbjRoot.transform)
        {
            foreach(Transform SceneObj in SceneObjContainer.transform)
            {
                NavMeshModifier nvm = SceneObj.gameObject.AddComponent<NavMeshModifier>();
                
                //Walkable = 0, Not Walkable = 1
                nvm.overrideArea = true;
                nvm.area = SceneObj.name == "Floor" ? 0 : 1;
            }
        }
    }

}
