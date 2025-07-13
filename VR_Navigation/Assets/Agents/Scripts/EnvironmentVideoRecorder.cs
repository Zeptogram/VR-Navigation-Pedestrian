#if UNITY_EDITOR
using UnityEngine;
using System;
using System.IO;

public class EnvironmentVideoRecorder : MonoBehaviour
{
    public string screenshotFolder = "Assets/Stats/Videos";
    public int extraEpisodes = 10;
    private int extraCounter = 0;
    private bool isRecording = false;
    private Action onExtraEpisodesComplete;
    private string runId = "";
    private string environmentName = "";
    private string sessionFolder = "";
    private static string lastRunId = "";
    private static string lastSessionFolder = "";
    private static string lastDateTime = "";
    private int screenshotCount = 0;

    public bool IsRecording => isRecording;

    public void StartRecording(Action onComplete, string runId, string environmentName)
    {
        if (isRecording)
        {
            StopRecording();
        }
        this.runId = runId;
        this.environmentName = environmentName;
        // Crea la cartella sessione solo una volta per runId+dataora
        if (string.IsNullOrEmpty(lastDateTime) || lastRunId != runId)
        {
            lastDateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            lastRunId = runId;
        }
        string safeEnvName = CleanFileName(environmentName);
        string runFolder = $"{runId}_{lastDateTime}";
        string rootFolder = Path.Combine(screenshotFolder, runFolder);
        // Crea la sottocartella per l'ambiente
        string envFolder = Path.Combine(rootFolder, safeEnvName);
        lastSessionFolder = envFolder;
        if (!Directory.Exists(lastSessionFolder))
        {
            Directory.CreateDirectory(lastSessionFolder);
        }
        sessionFolder = lastSessionFolder;
        onExtraEpisodesComplete = onComplete;
        extraCounter = 0;
        // Imposta screenshotCount in base ai file già presenti nella cartella
        string[] existingFiles = Directory.GetFiles(sessionFolder, $"{safeEnvName}_*.png");
        if (existingFiles.Length > 0)
        {
            int maxIndex = 0;
            foreach (var file in existingFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                int underscore = name.LastIndexOf('_');
                if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int idx))
                {
                    if (idx >= maxIndex) maxIndex = idx + 1;
                }
            }
            screenshotCount = maxIndex;
        }
        else
        {
            screenshotCount = 0;
        }
        isRecording = true;
    }

    public void OnEpisodeEnd()
    {
        if (!isRecording) return;
        TakeScreenshot();
        extraCounter++;
        if (extraCounter >= extraEpisodes)
        {
            StopRecording();
            onExtraEpisodesComplete?.Invoke();
        }
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private System.Collections.IEnumerator CaptureScreenshotCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        string safeEnvName = CleanFileName(environmentName);
        string filename = Path.Combine(sessionFolder, $"{safeEnvName}_{screenshotCount:D4}.png");
        ScreenCapture.CaptureScreenshot(filename);
        screenshotCount++;
    }


    private void StopRecording()
    {
        isRecording = false;
    }

    private string CleanFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
}
#endif
