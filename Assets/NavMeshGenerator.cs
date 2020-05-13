using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshGenerator : MonoBehaviour
{
    public NavMeshSurface navmeshSurf;
    public GameObject gbjRoot;

    private GameObject gbjNavAgent;

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

        gbjNavAgent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gbjNavAgent.transform.tag = "NavAgent";
        gbjNavAgent.transform.name = "NavAgent";
        gbjNavAgent.transform.position = new Vector3(0.0f,-1.0f,-3.0f);
        gbjNavAgent.transform.rotation = Quaternion.identity;
        gbjNavAgent.transform.transform.localScale = new Vector3(0.5f,0.5f,0.5f);
        //Layer 8 Ignores NavMesh
        gbjNavAgent.layer = 8;

        gbjNavAgent.AddComponent<NavMeshAgent>();
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
