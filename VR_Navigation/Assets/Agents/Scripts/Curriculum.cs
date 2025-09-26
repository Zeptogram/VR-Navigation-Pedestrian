using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;
using UnityEditor;

public class Curriculum : MonoBehaviour
{
    public string runId;

    [Header("Train settings")]

    private bool train = true;

    public bool EarlyFail;
    public int episodeNumberForMean;
    public List<float> scoreList;

    [Header("Train settings")]
    [Range(1, 5)] public int maxI;
    [Range(1, 5)] public int maxJ;
    public CurriculumSO curriculumList;
    [Header("Retrain settings")]
    [Space(10)]
    public bool retrain;
    public bool infiniteRetrain;
    [Range(1, 5)] public int retrainEnv;

    public CurriculumSO retrainCurriculumList;
    [Header("Consolidation settings")]
    [Space(10)]
    public bool consolidation;
    public int numberOfEpisodeForConsolidation;
    [Range(1, 5)] public int consolidationEnv;
    public CurriculumSO consolidatioCurriculumList;

    public EnvironmentStruct[] curriculumEnvironments => curriculumList.Environments; //List containing the curriculum environments
    public EnvironmentStruct[] retrainEnvironments => retrainCurriculumList.Environments;
    public EnvironmentStruct[] consolidatioEnvironments => consolidatioCurriculumList.Environments;


    public static bool isTrainig = false;



    // Start is called before the first frame update
    //Index to keep track of the current env
    private int environmentIndex = 0;

    private int episodeNumber = 0;

    private Dictionary<string, List<float>> retrainEnvNameListScore = new Dictionary<string, List<float>>();
    private Dictionary<string, float> retrainEnvNameMinScore = new Dictionary<string, float>();

    //consolidation
    private Dictionary<string, float> consolidationEnvNameNumberOfEpisode = new Dictionary<string, float>();


    private void Awake()
    {
        StatsWriter.setupCurriculum(runId, curriculumEnvironments[0].envGameObject.ToString(), curriculumEnvironments[0].minScore, this);
    }

    private void Start()
    {
        CreateEnvironmentsTraining(curriculumEnvironments);
    }

    void CreateEnvironmentsTraining(EnvironmentStruct[] environments)
    {
        DestroyAllEnvironments();
        StatsWriter.WriteChangeEnv(curriculumEnvironments[environmentIndex].envGameObject.ToString(),
                        curriculumEnvironments[environmentIndex].minScore,
                        "train");
        for (int i = 0; i < maxI; i++)
        {
            for (int j = 0; j < maxJ; j++)
            {
                var instantiatedEnvironment = Instantiate(environments[environmentIndex].envGameObject, new Vector3(i * 50, 0, j * 50), Quaternion.identity, transform);
                instantiatedEnvironment.environmentTerminated += EnvironmentTerminated;

            }
        }
    }

    void CreateEnvironmentsRetraining(EnvironmentStruct[] environments)
    {
        DestroyAllEnvironments();
        for (int i = 0; i < retrainEnv; i++)
        {
            for (int j = 0; j < environments.Length; j++)
            {
                var instantiatedEnvironment = Instantiate(environments[j].envGameObject, new Vector3(i * 50, 0, j * 50), Quaternion.identity, transform);
                instantiatedEnvironment.environmentTerminated += EnvironmentTerminated;
                retrainEnvNameListScore[instantiatedEnvironment.name] = new List<float>();
                retrainEnvNameMinScore[instantiatedEnvironment.name] = environments[environmentIndex].minScore;
            }

        }
    }

    void CreateEnvironmentsConsolidation(EnvironmentStruct[] environments)
    {
        DestroyAllEnvironments();
        for (int i = 0; i < retrainEnv; i++)
        {
            for (int j = 0; j < environments.Length; j++)
            {
                var instantiatedEnvironment = Instantiate(environments[j].envGameObject, new Vector3(i * 50, 0, j * 50), Quaternion.identity, transform);
                instantiatedEnvironment.environmentTerminated += EnvironmentTerminated;
                consolidationEnvNameNumberOfEpisode[instantiatedEnvironment.name] = numberOfEpisodeForConsolidation;

            }
        }
    }

    public void DestroyEnvironments(string env)
    {
        List<Transform> toKill = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.name == env) toKill.Add(child);
        }

        for (int i = toKill.Count - 1; i >= 0; i--)
        {
            Destroy(toKill[i].gameObject);
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

    private void EnvironmentTerminated(float finalScore, Environment env)
    {
        if (train)
        {
            TrainingHandler(finalScore);
        }
        else if (retrain)
        {
            RetrainingHandler(finalScore, env);
        }
        else if (consolidation)
        {
            ConsolidationHandler(finalScore, env);
        }
        else
        {
            DestroyAllEnvironments();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("all phases were successfully completed");
#endif
        }
    }

    private void ConsolidationHandler(float finalScore, Environment env)
    {
        consolidationEnvNameNumberOfEpisode[env.name]--;

        if (consolidationEnvNameNumberOfEpisode[env.name] <= 0)
        {
            DestroyEnvironments(env.name);
            StartCoroutine(KillLastEnvConsolidation());

        }
    }
    private IEnumerator KillLastEnv()
    {
        yield return new WaitForSeconds(5f);
        if (transform.childCount <= 0)
        {
            Debug.Log("Retraining phase completed");
            StatsWriter.WriteChangeEnv("Consolidation",
                        0,
                        "consolidation");
            retrain = false;
            if (consolidation)
            {
                episodeNumber = 0;
                environmentIndex = 0;

                CreateEnvironmentsConsolidation(consolidatioEnvironments);
            }
            else
            {
                StatsWriter.WriteChangeEnv("End", 0, "End");
            }
        }
    }

    private IEnumerator KillLastEnvConsolidation()
    {
        yield return new WaitForSeconds(5f);
        if (transform.childCount <= 0)
        {
            StatsWriter.WriteChangeEnv("End",
                        0,
                        "End");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("all phases were successfully completed");
#endif
        }
    }

    private void RetrainingHandler(float score, Environment env)
    {

        if (!infiniteRetrain)
        {

            retrainEnvNameListScore[env.name].Add(score);
            var thisScoreList = retrainEnvNameListScore[env.name];
            if (thisScoreList.Count() >= episodeNumberForMean)
            {
                List<float> avgScore = averageScore(thisScoreList);
                retrainEnvNameListScore[env.name].Clear();
                //print("SCORE MEDIO :" + avgScore[1]);

                if (avgScore[1] >= retrainEnvNameMinScore[env.name])
                {
                    DestroyEnvironments(env.name);
                    StartCoroutine(KillLastEnv());

                }
            }
        }
    }

    private void TrainingHandler(float score)
    {
        episodeNumber++;
        if (episodeNumber >= curriculumEnvironments[environmentIndex].maxEpisodes)
        {
            earlyFail();
        }
        scoreList.Add(score);
        if (scoreList.Count >= episodeNumberForMean)
        {
            List<float> avgScore = averageScore(scoreList);
            scoreList = new List<float>();
            //print("SCORE MEDIO :" + avgScore[1]);

            if (avgScore[1] >= curriculumEnvironments[environmentIndex].minScore)
            {

                if (environmentIndex + 1 < curriculumEnvironments.Length)
                {
                    episodeNumber = 0;
                    environmentIndex++;
                    CreateEnvironmentsTraining(curriculumEnvironments);
                }
                else
                {
                    Debug.Log("Training phase completed");

                    train = false;
                    if (retrain)
                    {
                        episodeNumber = 0;
                        environmentIndex = 0;
                        StatsWriter.WriteChangeEnv("Retraining",
                        0,
                        "retrain");
                        CreateEnvironmentsRetraining(retrainEnvironments);
                    }
                    else {
                        StatsWriter.WriteChangeEnv("End", 0, "End");
                    }
                }
            }
        }
    }

    private List<float> averageScore(List<float> l)
    {

        int trim = (int)Mathf.Floor(episodeNumberForMean / 10f);
        float raw = l.Average();

        l.Sort();
        l.RemoveRange(l.Count - trim, trim);
        l.RemoveRange(0, trim);
        float trimmed = l.Average();

        List<float> result = new List<float> { raw, trimmed };

        return result;
    }
    private void earlyFail()
    {
        if (EarlyFail)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("Early fail " + episodeNumber + " episodes");
#endif
        }

    }
}
