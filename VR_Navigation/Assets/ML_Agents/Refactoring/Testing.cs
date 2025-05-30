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
    [Tooltip("Numero di ambienti in cui eseguire screenshot (per ogni ambiente, screenshot ad ogni step per tutta la durata)")]
    public int numEnvironmentsWithScreenshots = 10;
    private int iteration = 0;
    public CurriculumSO testList;

    public EnvironmentStruct[] TestEnvironments => testList.Environments;

    private int environmentIndex = 0;
    private EnvironmentVideoRecorder videoRecorder;
    private bool waitingExtraEpisodes = false;

    private void Awake()
    {
        StatsWriter.setupTesting(runId);
    }

    private void Start()
    {
        videoRecorder = gameObject.AddComponent<EnvironmentVideoRecorder>();
        videoRecorder.extraEpisodes = 0; // Disabilita la logica extra
        CreateEnvironmentsTesting(TestEnvironments);
        iteration = 0;
        waitingExtraEpisodes = (iteration < numEnvironmentsWithScreenshots); // Screenshot per i primi n episodi di ogni ambiente
    }

    void CreateEnvironmentsTesting(EnvironmentStruct[] environments)
    {
        StatsWriter.ChangeEnvSetup(environments[environmentIndex].envGameObject.ToString());
        DestroyAllEnvironments();

        var instantiatedEnvironment = Instantiate(environments[environmentIndex].envGameObject, new Vector3(0, 0, 0), Quaternion.identity, transform);
        instantiatedEnvironment.environmentTerminated += EnvironmentTerminated;
        string envName = environments[environmentIndex].envGameObject != null ? environments[environmentIndex].envGameObject.name : "UnknownEnv";
        TakeInitialScreenshot(environments[environmentIndex].envGameObject.ToString());
        videoRecorder.StartRecording(null, runId, envName); // Avvia la registrazione per la cartella strutturata
        
    }


    private void EnvironmentTerminated(float finalScore, Environment env)
    {
        iteration++;
        waitingExtraEpisodes = (iteration < numEnvironmentsWithScreenshots);
        if (iteration >= repetitions)
        {
            if (environmentIndex + 1 < TestEnvironments.Length)
            {
                iteration = 0;
                environmentIndex++;
                CreateEnvironmentsTesting(TestEnvironments);
                waitingExtraEpisodes = (iteration < numEnvironmentsWithScreenshots);
            }
            else
            {
                iteration = 0;
                DestroyAllEnvironments();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }
    }

    // Metodo da chiamare ad ogni step dell'agente (deve essere chiamato dal RLAgent)
    public void OnAgentStep()
    {
#if UNITY_EDITOR
        if (waitingExtraEpisodes && videoRecorder != null)
        {
            videoRecorder.TakeScreenshot(); // Salva nella cartella strutturata come in training
        }
#endif
    }

    private bool isTakingScreenshot = false;

    public void TakeInitialScreenshot(string envName)
    {
        if (isTakingScreenshot) return;
        StartCoroutine(CaptureScreenshotCoroutine(envName));
    }

    private IEnumerator CaptureScreenshotCoroutine(string envName)
    {
        isTakingScreenshot = true;
        yield return new WaitForEndOfFrame();
        string folderPath = Application.dataPath + "/Stats/Screenshot Env/";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        string fileName = folderPath + envName + ".png";
        ScreenCapture.CaptureScreenshot(fileName);
        Debug.Log("Screenshot salvato in: " + fileName);
        // Attendi un altro frame per sicurezza
        yield return new WaitForEndOfFrame();
        isTakingScreenshot = false;
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
}

