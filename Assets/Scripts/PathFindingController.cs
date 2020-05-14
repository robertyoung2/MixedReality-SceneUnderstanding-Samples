using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathFindingController : MonoBehaviour
{
    private GameObject gbjNavMeshAgent;
    private Camera mainCamera;
    private RaycastHit raycastHit;
    public GameObject gbjHelperLine;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward),Color.green);
        //DrawLine(mainCamera.transform.position, raycastHit.point);

        if(Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
        {
            if(Input.GetKeyDown(KeyCode.G))
            {
                MoveAgent();
            }
        }
    }

    public void MoveAgent()
    {
        gbjNavMeshAgent = GameObject.FindGameObjectWithTag("NavAgent");
        gbjNavMeshAgent.GetComponent<NavMeshAgent>().SetDestination(raycastHit.point);
    }

    void DrawLine(Vector3 vc3Start, Vector3 vc3End)
        {
            gbjHelperLine.SetActive(true);
            LineRenderer line = gbjHelperLine.GetComponent<LineRenderer>();
            line.startWidth = 0.15f;
            line.endWidth = 0.15f;
            line.positionCount = 2;
            line.SetPosition(0, vc3Start - (Vector3.up * 0.2f));
            line.SetPosition (1, vc3End);
        }
}
