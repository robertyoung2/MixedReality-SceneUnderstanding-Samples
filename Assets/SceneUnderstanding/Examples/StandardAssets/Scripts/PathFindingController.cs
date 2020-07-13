using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows.Speech;

public class PathFindingController : MonoBehaviour
{
    public void MoveAgent()
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
        {
            GameObject gbjNavMeshAgent = GameObject.FindGameObjectWithTag("NavAgent");
            gbjNavMeshAgent.GetComponent<NavMeshAgent>().SetDestination(raycastHit.point);
        }
    }
}
