using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;
using UnityEditor;

public class CurriculumHandler : MonoBehaviour
{
    //Name of the folder used for Logs
    [Header("Run id")]
    public String runID;
    //The grid is structured in a ixj environments
    [Header("Environment grid (i * j)")]
    public int maxI;
    public int maxJ;
    [Space(10)]
    [Tooltip("Number of necessary episodes to calculate the average reward")]
    public int nEpisodes;
    [Header("Curriculum Mode")]
    [Tooltip("Check if you want to run a test run, when the env is completed it won't be restarted. Place the testing envs in the training list")]
    public bool testing;
    [Tooltip("Brain used by the agents. If testing is checked place here a NN model")]
    public NNModel brain;

    [Tooltip("Check if you want to start from the retrain phase")]
    public bool skipTraining;
    [Tooltip("Check if you want to run the retrain phase")]
    public bool includeRetrain;
    [Tooltip("Check if you want to run a consolidation phase")]
    public bool includeConsolidation;
    [Tooltip("Percentage of maxEpisode to be used for the consolidation run")]
    [Range(0f,1f)]public float consolidationPercentage;

    public float maxScore = -100;

    [Header("Score list")]
    public List<float> scoreList; //list containg the scores of the finished episodes (max lenght = nEpisodes)


    //Counter to check for early stopping in training phase
    private int trainingEpisodeCounter = 0;
    private string currentPhase = "train";
    public int testedRun;
    private int testedRunIndex;


    //data structure containing env training informations
    [Serializable] class TrainData{
        public string envName;
        public double endTime;
        public float meanScore;
        public float scoreRaw;
    }


    //datas structure containing env retraining info
    //Different from TrainData because multiple different env are executed at the same time
    private struct RetrainingData{
        public string envName;
        public List<float> scoresList;
        //counter used to compute averages
        public int avgCounter;
        //counter used to check the early fail
        public int totalCounter;
    }

    [Header("curriculum environments list")]
    public CurriculumSO curriculumList;
    public EnvironmentStruct[] curriculumEnvironments => curriculumList.Environments; //List containing the curriculum environments

    [Header("Reatrain environments list")]
    public CurriculumSO retrainCurriculumList;
    public EnvironmentStruct[] retrainEnvironments => retrainCurriculumList.Environments; //List containing the retraining environments


    //Used to store data regarding retraining environments
    private Dictionary<string,RetrainingData> retrainingDataDict;
    //Support dictionary to order the json when we will write on file
    private Dictionary<string,List<TrainData>> retrainTDsDict;

    //List containing the environment handlers for the current grid
    private EnvironmentHandler[] environmentHandlersList;

    ///Counter to identify the agents (we need it for logging)
    [NonSerialized] public int agentIDcounter;

    //Index to keep track of the current env
    private int environmentIndex = 0;



    //We take all the environment handlers from the environment grid we generated in Unity
    //put them in a list and bind the curriculum handler to them
    //If we have a 4x4 matrix of environments we will have a list with 16 environments handlers
    private void Awake()    {
        scoreList = new List<float>();
        environmentHandlersList = GetComponentsInChildren<EnvironmentHandler>(); 
        foreach (EnvironmentHandler amb in environmentHandlersList) amb.curriculumHandler = this;
    }

    private void Start(){
        testedRunIndex = testedRun;
        //Cleaning up the log files
        StreamWriter writer;

        writer = new StreamWriter("LogTraining/" + runID + "/EnvironmentChanges.txt", false);
        writer.Write("");
        writer.Close();

        //Using time to differ between different data we gather
        DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double cur_time = (DateTime.UtcNow - epochStart).TotalSeconds;

        //mean and raw score are 0 to differ between environment change
        TrainData td = new TrainData{
            envName = curriculumEnvironments[environmentIndex].envGameObject.name,
            endTime = cur_time,
            meanScore = 0,
            scoreRaw = 0
        };

        //Changing phase if needed, default is training
        if(testing){
            currentPhase = "test";
        }else if(skipTraining){ //if in retrain mode only skip the training phase completely
            DestroyEnvironments();
            CreateRetrainGrid(1f);
            currentPhase = "retrain";
        }
    }

    //Computing the raw and trimmed average score
    //[0] = raw average, [1] = trimmed average
    private List<float> averageScore(List<float> l){
        
        int trim = (int)Mathf.Floor(nEpisodes / 10f);
        float raw = l.Average();

        l.Sort();
        l.RemoveRange(l.Count - trim, trim);
        l.RemoveRange(0, trim);
        float trimmed = l.Average();
        
        List<float> result= new List<float>{raw,trimmed};

        return result;
    }

    //Early fail to stop if the env reached the max episodes, it's not converging
    private void earlyFail(EnvironmentStruct[] environments){
#if UNITY_EDITOR

        if (trainingEpisodeCounter > environments[environmentIndex].maxEpisodes){
            
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("Early fail " + trainingEpisodeCounter + " episodes");
        }
#endif
    }

    //Building data structure from scores
    private TrainData buildTrainData(List<float> scoresList, string name = ""){

        DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double cur_time = (DateTime.UtcNow - epochStart).TotalSeconds;
        
        if(currentPhase == "train") name = curriculumEnvironments[environmentIndex].envGameObject.name;
        
        TrainData td = new TrainData
        {
            envName = name,
            endTime = cur_time,
            meanScore = scoresList[1],
            scoreRaw = scoresList[0]
        };

        return td;
    }

    //Loggin environment changes to help with plotting
    //this txt is used to plot the graph
    private void logEnvironmentChange(){
        _ = environmentHandlersList[0].currentSteps;
        print(environmentHandlersList[0].currentSteps + "current step per grafico");
        DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double cur_time = (DateTime.UtcNow - epochStart).TotalSeconds;

        StreamWriter writer;
        writer = new StreamWriter("LogTraining/" + runID + "/EnvironmentChanges.txt", true);
        epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
        writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + curriculumEnvironments[environmentIndex].envGameObject.name + ";" + curriculumEnvironments[environmentIndex].minScore);
        writer.Close();
     
    }
     //Loggin retrain
    private void logRetrain(){
        _ = environmentHandlersList[0].currentSteps;
        print(environmentHandlersList[0].currentSteps + "current step per grafico");
        DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double cur_time = (DateTime.UtcNow - epochStart).TotalSeconds;

        StreamWriter writer;
        writer = new StreamWriter("LogTraining/" + runID + "/EnvironmentChanges.txt", true);
        epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
        writer.WriteLine(DateTime.Now + ";" + cur_time + ";Retrain" );
        writer.Close();
    }

    //Create the retraining grid
    //3 instances oif the same env per element in the retraining list
    private void CreateRetrainGrid(float episodesPercentage){
        
        //Dict used because the envs are updated in parallel and not sequentially
        retrainingDataDict = new Dictionary<string, RetrainingData>();
        retrainTDsDict = new Dictionary<string, List<TrainData>>();

        //Creating retraining environments grid
        for (int i = 0; i < retrainEnvironments.Length; i++){
            //Compute the maxEpisodes in case of consolidation run
            if(retrainEnvironments[i].maxEpisodes * episodesPercentage >= nEpisodes + 1){
                retrainEnvironments[i].maxEpisodes = (int)Math.Round(retrainEnvironments[i].maxEpisodes * episodesPercentage);
            }else{
                retrainEnvironments[i].maxEpisodes = nEpisodes + 1;
            }

            //3 istances of the same env
            EnvironmentHandler ga = Instantiate(retrainEnvironments[i].envGameObject, new Vector3(i * 103, 0, 0 * 103), Quaternion.identity, transform).GetComponent<EnvironmentHandler>();
       //     EnvironmentHandler ga2 = Instantiate(retrainEnvironments[i].envGameObject, new Vector3(i * 103, 0, 1 * 103), Quaternion.identity, transform).GetComponent<EnvironmentHandler>();
        //    EnvironmentHandler ga3 = Instantiate(retrainEnvironments[i].envGameObject, new Vector3(i * 103, 0, 2 * 103), Quaternion.identity, transform).GetComponent<EnvironmentHandler>();

            retrainingDataDict.Add(retrainEnvironments[i].envGameObject.name,new RetrainingData{
                envName = retrainEnvironments[i].envGameObject.name,
                scoresList = new List<float>(),
                avgCounter = 0,
                totalCounter = 0
            });
            retrainTDsDict.Add(retrainEnvironments[i].envGameObject.name,new List<TrainData>());
        }

        //Binding the environment handlers to the envs
        environmentHandlersList = GetComponentsInChildren<EnvironmentHandler>();
        foreach (EnvironmentHandler amb in environmentHandlersList) amb.curriculumHandler = this;
    }

    //Called by the environment handler when the episode is finished
    //envName is set only during the retraining because different envs runs at the same time
    public void EnvironmentTerminated(float finalScore,string envName = ""){
        if(currentPhase == "test"){
            //Testing phase

            testedRunIndex--;
            //checking if there are more envs to test
            if(testedRunIndex <= 0) {
            if (environmentIndex + 1 < curriculumEnvironments.Length )
            {
                testedRunIndex = testedRun;
                environmentIndex++;

                CreateEnvironments();

                environmentHandlersList = GetComponentsInChildren<EnvironmentHandler>();
                foreach (EnvironmentHandler amb in environmentHandlersList) amb.curriculumHandler = this;
            }
#if UNITY_EDITOR             
                else{
                    //No more envs to test, stop


                    DestroyEnvironments();
                UnityEditor.EditorApplication.isPlaying = false;
                }
#endif

            }

        }
        else if(currentPhase == "train"){
            //Training phase

            trainingEpisodeCounter++;

            earlyFail(curriculumEnvironments);
            scoreList.Add(finalScore);
            maxScore= Mathf.Max(maxScore, finalScore);
            //Reached number of episodes to calculate the average?
            if(scoreList.Count >= nEpisodes){
                List<float> avgScore = averageScore(scoreList);
                scoreList = new List<float>();

                print("Average score: " + avgScore[1] + " - n episodes: " + trainingEpisodeCounter);

                //Writing on json
                TrainData td = buildTrainData(avgScore);
                GenerateJson(td, "LogTraining/json/"+ currentPhase +".json");
                
                //If our score is good enough to pass to the next environment
                if (avgScore[1] >= curriculumEnvironments[environmentIndex].minScore){
                    print("Environment finished: " + envName + " - n episodes: " + trainingEpisodeCounter);
                    //writing the last one before changing environment
                    GenerateJson(td, "LogTraining/json/lastTrain.json");
                    //Loggin environment changes to help with plotting
                    logEnvironmentChange();

                    maxScore = -100;

                    //Check if there's another environment to train
                    if (environmentIndex + 1 < curriculumEnvironments.Length){
                        environmentIndex++;
                        trainingEpisodeCounter = 0;

                        CreateEnvironments();

                        environmentHandlersList = GetComponentsInChildren<EnvironmentHandler>();
                        foreach (EnvironmentHandler amb in environmentHandlersList) amb.curriculumHandler = this;
                    }
                    ///No more environments to train
                    else if (environmentIndex + 1 == curriculumEnvironments.Length){
                        //Check if we don't have a retraining, in this case we stop
#if UNITY_EDITOR
                        if (retrainEnvironments.Length <= 0 || !includeRetrain) UnityEditor.EditorApplication.isPlaying = false;
#endif


                        DestroyEnvironments();
                        CreateRetrainGrid(1f);

                        currentPhase = "retrain";
                    }
                }
            }
        }else{ 
            //Retraining phase
            //finding the index of the ending environment that called this method
            logRetrain();
            int currentIndex = -1;
            for (int i = 0; i < retrainEnvironments.Length; i++){
                if(retrainEnvironments[i].envGameObject.name == envName) currentIndex = i;
            }

            //Consolidation is equal to retraining with little differences
            //We need this bool to know when we switch to the consolidation phase
            bool consolidationSwitch = false;

            RetrainingData retrainingData = retrainingDataDict[envName];

            //counter for computing averages
            retrainingData.scoresList.Add(finalScore);
            retrainingData.avgCounter++;

            //counter for early fail
            retrainingData.totalCounter++;

            //early fail for retraining
            if(retrainingData.totalCounter>=retrainEnvironments[currentIndex].maxEpisodes){
#if UNITY_EDITOR

                UnityEditor.EditorApplication.isPlaying = false;
#endif

                Debug.Log("Early fail " + retrainingData.totalCounter + " episodes");
            }

            //compute the averages and log data
            if(retrainingData.avgCounter >= nEpisodes){
                List<float> avgScores = averageScore(retrainingData.scoresList);
                TrainData td = buildTrainData(avgScores,envName);

                print("Environment: " + retrainEnvironments[currentIndex].envGameObject.name + " average score: " + avgScores[1]);

                retrainTDsDict[envName].Add(td);

                //For now we print data even if not in order
                GenerateJson(td, "LogTraining/json/" + currentPhase + ".json");

                retrainingData.scoresList = new List<float>();
                retrainingData.avgCounter = 0;

                //Check if the score is good enough
                if(avgScores[1] > retrainEnvironments[currentIndex].minScore){
                    
                    Debug.Log("Early stop: " + avgScores[1]);
                    print("Environment finished: " + envName + " - n episodes: " + retrainingData.totalCounter);

                    DestroyEnvironments(retrainEnvironments[currentIndex].envGameObject.name);

                    //no more envs so stop the scene
                    if(this.transform.childCount==0){
                        Debug.Log("No more environments, stop.");

                        //Checking if we have to do the consolidation run
                        if(includeConsolidation){
                            CreateRetrainGrid(consolidationPercentage);
                            includeConsolidation = false;

                            Debug.Log("Consolidation run");
                            consolidationSwitch = true;
                            currentPhase = "consolidation";
                        }

#if UNITY_EDITOR

                        else { UnityEditor.EditorApplication.isPlaying = false; }
#endif

                    }
                }
            }

            //When switching to consolidation run we don't have to update the dictionary
            if(!consolidationSwitch) retrainingDataDict[envName] = retrainingData;
            else consolidationSwitch = false;
        }
    }

    //Create the environments grid
    [ContextMenu("Create environments")]
    void CreateEnvironments(){
        DestroyEnvironments();
        for (int i = 0; i < maxI; i++){
            for (int j = 0; j < maxJ; j++){
                Instantiate(curriculumEnvironments[environmentIndex].envGameObject, new Vector3(i * 103, 0, j * 103), Quaternion.identity, transform);
            }
        }
    }

    //Destroy the environments
    //If environmentName is not specified it will destroy every environment on the grid
    [ContextMenu("Destroy environments")]
    void DestroyEnvironments(string environmentName=""){
        List<EnvironmentHandler> childrens = new List<EnvironmentHandler>();

        //If we want to delete only certain environments we store them in a list
        if(environmentName != ""){
            environmentName = environmentName + "(Clone)";

            foreach(EnvironmentHandler e in GetComponentsInChildren<EnvironmentHandler>()){
                if(e.gameObject.name == environmentName) childrens.Add(e);
            }
        }

        //Destroy the agent first
        foreach (RLAgentScript ag in GetComponentsInChildren<RLAgentScript>())
        {
            if (environmentName == "") DestroyImmediate(ag.gameObject);
            else{
                foreach(EnvironmentHandler e in childrens){
                    if(ag != null && ag.transform.IsChildOf(e.transform)){
                        DestroyImmediate(ag.gameObject);
                    } 
                }
            }
        }

        //Destroy the whole environment
        int childs = transform.childCount;
        for (int i = childs - 1; i >= 0; i--)
        {
            if(environmentName == "") DestroyImmediate(transform.GetChild(i).gameObject);
            else if(transform.GetChild(i).gameObject.name == environmentName){
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        if(environmentName != "") Debug.Log("Destroyed env: " + environmentName);

    }

    [ContextMenu("Take screenshot")]
    public void Screenshot(){
        ///Take a screenshot from tha main camera    
        string dir = @"Foto";
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
        ScreenCapture.CaptureScreenshot($"Foto/Env_{curriculumEnvironments[environmentIndex].envGameObject.name}.png");
        if (environmentIndex < curriculumEnvironments.Length - 1) environmentIndex++;
    }

    [ContextMenu("Screenshot all")]
    public void ScreenshotAll(){
        //Take a screenshot for every environment in the grid
        string dir = @"Foto";
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
        if (Application.isPlaying) StartCoroutine(MyCoroutine());
    }

    //Used to take a screenshot of every env in the grid
    IEnumerator MyCoroutine()
    {
        int indiceIniziale = environmentIndex;

        for (int i = 0; i < curriculumEnvironments.Length; i++)
        {
            environmentIndex = i;
            //print(Time.time);
            CreateEnvironments();
            yield return new WaitForSeconds(1f);
            yield return new WaitForSecondsRealtime(0.1f);
            ScreenCapture.CaptureScreenshot($"Foto/{curriculumEnvironments[environmentIndex].envGameObject.name}.png");
            yield return new WaitForSeconds(1f);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        environmentIndex = indiceIniziale;
    }

    //Generate json log regarding training data
    private void GenerateJson(object obj, string path)
    {
        string dir = "LogTraining/json";

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(obj);
        using var tw = new StreamWriter(path, true);
        tw.WriteLine(json + ",");
        tw.Close();
    }


}
