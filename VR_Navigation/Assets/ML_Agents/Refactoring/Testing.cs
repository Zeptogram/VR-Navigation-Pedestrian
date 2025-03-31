using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using Unity.MLAgents.Policies;
using UnityEngine;

public class Testing : MonoBehaviour
{
    public string runId;
    [Header("Test settings")]
    public int repetitions;
    private int iteration = 0;
    public CurriculumSO testList;

    public EnvironmentStruct[] TestEnvironments => testList.Environments;

    private int environmentIndex = 0;


    private void Awake()
    {
        StatsWriter.setupTesting(runId);
    }

    void Start()
    {
        CreateEnvironmentsTesting(TestEnvironments);
    }

    void CreateEnvironmentsTesting(EnvironmentStruct[] environments)
    {
        StatsWriter.ChangeEnvSetup(environments[environmentIndex].envGameObject.ToString());
        DestroyAllEnvironments();

        var instantiatedEnvironment = Instantiate(environments[environmentIndex].envGameObject, new Vector3(0, 0, 0), Quaternion.identity, transform);
        instantiatedEnvironment.environmentTerminated += EnvironmentTerminated;
        TakeScrenshot(environments[environmentIndex].envGameObject.ToString());
    }


    private void EnvironmentTerminated(float finalScore, Environment env)
    {
        iteration++;
        if (iteration >= repetitions)
        {
            if (environmentIndex + 1 < TestEnvironments.Length)
            {
                iteration = 0;
                environmentIndex++;
                CreateEnvironmentsTesting(TestEnvironments);
            }
            else
            {
                iteration = 0;
                DestroyAllEnvironments();
# if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
# endif 
            }
        }

    }

    public void DestroyAllEnvironments()
    {
        List<Transform> toKill = new List<Transform>();

        foreach (Transform child in transform)
        {
            toKill.Add(child);
        }

        for (int i = toKill.Count - 1; i >= 0; i--) 
        {
            Destroy(toKill[i].gameObject);
        }
    }
    public void TakeScrenshot(string env)
    {
        ScreenCapture.CaptureScreenshot(Application.dataPath + "/Stats/Screenshot Env/" + env + ".png");
    }
}
