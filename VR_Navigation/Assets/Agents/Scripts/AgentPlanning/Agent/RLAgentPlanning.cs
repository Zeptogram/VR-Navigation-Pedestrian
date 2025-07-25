using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;


[RequireComponent(typeof(ObjectiveInteractionHandler))]
[RequireComponent(typeof(ObjectiveObserver))]
[RequireComponent(typeof(RLPlanningAnimationManager))]

/**
 * \class RLAgentPlanning
 * \brief Represents the RL agent. Inherits from the Agent class of ML-Agents.
 */
public class RLAgentPlanning : Agent, IAgentRL
{
    // Artifact lists
    [SerializeField] private List<Artifact> assignedArtifacts = new List<Artifact>();
    
    public TotemArtifact totemArtifact;
    public MonitorArtifact monitorArtifact;

    public int numberOfCrossings = 0;

    // Order tracking for this agent
    private int? myOrderId = null;

    public int? MyOrderId => myOrderId; // For obj handler

    private bool hasPlacedOrder = false;



    private bool run = true;

    /// <summary>
    /// Reference to constants.
    /// </summary>
    public IAgentConstants constants { get; private set; }

    /// <summary>
    /// The group to which the agent belongs.
    /// </summary>
    public Group group;

    /// <summary>
    /// Minimum and maximum speed for the agent.
    /// </summary>
    private Vector2 minMaxSpeed;

    /// <summary>
    /// Current speed of the agent.
    /// </summary>
    private float currentSpeed;
    /// <summary>
    /// Current angle of the agent.
    /// </summary>
    private float newAngle;

    private bool fleeing = false;

    /// <summary>
    /// Initial position of the agent.
    /// </summary>
    [NonSerialized] public Vector3 startPosition;
    /// <summary>
    /// Initial rotation of the agent.
    /// </summary>
    [NonSerialized] public Quaternion startRotation;

    /// <summary>
    /// Rigidbody component reference.
    /// </summary>
    private Rigidbody rigidBody;

    /// <summary>
    /// Animator component reference.
    /// </summary>
    private Animator animator;
    private RLPlanningAnimationManager animationManager;

    /// <summary>
    /// Range for randomizing speed.
    /// </summary>
    private Vector2 speedMaxRange;

    /// <summary>
    /// List of targets taken by the agent.
    /// </summary>
    [NonSerialized] public List<GameObject> targetsTaken = new List<GameObject>();

    /// <summary>
    /// Reference to the agent's sensors manager.
    /// </summary>
    private AgentPlanningSensorsManager agentSensorsManager;
    /// <summary>
    /// Reference to the agent's gizmos drawer.
    /// </summary>
    private AgentPlanningGizmosDrawer agentGizmosDrawer;
    /// <summary>
    /// Reference to the agent's observer.
    /// </summary>
    private AgentPlanningObserver agentObserver;
    /// <summary>
    /// Reference to the objective observer.
    /// </summary>
    private ObjectiveObserver objectiveObserver;

    /// <summary>
    /// Observations for walls and targets.
    /// </summary>
    private List<float> wallsAndTargetsObservations;
    /// <summary>
    /// Observations for walls and agents.
    /// </summary>
    private List<float> wallsAndAgentsObservations;
    /// <summary>
    /// Observations for walls and objectives.
    /// </summary>
    private List<float> wallsAndObjectivesObservations;

    /// <summary>
    /// List of walls and agents detected.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndAgents = new List<(GizmosTagPlanning, Vector3)>();
    /// <summary>
    /// List of walls and targets detected.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndTargets = new List<(GizmosTagPlanning, Vector3)>();
    /// <summary>
    /// List of walls and objectives detected.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndObjectives = new List<(GizmosTagPlanning, Vector3)>();

    /// <summary>
    /// Event triggered when the agent is terminated.
    /// </summary>
    public event Action<float, EnvironmentPlanning> agentTerminated;
    /// <summary>
    /// Event triggered to reset the agent.
    /// </summary>
    public event Action<RLAgentPlanning> resetAgent;

    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    private string uniqueID;
    /// <summary>
    /// Number of steps in the environment.
    /// </summary>
    /// TODO: CHECK IF NECESSARY HERE
    //[NonSerialized] public int envStep = -1;
    public int envStep;
    /// <summary>
    /// Steps left in the episode.
    /// </summary>
    private int stepLeft;

    /// <summary>
    /// Number of iterations performed by the agent.
    /// </summary>
    private int numberOfIteraction;
    /// <summary>
    /// Cumulative reward collected by the agent.
    /// </summary>
    private float cumulativeReward;
    /// <summary>
    /// If true, the agent's position is locked.
    /// </summary>
    public bool lockPosition;
    /// <summary>
    /// Initial time of the episode.
    /// </summary>
    private int tempoIniziale;

    /// <summary>
    /// Reference to the environment.
    /// </summary>
    [NonSerialized] public EnvironmentPlanning env;

    /// <summary>
    /// Value used for entry direction calculation.
    /// </summary>
    float entryValue;
    /// <summary>
    /// Value used for exit direction calculation.
    /// </summary>
    float exitValue;
    /// <summary>
    /// EnvironmentPlanning identifier.
    /// </summary>
    [NonSerialized] public string envID;

    private float lastYRotation;

    /// <summary>
    /// True if the task is completed.
    /// </summary>
    [NonSerialized] public bool taskCompleted = false;
    /// <summary>
    /// True if the environment is ready.
    /// </summary>
    //private bool isEnvReady = false;

    /// <summary>
    /// Reference to the objective interaction handler.
    /// </summary>
    private ObjectiveInteractionHandler objectiveHandler;

    // Variabili reflection per screenshot curriculum
#if UNITY_EDITOR
    private Curriculum curriculum;
    private Testing testing;
    private System.Reflection.FieldInfo videoRecorderField;
    private System.Reflection.FieldInfo waitingExtraEpisodesField;
#endif

    /**
     * \class ProxemicRange
     * \brief Helper class for proxemic reward ranges.
     */
    private class ProxemicRange
    {
        public float Start { get; set; }
        public float End { get; set; }
        public float Reward { get; set; }
        public string StatsTag { get; set; }
        public float RaysNumber { get; set; }
    }

    /// <summary>
    /// List of proxemic ranges for agent proximity rewards.
    /// </summary>
    private List<ProxemicRange> ProxemicRanges;

    /// <summary>
    /// Array of front crossings for the agent.
    /// </summary>
    public float[] crossings;

    /// <summary>
    /// Maximum number of intermediate targets that can be handled by the agent.
    /// </summary>
    public const int MaxIntermediateTargets = 15;

    /**
     * \brief Called when the script instance is being loaded.
     * Initializes the agent's components and sets its initial position and rotation.
     */
    private void Awake()
    {
        // Init all components
        animator = GetComponent<Animator>();
        animationManager = GetComponent<RLPlanningAnimationManager>();
        agentSensorsManager = GetComponent<AgentPlanningSensorsManager>();
        agentGizmosDrawer = GetComponent<AgentPlanningGizmosDrawer>();
        agentObserver = GetComponent<AgentPlanningObserver>();
        objectiveObserver = GetComponent<ObjectiveObserver>();
        objectiveHandler = GetComponent<ObjectiveInteractionHandler>();
        rigidBody = GetComponent<Rigidbody>();

        // Constants
        constants = new ConstantsPlanning();
        agentSensorsManager.constants = this.constants;
        agentObserver.constants = this.constants;
        agentGizmosDrawer.constants = this.constants;
        minMaxSpeed = constants.minMaxSpeed;
        speedMaxRange = constants.speedMaxRange;

        ProxemicRanges = new List<ProxemicRange>
        {
            new ProxemicRange { Start = constants.proxemic_medium_distance, End = constants.proxemic_large_distance, Reward = constants.proxemic_large_agent_reward, StatsTag = "Large", RaysNumber = constants.proxemic_large_ray },
            new ProxemicRange { Start = constants.proxemic_small_distance, End = constants.proxemic_medium_distance, Reward = constants.proxemic_medium_agent_reward, StatsTag = "Medium", RaysNumber = constants.proxemic_medium_ray },
            new ProxemicRange { Start = 0, End = constants.proxemic_small_distance, Reward = constants.proxemic_small_agent_reward, StatsTag = "Small", RaysNumber = constants.proxemic_small_ray }
        };



        // Check if all required components are present
        if (agentSensorsManager == null)
            Debug.LogError($"AgentSensorsManager component missing on {gameObject.name}");
        if (agentObserver == null)
            Debug.LogError($"AgentObserver component missing on {gameObject.name}");
        if (objectiveObserver == null)
            Debug.LogError($"ObjectiveObserver component missing on {gameObject.name}");
        if (objectiveHandler == null)
            Debug.LogError($"ObjectiveInteractionHandler component missing on {gameObject.name}");
        if (agentGizmosDrawer == null)
            Debug.LogError($"AgentGizmosDrawer component missing on {gameObject.name}");

        // TODO: Keep or not
        if (agentSensorsManager != null)
        {
            /*if (agentSensorsManager.invisibleTargets == null)
            {
                Debug.LogWarning($"invisibleTargets was null, initializing empty list for {gameObject.name}");
                agentSensorsManager.invisibleTargets = new List<GameObject>();
            }
            
            // Tutti i target sono ora sempre visibili - rimuovi tutti gli elementi dalla lista invisibleTargets
            agentSensorsManager.invisibleTargets.Clear();*/

            // Chiama questi metodi solo se agentSensorsManager è valido
            try
            {
                agentSensorsManager.UpdateTargetSensorVision(group);
                agentSensorsManager.UpdateObjectiveSensorVision(group);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating sensor vision for {gameObject.name}: {e.Message}");
            }
        }

        startPosition = transform.position;
        startRotation = transform.rotation;

        // Default walking state
        if (animationManager != null)
        {
            animationManager.SetWalking(true);
        }

#if UNITY_EDITOR
        curriculum = FindObjectOfType<Curriculum>();
        if (curriculum != null)
        {
            videoRecorderField = curriculum.GetType().GetField("videoRecorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            waitingExtraEpisodesField = curriculum.GetType().GetField("waitingExtraEpisodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
        testing = FindObjectOfType<Testing>();
#endif

        switch (group)
        {
            case Group.First:
                GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.red;
                break;
            case Group.Second:
                GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.cyan;
                break;
            case Group.Third:
                GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.green;
                break;
            case Group.Fourth:
                GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.yellow;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        // If the objective handler is executing animations, skip the update
        if (objectiveHandler != null && objectiveHandler.IsExecutingObjectiveAnimations())
        {
            return; // No basic movement or animation updates while executing animations
        }

        float speed = rigidBody.velocity.magnitude;
        animationManager.UpdateSpeed(speed / 10);

        // Idle/Walking
        if (speed < 0.25f)
        {
            //Debug.Log("Animation State: Idle");
            animationManager.SetWalking(false);

            float currentYRotation = transform.eulerAngles.y;
            float deltaY = Mathf.DeltaAngle(lastYRotation, currentYRotation);
            float angularSpeed = Mathf.Abs(deltaY) / Time.deltaTime;

            // Turn - only if not already turning and significant rotation
            if (Mathf.Abs(deltaY) > 10f && !IsTurnAnimationPlaying())
            {
                Debug.Log("Animation State: Turn");
                float normalizedTurnSpeed = Mathf.Clamp(angularSpeed / 90f, 0.5f, 1.0f);
                if (deltaY > 0)
                    animationManager.PlayTurn(true, normalizedTurnSpeed); // TurnRight
                else
                    animationManager.PlayTurn(false, normalizedTurnSpeed); // TurnLeft
            }
            else if (Mathf.Abs(deltaY) < 2f && IsTurnAnimationPlaying())
            {
                // Stop turn if rotation is minimal
                animationManager.StopTurn();
                animationManager.SetIdle(true);
            }
        }
        else
        {
            //Debug.Log("Animation State: Walking");
            // Stop any turn animations when walking
            if (IsTurnAnimationPlaying())
            {
                animationManager.StopTurn();
            }
            animationManager.SetWalking(true);
        }

        lastYRotation = transform.eulerAngles.y;
    }
    
    /// <summary>
    /// Checks if a turn animation is currently playing
    /// </summary>
    /// <returns>True if turn animation is active</returns>
    private bool IsTurnAnimationPlaying()
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("TurnLeft") || stateInfo.IsName("TurnRight");
    }

    private void Start()
    {
        if (monitorArtifact != null)
            monitorArtifact.OnPropertyChanged += HandleMonitorPropertyChanged;
    }

    

    /// <summary>
    /// Gets the color associated with this agent's group.
    /// </summary>
    /// <returns>The agent's group color</returns>
    public Color GetAgentColor()
    {
        switch (group)
        {
            case Group.First:
                return Color.red;
            case Group.Second:
                return Color.cyan;
            case Group.Third:
                return Color.green;
            case Group.Fourth:
                return Color.yellow;
            default:
                return Color.white; // Colore di default per gruppi non riconosciuti
        }
    }

    /**
     * \brief Called at the beginning of each episode.
     * Resets the agent's state.
     */
    public override void OnEpisodeBegin()
    {
        stepLeft = envStep;
        uniqueID = Guid.NewGuid().ToString();
        rigidBody.velocity = Vector3.zero;
        tempoIniziale = (int)Time.time;
        currentSpeed = 0;
        numberOfIteraction = 0;
        InitCrossings(numberOfCrossings);
        // minMaxSpeed.y = RandomGaussian(speedMaxRange.x, speedMaxRange.y); // Disabilita randomizzazione
        resetAgent?.Invoke(this);
    }

    /**
     * \brief Makes the agent listen for environment readiness.
     * \param action Reference to the action to subscribe to.
     */
    public void MakeListenEnvReady(ref Action<RLAgentPlanning> action)
    {
        action += (agent) =>
        {
            if (agent == this)
            {
                if (env != null)
                {
                    numberOfCrossings = env.GetNumIntermediateTargets();
                    InitCrossings(numberOfCrossings);
                    Debug.Log($"Agent {gameObject.name}: initialized {numberOfCrossings} crossings");
                }
                else
                {
                    Debug.LogError($"Agent {gameObject.name}: EnvironmentPlanning is null!");
                }

                if (objectiveHandler != null)
                {
                    objectiveHandler.InitializeObjectivesFromEnvironment();
                    Debug.Log($"Agent {gameObject.name} initialized objectives via ObjectiveInteractionHandler");
                }
                else
                {
                    Debug.LogWarning($"Agent {gameObject.name} does not have an ObjectiveInteractionHandler component");
                }
            }
        };

        print("Agent " + gameObject.name + " is listening to the environment");
    }

    /**
     * \brief Collects observations from the environment to feed to the neural network.
     * \param vectorSensor The vector sensor for observations.
     */
    public override void CollectObservations(VectorSensor vectorSensor)
    {
        Dictionary<AgentPlanningSensorsManager.Sensore, RaycastHit[]> sensorsResults = agentSensorsManager.ComputeSensorResults();

        wallsAndAgents = agentObserver.WallsAndAgentsGizmos;
        wallsAndTargets = agentObserver.WallsAndTargetsGizmos;
        wallsAndObjectives = agentObserver.WallsAndObjectivesGizmos;

        (wallsAndTargetsObservations, wallsAndAgentsObservations, wallsAndObjectivesObservations) = agentObserver.ComputeObservations(sensorsResults);

        float normalizedSpeed = (currentSpeed - minMaxSpeed.x) / (minMaxSpeed.y - minMaxSpeed.x);
        agentGizmosDrawer.SetObservationsResults(wallsAndTargets, wallsAndAgents, wallsAndObjectives);

        vectorSensor.AddObservation(wallsAndTargetsObservations);
        vectorSensor.AddObservation(wallsAndAgentsObservations);
        vectorSensor.AddObservation(wallsAndObjectivesObservations);
        vectorSensor.AddObservation(normalizedSpeed);
        vectorSensor.AddObservation(objectiveObserver.GetObjectivesObservation());
        //vectorSensor.AddObservation(GetCrossings());
        vectorSensor.AddObservation(taskCompleted ? 1.0f : 0.0f); //TODO: check se ha senso

        rewardsWallsAndTargetsObservations(wallsAndTargets);
        rewardsWallsAndAgentsObservations(wallsAndAgents);
    }

    /**
     * \brief Applies the actions received from the neural network.
     * \param vectorAction Array of actions.
     */
    [Obsolete]
    public override void OnActionReceived(float[] vectorAction)
    {
        if (run)
        {
            float realSpeed = rigidBody.velocity.magnitude;
            float actionSpeed;
            float actionAngle;
            if (constants.discrete)
            {
                actionSpeed = vectorAction[0];
                actionSpeed = (actionSpeed - 5f) / 5f;
                actionAngle = vectorAction[1];
                actionAngle = (actionAngle - 5f) / 5f;
            }
            else
            {
                actionSpeed = Mathf.Clamp(vectorAction[0], -1f, 1f);
                actionAngle = Mathf.Clamp(vectorAction[1], -1f, 1f);
            }

            // Non applicare movimenti se le animazioni degli obiettivi sono in corso
            if (!lockPosition && !(objectiveHandler != null && objectiveHandler.IsExecutingObjectiveAnimations()))
            {
                AngleChange(actionAngle);
                SpeedChange(actionSpeed);
            }

            int agentID = gameObject.GetInstanceID();
            numberOfIteraction++;
            StatsWriter.WriteAgentStats(
                transform.position.x,
                transform.position.z,
                group,
                currentSpeed,
                realSpeed,
                (actionAngle * constants.angleRange),
                envID,
                uniqueID,
                numberOfIteraction
                );
            StatsWriter.WritePedPyStats(
                transform.position.x,
                transform.position.y,
                transform.position.z,
                uniqueID.GetHashCode());
            ComputeSteps();
        }
    }

    /**
     * \brief Computes the steps and applies step rewards.
     */
    public void ComputeSteps()
    {
#if UNITY_EDITOR
        // Screenshot logic TRAINING 
        if (curriculum != null && videoRecorderField != null && waitingExtraEpisodesField != null)
        {
            var videoRecorder = videoRecorderField.GetValue(curriculum) as EnvironmentVideoRecorder;
            bool waitingExtraEpisodes = (bool)waitingExtraEpisodesField.GetValue(curriculum);
            if (waitingExtraEpisodes && videoRecorder != null && videoRecorder.IsRecording)
            {
                videoRecorder.TakeScreenshot();
            }
        }
        // Screenshot logic TESTING

        if (testing != null)
        {
            testing.OnAgentStep();
        }
#endif
        AddReward(constants.step_reward);
        stepLeft--;
        if (!taskCompleted)
        {
            AddReward(constants.incomplete_task_step_reward * objectiveHandler.GetRemainingObjectivesPercentage()); /// inomplete_task_step_reward is moltiplied by the percentage of objectives not completed
        }
        if (stepLeft <= 0)
        {
            AddReward(constants.step_finished_reward);
            print("finished_step");
            Finished();
        }
    }

    /**
     * \brief Ends the episode and resets the agent.
     */
    public void Finished()
    {
        cumulativeReward = GetCumulativeReward();
        gameObject.SetActive(false);
        StatsWriter.WriteEnvStats(group, (int)(Time.time - tempoIniziale));
        targetsTaken.Clear();
        transform.position = startPosition;
        transform.rotation = startRotation;
        EndEpisode();
        agentTerminated?.Invoke(cumulativeReward, env);
    }

    /**
     * \brief Generates a random value with a Gaussian distribution.
     * \param minValue Minimum value.
     * \param maxValue Maximum value.
     * \return Random Gaussian value.
     */
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;
        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    /**
     * \brief Triggered when the agent enters a collider.
     * \param other The collider entered.
     */
    private void OnTriggerEnter(Collider other)
    {
        GameObject triggerObject = other.gameObject;

        if (triggerObject.CompareTag("ArtifactZone"))
        {
            Artifact artifact = triggerObject.GetComponent<Artifact>() ?? 
                               triggerObject.GetComponentInParent<Artifact>();
            
            if (artifact != null && assignedArtifacts.Contains(artifact))
            {
                Debug.Log($"[Agent {gameObject.name}] Collided with assigned artifact: {artifact.ArtifactName}");
                
                int agentId = gameObject.GetInstanceID();
                //artifact.Use(agentId, "agent_collision", gameObject);
            }
            else if (artifact != null)
            {
                Debug.Log($"[Agent {gameObject.name}] Collided with unassigned artifact: {artifact.ArtifactName}");
            }

            return;
        }

        entryValue = Vector3.Dot(transform.forward, triggerObject.transform.forward);
        if (triggerObject.CompareTag("Target"))
        {
            Target target = triggerObject.GetComponent<Target>();

            if (IsFinalTarget(target))
            {
                if (taskCompleted)
                {
                    Debug.Log("Final target and all objectives completed!");
                    AddReward(constants.finale_target_all_objectives_completed_reward);
                    Finished();
                }
                else
                {
                    Debug.Log("Final target reached but not all objectives completed!");
                    AddReward(constants.finale_target_incomplete_objectives_reward);
                    Finished();
                }
            }
            else if (IsIntermediateTarget(target))
            {
                insideTargets.Add(target.id); 
            }
        }
        else if (fleeing && other.gameObject.name.Contains("Flee"))
        {
            Target target = triggerObject.GetComponent<Target>();
            entryValue = Vector3.Dot(transform.forward, triggerObject.transform.forward);
            if ((target.group == group || target.group == Group.Generic) && target.targetType == TargetType.Final)
            {
                if (taskCompleted)
                {
                    AddReward(constants.finale_target_all_objectives_completed_reward);
                }
                else
                {
                    AddReward(constants.finale_target_incomplete_objectives_reward);
                }
                print("Final target");
                Finished();
            }
        }
    }

    /**
     * \brief Triggered when the agent exits a collider.
     * \param other The collider exited.
     */
    private void OnTriggerExit(Collider other)
    {
        GameObject triggerObject = other.gameObject;

        if (objectiveHandler != null && objectiveHandler.IsValidObjective(triggerObject))
        {
            objectiveHandler.HandleObjectiveTrigger(triggerObject);
        }
        else if (triggerObject.CompareTag("Target"))
        {
            Target target = triggerObject.GetComponent<Target>();
            if (IsIntermediateTarget(target))
            {
                // Incrementa SOLO se eri dentro!
                if (insideTargets.Contains(target.id))
                {
                    HandleIntermediateTarget(triggerObject);
                    insideTargets.Remove(target.id); // ora sei fuori
                }
            }
        }
    }

    public void flee()
    {
        //animationManager.SetRunning();
        GameObject[] fleeTargets = GameObject.FindGameObjectsWithTag("Target");
        fleeing = true;


        var sensors = gameObject.GetComponent<AgentSensorsManager>();
        // TODo fix
        foreach (GameObject target in fleeTargets)
        {
            if (target.name.Contains("Flee"))
            {
                //sensors.invisibleTargets.Remove(target);
            }
            else
            {
                //if (!sensors.invisibleTargets.Contains(target))
                //sensors.invisibleTargets.Add(target);
            }
        }
    }

    /**
     * \brief Returns whether the task is completed.
     * \return True if task is completed.
     */
    public bool IsTaskCompleted()
    {
        return taskCompleted;
    }

    public List<GameObject> GetObjective()
    {
        return objectiveHandler.objectives;
    }

    /**
     * \brief Checks if a direction is valid based on the array returned by DetermineVisualizationDirection.
     * \param Array of directions to check
     * \return True if the direction is valid, false otherwise.
    */
    public bool CheckForValidDirection(float[] direction)
    {
        if (direction == null || direction.Length == 0)
        {
            Debug.LogWarning("Direction array is null or empty in CheckForValidDirection");
            return false;
        }

        float[] objectives = objectiveObserver.GetObjectivesObservation();
        int lastIndex = direction.Length - 1;

        if (!taskCompleted)
        {
            // Check if the direction matches any of the objectives
            for (int i = 0; i < direction.Length - 1; i++)
            {
                if (direction[i] == 1 && direction[i] == objectives[i])
                {
                    //Debug.Log($"Valid direction found at index {i}");
                    return true;
                }
            }
        }
        else
        {
            // Task is completed, check only the last index
            if (direction[lastIndex] == 1 && direction[lastIndex] == objectives[lastIndex])
            {
                //Debug.Log("Valid direction for final target");
                return true;
            }
        }

        //Debug.Log("No valid direction found");
        return false;
    }

    /**
     * \brief Determines the visualization direction based on the target object.
     * 
     * This method calculates the direction from the agent to the target and checks if it aligns with the target's forward direction.
     * It returns an array of float values representing the compatibility of the direction with the original system.
     * 
     * If the DirectionsObjectives component is not found on the target object, an error is logged and an empty array is returned.
     * 
     * @param targetObject The target GameObject to evaluate.
     * @return An array of float values representing the direction compatibility.
     */
    public float[] DetermineVisualizationDirection(GameObject targetObject)
    {
        Vector3 agentToTarget = targetObject.transform.position - transform.position;
        agentToTarget.y = 0;

        Vector3 targetForward = targetObject.transform.forward;
        targetForward.y = 0;

        float alignment = Vector3.Dot(targetForward, agentToTarget.normalized);

        int passDirection = (alignment > 0) ? 0 : 1;

        DirectionsObjectives directions = targetObject.GetComponent<DirectionsObjectives>();
        if (directions == null)
        {
            Debug.LogError("DirectionsObjectives component not found on " + targetObject.name);
            return new float[0];
        }

        return directions.getDirections(passDirection);
    }

    /**
     * \brief Determines the passing direction based on the trigger object.
     * 
     * This method calculates the passing direction based on the entry value and retrieves the direction objectives from the DirectionsObjectives component.
     * If the DirectionsObjectives component is not found, an error is logged and an empty array is returned.
     * 
     * @param triggerObject The GameObject that triggered the event.
     * @return An array of float values representing the passing direction.
     */
    public float[] DeterminePassingDirection(GameObject triggerObject)
    {
        int passDirection = (entryValue > 0) ? 0 : 1;
        DirectionsObjectives directions = triggerObject.GetComponent<DirectionsObjectives>();
        if (directions == null)
        {
            Debug.LogError("DirectionsObjectives component not found on " + triggerObject.name);
            return new float[0];
        }

        float[] directionObjectives = directions.getDirections(passDirection);
        return directionObjectives;
    }
    /**
     * \brief Provides manual control for the agent (for debugging).
     * \param actionsOut Output actions array.
     */
    [Obsolete]
    public override void Heuristic(float[] actionsOut)
    {
        // move agent by keyboard
        var continuousActionsOut = actionsOut;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    public void SpeedChange(float deltaSpeed)
    {
        currentSpeed += minMaxSpeed.y * deltaSpeed / 2f;
        currentSpeed = Mathf.Clamp(currentSpeed, minMaxSpeed.x, minMaxSpeed.y);
        Vector3 velocityChange = (transform.forward * currentSpeed * 5) - rigidBody.velocity;
        rigidBody.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void AngleChange(float deltaAngle)
    {
        newAngle = Mathf.Round((deltaAngle * constants.angleRange) + transform.rotation.eulerAngles.y);
        newAngle %= 360;
        if (newAngle < 0) { newAngle += 360f; }
        transform.eulerAngles = new Vector3(0, newAngle, 0);
    }

    /**
     * \brief Checks if the target is a final target for this agent.
     * \param target The target to check.
     * \return True if it is a final target.
     */
    private bool IsFinalTarget(Target target)
    {
        return target.targetType == TargetType.Final && (target.group == group || target.group == Group.Generic);
    }

    /**
     * \brief Checks if the target is an intermediate target for this agent.
     * \param target The target to check.
     * \return True if it is an intermediate target.
     */
    private bool IsIntermediateTarget(Target target)
    {
        return target.targetType == TargetType.Intermediate &&
            (target.group == group || target.group == Group.Generic);
    }

    /**
     * \brief Applies rewards based on proximity to walls and targets.
     * \param wallsAndTargets List of detected walls and targets.
     */
    private void rewardsWallsAndTargetsObservations(List<(GizmosTagPlanning, Vector3)> wallsAndTargets)
    {
        bool proxemic_small_wall = false;

        for (int i = 0; i < wallsAndTargets.Count; i++)
        {
            (GizmosTagPlanning wallsAndTargetTag, Vector3 wallsAndTargetVector) = wallsAndTargets[i];
            float agentAndWallsAndTargetDistance = Vector3.Distance(transform.position + Vector3.up, wallsAndTargetVector);
            if ((agentAndWallsAndTargetDistance < constants.proxemic_small_distance + constants.rayOffset) &&
                (wallsAndTargetTag == GizmosTagPlanning.Wall))
            {
                StatsWriter.WriteAgentCollision(
                   transform.position.x,
                   transform.position.z,
                   "Wall",
                   "Small",
                   uniqueID
                );
                proxemic_small_wall = true;
            }
        }

        if (proxemic_small_wall)
        {
            AddReward(constants.proxemic_small_wall_reward);
            print("proxemic_small_wall_reward");
        }
    }

    /**
     * \brief Checks if the ray index is within the limit.
     * \param rayIndex Index of the ray.
     * \param proxemicRaysNumber Number of proxemic rays.
     * \return True if within the limit.
     */
    private bool IsRayWithinTheLimit(int rayIndex, float proxemicRaysNumber)
    {
        return rayIndex < (proxemicRaysNumber * 2) + 1;
    }

    /**
     * \brief Checks if the distance is within the proxemic range and tag matches.
     * \param distance Distance to check.
     * \param start Start of the range.
     * \param end End of the range.
     * \param RaysNumber Number of rays.
     * \param tag Tag to check.
     * \param expectedTag Expected tag.
     * \return True if within range and tag matches.
     */
    private bool IsWithinProxemicRange(float distance, float start, float end, float RaysNumber, GizmosTagPlanning tag, GizmosTagPlanning expectedTag)
    {
        return (distance >= start + constants.rayOffset &&
            distance < end + constants.rayOffset &&
            tag == expectedTag);
    }

    /**
     * \brief Checks proxemic ranges and applies rewards.
     * \param distance Distance to the object.
     * \param tag Tag of the object.
     * \param uniqueID Unique agent ID.
     * \param id Ray index.
     */
    private void CheckProxemicRanges(float distance, GizmosTagPlanning tag, string uniqueID, int id)
    {
        foreach (var range in ProxemicRanges)
        {
            if (IsWithinProxemicRange(distance, range.Start, range.End, range.RaysNumber, tag, GizmosTagPlanning.Agent)
                && IsRayWithinTheLimit(id, range.RaysNumber))
            {
                StatsWriter.WriteAgentCollision(
                    transform.position.x,
                    transform.position.z,
                    "Agent",
                    range.StatsTag,
                    uniqueID
                );
                AddReward(range.Reward);
                print($"{range.StatsTag} reward applied");
                break; // Exit the loop if a match is found
            }
        }
    }

    /**
     * \brief Applies rewards based on proximity to walls and agents.
     * \param wallsAndAgents List of detected walls and agents.
     */
    private void rewardsWallsAndAgentsObservations(List<(GizmosTagPlanning, Vector3)> wallsAndAgents)
    {
        int id = 0;
        foreach (var (tag, position) in wallsAndAgents)
        {
            float distance = Vector3.Distance(transform.position + Vector3.up, position);
            CheckProxemicRanges(distance, tag, uniqueID, id++);
        }
    }

    /**
     * \brief Handles logic when passing through an intermediate target.
     * \param triggerObject The target object.
     */
    private void HandleIntermediateTarget(GameObject triggerObject)
    {
        exitValue = Vector3.Dot(transform.forward, triggerObject.transform.forward);
        float crossingValue = entryValue * exitValue;

        if (crossingValue >= 0)
        {
            targetsTaken.Add(triggerObject);
            Target target = triggerObject.GetComponent<Target>();
            if (crossings[target.id] > 0)
            {
                AddReward(constants.target_alredy_crossed_reward * crossings[target.id]);
                Debug.Log("Target already crossed");
            }
            IncrementCrossing(target);

            float[] directionObjectives = DeterminePassingDirection(triggerObject);

            if (directionObjectives != null && directionObjectives.Length > 0)
            {
                if (CheckForValidDirection(directionObjectives))
                {
                    //Debug.Log($"Correct direction taken (entryValue: {entryValue})");
                    //AddReward(constants.correct_direction_reward);
                }
                else
                {
                    //Debug.Log($"Wrong direction taken (entryValue: {entryValue})");
                    AddReward(constants.wrong_direction_reward);
                }
            }
            if (targetsTaken.Contains(triggerObject))
            {
                AddReward(constants.already_taken_target_reward);
                Debug.Log("Target already taken");
            }
        }
        else
        {
            AddReward(constants.target_taken_incorrectly_reward);
            Debug.Log("Target taken incorrectly");
        }
    }

    // Method to initialize the crossing arrays (call when you know the number of intermediate targets)
    public void InitCrossings(int numIntermediateTargets)
    {
        crossings = new float[MaxIntermediateTargets];
        for (int i = 0; i < MaxIntermediateTargets; i++)
        {
            if (i < numIntermediateTargets)
                crossings[i] = 0;
            else
                crossings[i] = -1f;
        }
        Debug.Log($"Agent {gameObject.name}: inizializzato array crossings con {numIntermediateTargets} target intermedi");
    }

    // Method to increment only if the index is valid (not a sentinel cell)
    public void IncrementCrossing(Target target)
    {
        if (target.targetType == TargetType.Intermediate && target.id >= 0 && target.id < crossings.Length && crossings[target.id] != -1f)
        {
            crossings[target.id]++;
        }
    }

    // Getters for the neural network (arrays always have fixed size)
    public float[] GetCrossings() => crossings;

    public float GetCrossings(int index)
    {
        if (index >= 0 && index < crossings.Length)
        {
            return crossings[index];
        }
        else
        {
            return -1f;
        }
    }


    public void SetRun(bool value)
    {
        run = value;
    }


    /// <summary>
    /// Returns the agent's Rigidbody component.
    /// </summary>
    /// <returns>The Rigidbody component</returns>
    public Rigidbody GetRigidBody()
    {
        return rigidBody;
    }

    /// <summary>
    /// Public accessor for the agent's Rigidbody component.
    /// </summary>
    public Rigidbody RigidBody => rigidBody;

    private HashSet<int> insideTargets = new HashSet<int>();

    
    /// <summary>
    /// Handles property changes from the monitor artifact.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    private void HandleMonitorPropertyChanged(string propertyName, object value)
    {
        switch (propertyName)
        {
            case "placedOrders":
                var orders = value as List<OrderPlacedData>;
                if (orders != null && !myOrderId.HasValue && hasPlacedOrder)
                {
                    foreach (var order in orders)
                    {
                        if (order.agentId == gameObject.GetInstanceID())
                        {
                            myOrderId = order.orderId;
                            Debug.Log($"[Agent {gameObject.name}] (ObsProp) Order #{order.orderId} successfully placed");
                            break;
                        }
                    }
                }
                break;

            case "ordersReady":
                var readyOrderIds = value as List<int>;
                if (myOrderId.HasValue && readyOrderIds != null && readyOrderIds.Contains(myOrderId.Value))
                {
                    Debug.Log($"[Agent {gameObject.name}] (ObsProp) My order {myOrderId.Value} ready!");
                    // PickUpOrder();
                }
                break;

            case "ordersInPreparation":
                var prepOrderIds = value as List<int>;
                if (myOrderId.HasValue && prepOrderIds != null && prepOrderIds.Contains(myOrderId.Value))
                {
                    Debug.Log($"[Agent {gameObject.name}] My order {myOrderId.Value} is now in preparation");
                }
                break;


            default:
                break;
        }
    }

    /// <summary>
    /// Method for agent to place an order at the totem
    /// </summary>
    public void PlaceOrder()
    {
        if (totemArtifact != null && !hasPlacedOrder)
        {
            hasPlacedOrder = true;

            int agentId = gameObject.GetInstanceID();
            totemArtifact.Use(agentId);

            Debug.Log($"[Agent {gameObject.name}] Placed Order");
        }
        else if (hasPlacedOrder)
        {
            Debug.Log($"[Agent {gameObject.name}] Already Placed Order");
        }
    }

    /// <summary>
    /// Method for agent to pick up a ready order
    /// </summary>
    public void PickUpOrder()
    {
        if (monitorArtifact == null || !myOrderId.HasValue)
        {
            Debug.Log($"[Agent {gameObject.name}] Cannot pick up order: monitor or orderId missing");
            return;
        }

        int agentId = gameObject.GetInstanceID();
        monitorArtifact.Use(agentId, myOrderId.Value);
        Debug.Log($"[Agent {gameObject.name}] Picked up order #{myOrderId.Value}");
    }
    

    private void OnDestroy()
    {
        if (monitorArtifact != null)
            monitorArtifact.OnPropertyChanged -= HandleMonitorPropertyChanged;
        
    }

}