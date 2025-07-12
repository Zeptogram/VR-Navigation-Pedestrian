using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RLAgentScript : Agent
{
    public float score;
    [Tooltip("Agents viewing angle")]
    [Range(0, 90)] public int viewingAngle;

    //proxemic distance   
    [Tooltip("WireArc distance of proxemic")]
    public Vector3 wireArcDistances = new Vector3(1.4f, 1, 0.6f);

    [Tooltip("Number of right and left rays from the central one for the proxemic")]
    public Vector3 wireArcRaysN = new Vector3(4, 5, 8);

    [NonSerialized] public Vector2 minMaxSpeed = new Vector2(0f, 1.7f);

    private Sensor[] sensors;

    //distance used to cap the observations distances
    //TODO: search for a better way to normalize the distance
    private readonly float maxDist = 14f;
    [NonSerialized] public EnvironmentHandler environmentHandler;

    [NonSerialized] public string finalTarget;

    [Serializable] public struct TargetReached{
        public GameObject target;
        public string entrance;
        public TargetReached(GameObject target, string entrance)
        {
            this.target = target;
            this.entrance = entrance;
        }
    }

    ///Set targets already taken manually (In case an agent is placed in a specific place maybe we need to set an already taken target)
    [Serializable]
    public struct TargPresiInit{
        public GameObject target;
        public string entrata;
        public TargPresiInit(GameObject target, string entrata)
        {
            this.target = target;
            this.entrata = entrata;
        }
    }
    [Header("Midtargets reached by the agent")]
    public List<TargetReached> reachedTargets = new List<TargetReached>();
    [Header("Midtargets reached by the agent to initialize")]
    public List<TargPresiInit> targetPresiInit = new List<TargPresiInit>();

    private float currentSpeed;

    //used the "my" word to differentiate from MaxStep that is a attribute of MLAgent.Agent 
    [NonSerialized] public int myMaxSteps;

    private int stepsLeft;

    [NonSerialized] public Vector3 startingPos;
    [NonSerialized] public Quaternion startingRot;

    private Rigidbody rigidBody;

    private Animator animator;

    private Sensor gizmosSensor;

    [Header("Final target")]
    public GameObject finalTargetGO;

    private Vector3 finalTargetPos;
    //used for the gaussian distribution
    private Vector2 speedMaxRange = new Vector2(1.3f, 1.7f);

    //initialize the agent every episode
    public void Init(){
        if (myMaxSteps == 0) Debug.LogError("MaxSteps value incorrect");

        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        finalTargetPos = finalTargetGO.transform.position;
        //Max steps for MLAgent (not used)
        MaxStep = 10000000;

        //add in the reachedTargets the targets already reached 
        reachedTargets = new List<TargetReached>();
        foreach (var t in targetPresiInit){
            reachedTargets.Add(new TargetReached(t.target, t.entrata));
        }

        sensors = GetComponents<Sensor>();

        //TODO; change names to english (WARNING: this can lead up to errors)
        if (transform.parent.name == "Sotto"){ //Bottom
            finalTarget = "TargetFine1";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.red;
        }else if (transform.parent.name == "Sopra"){ //Top
            finalTarget = "TargetFine2";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.cyan;
        }else if (transform.parent.name == "Destra"){ //Right
            finalTarget = "TargetFine3";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.green;
        }else if (transform.parent.name == "Sinistra"){ //Left
            finalTarget = "TargetFine4";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.yellow;
        }else
            Debug.LogError("Gli agenti dovrebbero essere in \"Sotto\", \"Sopra\", \"Destra\" o \"Sinistra\"");

        //after deciding what target is correct for the agent lets him see it using the RayLayeredMask
        //assigns sensors[0].RayLayeredMask to sensors[0].RayLayeredMask or(logic bitwise) 1 shifted by LayerMask.NameToLayer(finalTarget) positions
        sensors[0].rayLayeredMask |= 1 << LayerMask.NameToLayer(finalTarget);

        GetComponent<Rigidbody>().velocity = Vector3.zero;
        currentSpeed = 0;
        stepsLeft = myMaxSteps;
        minMaxSpeed.y = RandomGaussian(speedMaxRange.x, speedMaxRange.y);

        //Change brain if we are in testing with the brain selected by the user
        if(environmentHandler.curriculumHandler.testing){
            SetModel("Pedone",environmentHandler.curriculumHandler.brain);
        }
    }

    //handle exit from the targets
    private void OnTriggerExit(Collider other)
    {
        Vector3 currentPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 direction = other.transform.position - currentPosition;
        direction.y = 0;

        string entranceDirection = "";
        //compute the entranceDirection 
        if (Vector3.Dot(other.transform.forward, direction) > 0) entranceDirection = "Sotto";
        if (Vector3.Dot(other.transform.forward, direction) < 0) entranceDirection = "Sopra";
        if (entranceDirection == "") print("Entrata settata male, deve seguire la freccia blu!");

        GameObject reachedTarget = other.gameObject;

        int initialCount = reachedTargets.Count;

        //if we enter a target but go through it completely (we go back) we remove it (and brothers) from the reached targets list 
        foreach (Transform brother in reachedTarget.transform.parent){
            if (brother.CompareTag("Target") && reachedTargets.Any(myT => myT.target == brother.gameObject)){
                //use the entranceDirection to understand if the agent exited from the part he entered (in that case it's removed) 
                reachedTargets.Remove(new TargetReached(brother.gameObject, entranceDirection));
            }
        }

        if (initialCount > reachedTargets.Count){
            AddReward(-1f);
        }
    }


    //handle entrance in the targets
    private void OnTriggerEnter(Collider other){
        
        Vector3 currentPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 closestPointToAgent = other.ClosestPoint(transform.position);
        closestPointToAgent.y = 0;
        
        if (Vector3.Distance(currentPosition, closestPointToAgent) <= 0.01f){
            Vector3 currentDirection = other.transform.position - currentPosition;
            currentDirection.y = 0;
            //compute the entranceDirection 
            string entranceDirection = "";
            if (Vector3.Dot(other.transform.forward, currentDirection) > 0) entranceDirection = "Sotto";
            if (Vector3.Dot(other.transform.forward, currentDirection) < 0) entranceDirection = "Sopra";
            if (entranceDirection == "") print("Entrata settata male, deve seguire la freccia blu!");

            GameObject reachedTarget = other.gameObject;

            if (reachedTarget.CompareTag("Target")){
                //final target
                if (reachedTarget.name == finalTarget){
                    AddReward(6f);
                    Finished();
                }else{
                    //reached mid target, if this wasn't already reached add him and his brothers and the entranceDirection
                    if (!reachedTargets.Any(myT => myT.target == reachedTarget)){
                        AddReward(0.5f);
                        foreach (Transform brother in reachedTarget.transform.parent){
                            if (brother.CompareTag("Target") && !reachedTargets.Any(myT => myT.target == brother.gameObject)){
                                reachedTargets.Add(new TargetReached(brother.gameObject, entranceDirection));
                            }
                        }
                    }
                    //reached mid target but already reached, so lose points and remove from reached
                    else{
                        AddReward(-0.2f);
                        foreach (Transform brother in reachedTarget.transform.parent){
                            if (brother.CompareTag("Target") && reachedTargets.Any(myT => myT.target == brother.gameObject)){
                                reachedTargets.Remove(new TargetReached(brother.gameObject, entranceDirection));
                            }
                        }
                    }
                }
            }else{ Debug.LogError("Unknown target: " + reachedTarget.tag); }
        }
    }


    public override void OnEpisodeBegin(){
        Init();
    }

    private void OnDrawGizmos(){
        
        if (!gizmosSensor) gizmosSensor = GetComponents<Sensor>()[0];
        else{
            DrawWireArc(transform.position, transform.forward, GetAngle(wireArcRaysN.x), wireArcDistances.x);
            DrawWireArc(transform.position, transform.forward, GetAngle(wireArcRaysN.y), wireArcDistances.y);
            DrawWireArc(transform.position, transform.forward, GetAngle(wireArcRaysN.z), wireArcDistances.z);
        }
    }

    //pass the observation to the NN
    public override void CollectObservations(VectorSensor vectorSensor){
        float normalizedSpeed = (currentSpeed - minMaxSpeed.x) / (minMaxSpeed.y - minMaxSpeed.x);

        vectorSensor.AddObservation(normalizedSpeed);

        foreach (Sensor s in sensors){
            if (s.myName == "Muri+Target") WallsAndTargets(vectorSensor, s);
            else if (s.myName == "Muri+Agenti") WallAndAgents(vectorSensor, s);
            else Debug.LogError("Sensore non riconosciuto: " + s.myName);
        }
    }

    private void WallsAndTargets(VectorSensor vectorSensor, Sensor sensor){
        //in onehotencoding -1 corresponds to empty observation
        List<Sensor.ObjectHit> observations = sensor.GetRaysInfo();
        foreach (var observation in observations){
            float finalTargetDistanceFromMidTarget = 1;
            int hitObjectIndex = -1; //0 if viewing a wall, 1 if viewing a target, 2 viewing a already catched target, -1 don't care
            float normalizedDistance = 1;
            if (observation.gameObject == null){
                Debug.LogError("Ray layer mask error, check it");
                normalizedDistance = 1;
                hitObjectIndex = 0;
            }else{
                //Hit object is a wall
                if (observation.gameObject.CompareTag("Muro")){
                    hitObjectIndex = 0;
                }//hit object is a target (not yet passed middle or final)
                else if (observation.gameObject.CompareTag("Target") && !reachedTargets.Any(myT => myT.target == observation.gameObject)){
                    if (observation.gameObject.gameObject == finalTargetGO){
                        hitObjectIndex = 1;
                    }else{
                        //mid target not yet catched
                        hitObjectIndex = 1;
                        Vector3 hitObjPos = observation.gameObject.transform.position;
                        //TODO check for a better way to normalized distance
                        // normalized distance between final target and midtarget just viewed
                        finalTargetDistanceFromMidTarget = Vector3.Distance(finalTargetPos, hitObjPos) / 20f;
                    }
                }
                //already catched target                
                else if (observation.gameObject.CompareTag("Target") && reachedTargets.Any(myT => myT.target == observation.gameObject)) { hitObjectIndex = 2; }

                else { Debug.LogError("Error on the Tag: " + observation.gameObject.tag); }

                //compute normalized distance 
                normalizedDistance = observation.distance / maxDist;
                if (normalizedDistance > 1) normalizedDistance = 1;
            }

            if (hitObjectIndex > 2 || hitObjectIndex < 0) Debug.LogError("Target not recognized: " + observation.gameObject.tag + " of: " + observation.gameObject.name + hitObjectIndex);
            if (normalizedDistance > 1 || normalizedDistance < 0) Debug.LogError("Error on distance : " + normalizedDistance + " with: " + observation.gameObject.name);

            vectorSensor.AddObservation(normalizedDistance); 
            vectorSensor.AddOneHotObservation(hitObjectIndex, 3);   
        }
    }

    private void WallAndAgents(VectorSensor sensor, Sensor s){

        List<Sensor.ObjectHit> observations = s.GetRaysInfo();
        foreach (var observation in observations){
            int tagIndex = -1; //0 for walls/null, 1 for agent
            float normalizedDirection = -1; //0 if viewing a wall, 1 if viewing a target, 2 viewing a already catched target, -1 don't care
            float normalizedSpeed = -1; // 0 viewing final target, 1/2/3 for viewing midtarget, -1 not viewing a target (-> don't care)
            float normalizedDistance = 1;

            if (observation.gameObject == null){
                Debug.LogError("Ray layer mask error, check it");
                tagIndex = 0;
                normalizedDistance = 1;
                normalizedDirection = 0;
                normalizedSpeed = 0;
            }else{
                if (observation.gameObject.CompareTag("Muro")){//wall
                    tagIndex = 0;
                    normalizedDirection = 0;
                    normalizedSpeed = 0;
                    normalizedDistance = observation.distance / maxDist;
                }else if (observation.gameObject.CompareTag("Agente")){//agents
                    tagIndex = 1;
                    //fictional agents (static)/we are in heuristic mode so fictional agent it's standing still and with random direction (we don't care about him)
                    if (!observation.gameObject.GetComponent<RLAgentScript>() ||
                            observation.gameObject.GetComponent<BehaviorParameters>().BehaviorType.ToString() == "HeuristicOnly"){
                        normalizedDirection = UnityEngine.Random.Range(-1f, 1f);
                        normalizedDirection = (float)Math.Round(normalizedDirection, 1);
                        normalizedSpeed = 0;
                    }else{//normal agent
                        Vector3 forward = transform.forward;
                        Vector3 otherForward = observation.gameObject.transform.forward;
                        //direction compared to the current agent
                        normalizedDirection = Math.Sign(otherForward.x) * (Vector3.Angle(forward, otherForward) / 180f);
                        normalizedDirection = (float)Math.Round(normalizedDirection, 1);
                        //speed
                        normalizedSpeed = observation.gameObject.GetComponent<RLAgentScript>().currentSpeed;
                        normalizedSpeed = (normalizedSpeed - minMaxSpeed.x) / (speedMaxRange.y - minMaxSpeed.x);
                    }
                    normalizedDistance = observation.distance / 6;
                }else { Debug.LogError("Tag error: " + observation.gameObject.tag); }

                if (normalizedDistance > 1) normalizedDistance = 1;
            }

            if (tagIndex > 1 || tagIndex < 0) Debug.LogError("Tag error, not recognized: " + observation.gameObject.tag + " of: " + observation.gameObject.name);
            if (normalizedDistance > 1 || normalizedDistance < 0) Debug.LogError("Distance error : " + normalizedDistance + " with: " + observation.gameObject.name);
            if (normalizedDirection > 1 || normalizedDirection < -1) Debug.LogError("Direction error : " + normalizedDirection + " with: " + observation.gameObject.name);
            if (normalizedSpeed > 1 || normalizedSpeed < 0) Debug.LogError("Speed error : " + normalizedSpeed + " with: " + observation.gameObject.name);

            sensor.AddObservation(normalizedDistance);
            sensor.AddObservation(tagIndex);
            sensor.AddObservation(normalizedDirection);
            sensor.AddObservation(normalizedSpeed);
        }
    }

    //public override void OnActionReceived(float[] vectorAction)
    //{
    //    stepsLeft--;
    //    //if outside [-1,1] clamp to it
    //    var actionSpeed = Mathf.Clamp(vectorAction[0], -1f, 1f);
    //    var actionAngle = Mathf.Clamp(vectorAction[1], -1f, 1f);

    //    AngleChange(actionAngle);

    //    if (transform.parent.parent.name != "Osserva(Clone)") SpeedChange(actionSpeed);
    //    ComputeRewards();

    //    if (stepsLeft <= 0) Finished();
    //}

    public void ComputeRewards(){
        
        // when just started sometimes rays return fewer infos than real ones, so we use this function to avoid errors
        if (sensors[0].GetRaysInfo().Count < wireArcRaysN.z) { print("Starting"); return; }

        //check if there are more rays than expected
        if (sensors[0].GetRaysInfo().Count < 2 * (int)wireArcRaysN[2] + 1) { print("Too many rays in wireArcRaysN!"); }

        //flags used to punish bad behaviour
        bool catchedWall = false;
        bool catchedAgent1 = false; //nearest arc
        bool catchedAgent2 = false; //middle arc
        bool catchedAgent3 = false; //furthest arc
        bool notWatchingTarget = false;

        List<Sensor.ObjectHit> observationsWallsAndTargets = sensors[0].GetRaysInfo();
        List<Sensor.ObjectHit> observationsWallsAndAgents = sensors[1].GetRaysInfo();
        List<Sensor.ObjectHit> observations = new List<Sensor.ObjectHit>();

        //rays checks
        if(!observationsWallsAndTargets.Any(hit => hit.gameObject.tag == "Target")) notWatchingTarget = true;

        //proxemic checks
        // nearest arc before the agent, check if collide with walls and agent
        observations.AddRange(observationsWallsAndTargets.GetRange(0, 2 * (int)wireArcRaysN[2] + 1));
        observations.AddRange(observationsWallsAndAgents.GetRange(0, 2 * (int)wireArcRaysN[2] + 1));
        foreach (var observation in observations){
            if (observation.gameObject.CompareTag("Muro") && observation.distance <= wireArcDistances[2]){
                catchedWall = true;
            }
            if (observation.gameObject.CompareTag("Agente") && observation.distance <= wireArcDistances[2]){
                catchedAgent1 = true;
            }
        }

        //middle arc, check if collide with agent
        observations = new List<Sensor.ObjectHit>();
        observations.AddRange(observationsWallsAndTargets.GetRange(0, 2 * (int)wireArcRaysN[1] + 1));
        observations.AddRange(observationsWallsAndAgents.GetRange(0, 2 * (int)wireArcRaysN[1] + 1));
        foreach (var observation in observations){
            if (observation.gameObject.CompareTag("Agente") && observation.distance <= wireArcDistances[1]){
                catchedAgent2 = true;
            }
        }

        //furthest arc check if collide with agent
        observations = new List<Sensor.ObjectHit>();
        observations.AddRange(observationsWallsAndTargets.GetRange(0, 2 * (int)wireArcRaysN[0] + 1));
        observations.AddRange(observationsWallsAndAgents.GetRange(0, 2 * (int)wireArcRaysN[0] + 1));
        foreach (var observation in observations){
            if (observation.gameObject.CompareTag("Agente") && observation.distance <= wireArcDistances[0]){
                catchedAgent3 = true;
            }
        }

        //assign rewards
        if (notWatchingTarget) AddReward(-0.5f);
        if (catchedWall) AddReward(-0.5f);
        if (catchedAgent1) AddReward(-0.5f);
        else if (catchedAgent2) AddReward(-0.005f);
        else if (catchedAgent3) AddReward(-0.001f);

        if (stepsLeft <= 0) AddReward(-6.0f);

        //for every step passed
        AddReward(-0.0001f);
    }

    private void SpeedChange(float deltaSpeed){
        currentSpeed += (minMaxSpeed.y * deltaSpeed / 2f);
        currentSpeed = Mathf.Clamp(currentSpeed, minMaxSpeed.x, minMaxSpeed.y);
        rigidBody.velocity = transform.forward * currentSpeed;
        animator.SetFloat("Speed", currentSpeed);
    }

    private void AngleChange(float deltaAngle){
        float angle = (deltaAngle * 25); //da aumentare?
        float newAngle = Mathf.Round(angle + transform.rotation.eulerAngles.y);
        transform.eulerAngles = new Vector3(0, newAngle, 0);
    }

    public void Finished(){
        float score = GetCumulativeReward();
        //log agent data 
        GetComponent<PythonAgent>().WriteStats(score, "RL");
        gameObject.SetActive(false);
        environmentHandler.NotifyEnd(score, myMaxSteps - stepsLeft, this.startingPos, this.startingRot, this.finalTargetGO, this.targetPresiInit);
    }

    //public override void Heuristic(float[] actionsOut){
    //    //move agent by keyboard
    //    var continuousActionsOut = actionsOut;
    //    continuousActionsOut[0] = Input.GetAxis("Vertical");
    //    continuousActionsOut[1] = Input.GetAxis("Horizontal");
    //}

    private void DrawWireArc(Vector3 position, Vector3 direction, float anglesRange, float radius, float maxSteps = 50){
        var srcAngles = GetAnglesFromDir(position, direction);
        var initialPos = position;
        initialPos.y = 1;
        var posA = initialPos;
        var stepAngles = anglesRange / maxSteps;
        var angle = srcAngles - anglesRange / 2;

        //link the dots
        for (var i = 0; i <= maxSteps; i++){
            var rad = Mathf.Deg2Rad * angle;
            var posB = initialPos;
            posB += new Vector3(radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
            Gizmos.color = Color.white;
            Gizmos.DrawLine(posA, posB);

            angle += stepAngles;
            posA = posB;
        }
        //Link last dot to first
        Gizmos.DrawLine(posA, initialPos);
    }

    static float GetAnglesFromDir(Vector3 position, Vector3 dir){
        var forwardLimitPos = position + dir;
        var srcAngles = Mathf.Rad2Deg * Mathf.Atan2(forwardLimitPos.z - position.z, forwardLimitPos.x - position.x);
        return srcAngles;
    }

    private float GetAngle(float rays){
        ///Ritorna l'angolo dal numero di raggi
        Sensor s = gizmosSensor;
        float currentAngle = 0;
        int raysN = -1;

        for (float i = s.delta; currentAngle <= viewingAngle; i += s.delta){
            raysN++;
            if (raysN == rays) return 2 * currentAngle;
            if (currentAngle != viewingAngle && currentAngle + i > viewingAngle) currentAngle = viewingAngle;
            else currentAngle += i;
        }
        return 2 * viewingAngle;
    }

    //returns a number derived by a gaussian
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f){
        float u, v, S;
        do{
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        /// Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        /// Normal Distribution centered between the min and max value
        /// and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }
    private void FixedUpdate()
    {
        score = GetCumulativeReward();
    }

}
