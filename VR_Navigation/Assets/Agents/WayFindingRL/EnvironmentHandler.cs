using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
public class EnvironmentHandler : MonoBehaviour
{
    public Text canvas;
    //To plot you need a 1x1 grid, otherwise plots gonna be a mess
    // [Tooltip("Check true if you need to create plot via the python scripts")]
    // [Header("Create txt file for plotting in /LogTraining/AgentStats")]
    // public bool pythonPlot;

    [Tooltip("Check true if you need to compare RL and NavMesh")]
    [Header("RL and NavMesh comparison")]
    public bool navMeshComparison;
    [Header("Maximum number of steps for the environment to reach convergence in training")]
    [SerializeField] int maxSteps;
    [Tooltip("Check if you want to make the env toroidal (each agent will respawn)")]
    public bool toroidal;
    [Tooltip("Number of steps before ending the env")]
    public int toroidalSteps;
    [Tooltip("Agent prefab to repawn")]
    public GameObject agentPrefab;

    ///Struct for the object switching 
    [Serializable]
    public struct SwitchObject{
        public RLAgentScript agent;
        public Transform target;
        public Vector3 pos1;
        public Vector3 pos2;
    }
    [Space(10)]
    public SwitchObject[] objToSwitch;

    ///Struct for objects translation
    [Serializable]
    public struct ObjOffset{
        public GameObject obj;
        public Vector2 xOffset;
        public Vector2 zOffset;
        public bool randomPosition;
        public bool randomRotation;
    }
    public ObjOffset[] ObjToTranslate;

    [Header("List of object to activate at the beginning of the env")]
    public GameObject[] activationList1;
    public GameObject[] activationList2;

    //number of agents that have finished the env task
    private int numAgentsDone = 0;
    //sum of the score and steps of all the agents
    private float totalScore = 0;
    public float totalSteps = 0;
    public float currentSteps = 0; //Keep track of the total steps since the env has started
    //the agents of the env 
    private List<RLAgentScript> agents;
    [SerializeField] public CurriculumHandler curriculumHandler;


    //Event to refresh regularly, too complicated to comment here
    //Check how unity handles events for a satisfing explanation
    public delegate void ClickAction();
    public static event ClickAction RegularRefresh;
    private float lastTimer = 0f; //Used by RegularRefresh to check if we need to refresh
    private readonly float sendTimer = 0.1f; //Similar as lastTimer

    [Header("Number of Objects in the first array")]
    public int activeNumber1 = 1;
    [Header("Number of Objects in the second array")]
    public int activeNumber2 = 1;

    //average score at the end of the episode
    private float avgEndingScore;

    void Start(){
        canvas = GetComponentInChildren<Text>();

        currentSteps = 0;

        //Resetting log file
        //if(pythonPlot){
        //    string path = "LogTraining/AgentStats/" + this.name + ".txt";
        //    using var tw = new StreamWriter(path, false);
        //    tw.Write("");
        //    tw.Close();
        //}
        ScreenCapture.CaptureScreenshot("LogTraining/ScreenEnv/" + this.name + ".png");
        ActivateObjs(activeNumber1, this.activationList1);
        ActivateObjs(activeNumber2, this.activationList2);

        Invoke(nameof(InitializeAgents), 0.5f);
    }

    //activate random objs given number of active objs and list of possible objs in a specific section of the env 
    private void ActivateObjs(int activeNumber, GameObject[] activationArray){
        if (activationArray.Length < 1) return;
        
        if (activeNumber > activationArray.Length) Debug.LogError("Number of active objs too high");

        //deactivate all objs (just to be sure)
        for (int i = 0; i < activationArray.Length; i++){
            activationArray[i].SetActive(false);
        }
        //activate random activeNumber of objs
        for (int j = 0; j < activeNumber; j++){
            int random = UnityEngine.Random.Range(0, activationArray.Length);
            while (activationArray[random].activeSelf){
                random = UnityEngine.Random.Range(0, activationArray.Length);
            }
            activationArray[random].SetActive(true);
        }
    }

    //call regularRefresh if a certain amount of time is passed (only used if pythonPlot is true)
    private void FixedUpdate(){
    float scoreShown = 0;

    agents = GetComponentsInChildren<RLAgentScript>(includeInactive: true).ToList();
        foreach(RLAgentScript a in agents)
        {
            scoreShown += a.score;

        }
        canvas.text = "Score:" + scoreShown;
        
        //print(currentSteps);
        currentSteps = currentSteps + 1;
        if ( Time.time - lastTimer >= sendTimer){
            if (RegularRefresh != null) RegularRefresh(); 
            lastTimer = Time.time;
        }
#if UNITY_EDITOR
        //Stop if the env is toroidal and reach his maximum steps
        if(toroidal && currentSteps >= toroidalSteps){
            UnityEditor.EditorApplication.isPlaying = false;


        }
# endif
    }

    //Called when the application is stopped
    private void OnApplicationQuit() {
    if (curriculumHandler == null) {
        Debug.LogWarning("curriculumHandler is NULL during OnApplicationQuit. Skipping...");
        return;
    }

    if (curriculumHandler.testing) {
        foreach (RLAgentScript agent in agents) {
            if (agent.isActiveAndEnabled) {
                float score = agent.GetCumulativeReward();
                agent.GetComponent<PythonAgent>().WriteStats(score, "RL", false);
            }
        }
    }
}


    private void InitializeAgents(){
        agents = GetComponentsInChildren<RLAgentScript>(includeInactive: true).ToList();
        foreach (RLAgentScript a in agents){
            a.startingPos = a.transform.position;
            a.startingRot = a.transform.rotation;

            a.myMaxSteps = maxSteps;
            a.environmentHandler = this;

            a.gameObject.SetActive(true);
        }
    }

    //update stats when an agent completes the env
    public void NotifyEnd(float agentScore, float stepsPassed, Vector3 startPosition, Quaternion startRotation, GameObject finalTargetGO, List<RLAgentScript.TargPresiInit> targetPresiInit){
        numAgentsDone++;
        totalScore += agentScore;
        totalSteps += stepsPassed;

        //If toroidal we respawn another agent
        if(toroidal){
            GameObject newAgent = Instantiate(this.agentPrefab, startPosition, startRotation, GameObject.Find("Sotto").transform);
            RLAgentScript newHandler = newAgent.GetComponent<RLAgentScript>();
            newHandler.targetPresiInit = targetPresiInit;
            newHandler.myMaxSteps = maxSteps;
            newHandler.startingPos = startPosition;
            newHandler.startingRot = startRotation;
            newHandler.environmentHandler = this;
            newHandler.finalTargetGO = finalTargetGO;
            newAgent.SetActive(true);
            agents.Add(newHandler);
        }
        //If all the egents completed the env call the episode end
        if (numAgentsDone >= agents.Count && !toroidal) Invoke(nameof(EndEpisode), 0.05f);
    }
    //called when all agents have finished 
    void EndEpisode(){
        avgEndingScore = totalScore / agents.Count;
        float avgStepsToEnd = totalSteps / agents.Count;

        numAgentsDone = 0;
        totalScore = 0;
        totalSteps = 0;

        //activate the objs
        ActivateObjs(activeNumber1, this.activationList1);
        ActivateObjs(activeNumber2, this.activationList2);

        foreach (RLAgentScript a in agents){
            a.transform.position = a.startingPos;
            a.transform.rotation = a.startingRot;
        }

        //move objs
        foreach (ObjOffset o in ObjToTranslate)
        {
            if(o.randomPosition){
                float posX = UnityEngine.Random.Range(o.xOffset.x, o.xOffset.y);
                float posZ = UnityEngine.Random.Range(o.zOffset.x, o.zOffset.y);

                o.obj.transform.localPosition = new Vector3(posX, 0, posZ);
            }

            if(o.randomRotation){
                int rotation = UnityEngine.Random.Range(0,360);
                o.obj.transform.Rotate(rotation,0,0);
            } 
        }

        //switch objs
        foreach (SwitchObject ogg in objToSwitch){
            int flag = UnityEngine.Random.Range(0, 2);
            if (flag == 0){
                ogg.target.localPosition = ogg.pos1;
                ogg.agent.transform.localPosition = ogg.pos2;
            }else{
                ogg.target.localPosition = ogg.pos2;
                ogg.agent.transform.localPosition = ogg.pos1;
            }
        }

        //activate agents and call the end of the episode (the ml-agents one)
        if (!navMeshComparison){
            foreach (RLAgentScript a in agents){
                a.gameObject.SetActive(true);
                a.EndEpisode(); //the ml-agents method
            }
        }

        string envName = this.name.Replace("(Clone)","");

        //Call environment terminated
        if(!toroidal){
            curriculumHandler.EnvironmentTerminated(avgEndingScore,envName);
        }
    }
}
