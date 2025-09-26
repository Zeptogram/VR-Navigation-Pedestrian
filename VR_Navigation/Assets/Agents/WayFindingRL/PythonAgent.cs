using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

//agent that handle the agent stats gathering and logging  
public class PythonAgent : MonoBehaviour{
    private float lastTimer = 0f;
    private float desiredSpeed;
    private List<float> avgSpeed = new List<float>();
    private List<float> avgDensity = new List<float>();
    private List<float> timestamps = new List<float>();
    private float startTimestamp = 0;
    private List<Vector3> positions = new List<Vector3>();
    private string colorIndex;
    private Vector3 lastPosition;
    RLAgentScript[] otherAgents;
    EnvironmentHandler environmentHandler;
    private int id;
    private RLAgentScript rlAgent;
    private String runID;
    private String timeNow;
    private bool testing;


    private void Start(){
        timeNow = DateTime.Now.ToString("yyyyMMddhmmss"); 
        runID = transform.GetComponentInParent<CurriculumHandler>().runID;
        testing = transform.GetComponentInParent<CurriculumHandler>().testing;
        id = transform.GetComponentInParent<CurriculumHandler>().agentIDcounter++;
        Debug.Log("Started agent id: "+ id);
        otherAgents = transform.parent.parent.GetComponentsInChildren<RLAgentScript>();
        environmentHandler = transform.parent.parent.GetComponent<EnvironmentHandler>();
        lastPosition = transform.localPosition;
        startTimestamp = environmentHandler.currentSteps;
        //method called every 5 steps
        EnvironmentHandler.RegularRefresh += GatherStats;

        rlAgent = GetComponent<RLAgentScript>();

        //TODO Translate names in english (WARNING: this will lead up to errors)
        //handle the color index 
        if (transform.parent.name == "Sotto") colorIndex = "red";
        else if (transform.parent.name == "Sopra") colorIndex = "blue";
        else if (transform.parent.name == "Destra") colorIndex = "green";
        else if (transform.parent.name == "Sinistra") colorIndex = "yellow";
        else Debug.LogError("Gli agenti dovrebbero essere in \"Sotto\" o \"Sopra\"");
    }

    //calculate stats for the agent and append them to the stats lists
    void GatherStats(){
        if (this != null && this.gameObject.activeSelf){
            if (rlAgent != null){
                desiredSpeed = (float)Math.Round(rlAgent.minMaxSpeed.y, 3);
            }else{
                desiredSpeed = 1.7f;
            }
            float perceivedDensity = 1;
            foreach (RLAgentScript otherAgent in otherAgents){
                if (otherAgent.gameObject != this.gameObject && otherAgent.gameObject.activeSelf){
                    //if the two agents collide/are really close (?)
                    Vector3 vectorToCollider = (otherAgent.transform.position - transform.position).normalized;
                    if (Vector3.Dot(vectorToCollider, transform.forward) > 0) perceivedDensity++;
                }
            }
            float currentSpeed = (float)Math.Round(Vector3.Distance(transform.localPosition, lastPosition) / (Time.time - lastTimer), 3);
            if (currentSpeed > desiredSpeed) currentSpeed = desiredSpeed;
            avgSpeed.Add(currentSpeed);
            avgDensity.Add(perceivedDensity);
            timestamps.Add(environmentHandler.currentSteps);
            positions.Add(new Vector3((float)Math.Round(transform.localPosition.x, 3), 0, (float)Math.Round(transform.localPosition.z, 3)));
            lastTimer = Time.time;
            lastPosition = transform.localPosition;
        }
    }

    //when the agent is terminated log the stats and reset the stats lists
    public void WriteStats(float score, string type,bool finished = true){
        
        if (environmentHandler != null){
            if (testing){
                StreamWriter writer;
                var fileName = "LogTraining/"+runID +"/Ambienti/" + transform.parent.parent.name + runID + ".txt";

                writer = new StreamWriter(fileName, true);
                for (int i = 0; i < avgSpeed.Count; i++){
                    float timeToFinish = -1;

                    if(finished) timeToFinish = environmentHandler.currentSteps - startTimestamp;

                    writer.WriteLine(positions[i].x + ";" + positions[i].z + ";" + avgSpeed[i] + ";" + colorIndex + ";" + id + ";" + desiredSpeed + ";"+ timestamps[i] + ";" + timeToFinish + ";" + type);
                }
                writer.Close();
            }
            avgSpeed = new List<float>();
            avgDensity = new List<float>();
            positions = new List<Vector3>();
            startTimestamp = environmentHandler.currentSteps;
        }
    }
}
