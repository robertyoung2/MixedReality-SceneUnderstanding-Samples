using UnityEngine;
using UnityEngine.AI;

public class NavMeshGenerator : MonoBehaviour
{
    public NavMeshSurface navMeshSurf;
    public GameObject SceneRoot;
    public GameObject navMeshAgentRef;

    private GameObject navMeshAgentInstance;

    public void BakeMesh()
    {
        UpdateNavMeshSettingsForObjsUnderRoot();
        navMeshSurf.BuildNavMesh();
        CreateNavMeshAgent();
    }

    void CreateNavMeshAgent()
    {
        if(navMeshAgentRef == null)
        {
            return;
        }

        if(navMeshAgentInstance == null)
        {
            navMeshAgentInstance = Instantiate<GameObject>(navMeshAgentRef, new Vector3(0.0f,-0.5f,-3.0f), Quaternion.identity);
        }
    }

    void UpdateNavMeshSettingsForObjsUnderRoot ()
    {
        //Iterate all the Scene Objects
        foreach(Transform SceneObjContainer in SceneRoot.transform)
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
