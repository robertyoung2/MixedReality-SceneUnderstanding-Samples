using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathFindingController : MonoBehaviour
{
    public GameObject gbjNavMeshAgent;
    private Camera mainCamera;
    private RaycastHit raycastHit;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward),Color.green);

        if(Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
        {
            if(Input.GetKeyDown(KeyCode.G))
            {
                gbjNavMeshAgent = GameObject.FindGameObjectWithTag("NavAgent");
                gbjNavMeshAgent.GetComponent<NavMeshAgent>().SetDestination(raycastHit.point);
            }
        }
    }
}
