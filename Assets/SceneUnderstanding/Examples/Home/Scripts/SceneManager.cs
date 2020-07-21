using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public void LoadUnderstanding()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Understanding-Simple");
    }

    public void LoadPlacement()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Placement-Simple");
    }

    public void LoadNavMesh()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("NavMesh-Simple");
    }
}
