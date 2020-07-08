using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows.Speech;

public class PathFindingController : MonoBehaviour
{
    private GameObject gbjNavMeshAgent;
    private RaycastHit raycastHit;

    //XR Inputs
    public delegate void InvokeCommand();
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string,InvokeCommand> speechCommands = new Dictionary<string, InvokeCommand>();

    void Start()
    {
        speechCommands.Add("go", new InvokeCommand( () =>
        {
            MoveAgent();
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
        Debug.Log("PathFindingController.OnPhraseRecognized: Phrase '" + arg + "' recognized");

        InvokeCommand cmd = speechCommands[arg];

        if(cmd != null)
        {
            cmd();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
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

}
