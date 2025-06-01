using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.AI;

[RequireComponent(typeof(ObjectiveInteractionHandler))]
[RequireComponent(typeof(ObjectiveObserver))]
/**
 * \class RLAgent
 * \brief Represents the RL agent. Inherits from the Agent class of ML-Agents.
 */
public class RLAgent : Agent
{
    private bool run = true;

    /// <summary>
    /// The group to which the agent belongs.
    /// </summary>
    public Group group;

    /// <summary>
    /// Minimum and maximum speed for the agent.
    /// </summary>
    private Vector2 minMaxSpeed = MyConstants.minMaxSpeed;

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
    private AgentAnimationManager animationManager;

    /// <summary>
    /// Range for randomizing speed.
    /// </summary>
    private Vector2 speedMaxRange = MyConstants.speedMaxRange;

    /// <summary>
    /// List of targets taken by the agent.
    /// </summary>
    [NonSerialized] public List<GameObject> targetsTaken = new List<GameObject>();

    /// <summary>
    /// Reference to the agent's sensors manager.
    /// </summary>
    private AgentSensorsManager agentSensorsManager;
    /// <summary>
    /// Reference to the agent's gizmos drawer.
    /// </summary>
    private AgentGizmosDrawer agentGizmosDrawer;
    /// <summary>
    /// Reference to the agent's observer.
    /// </summary>
    private AgentObserver agentObserver;
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
    private List<(GizmosTag, Vector3)> wallsAndAgents = new List<(GizmosTag, Vector3)>();
    /// <summary>
    /// List of walls and targets detected.
    /// </summary>
    private List<(GizmosTag, Vector3)> wallsAndTargets = new List<(GizmosTag, Vector3)>();
    /// <summary>
    /// List of walls and objectives detected.
    /// </summary>
    private List<(GizmosTag, Vector3)> wallsAndObjectives = new List<(GizmosTag, Vector3)>();

    /// <summary>
    /// Event triggered when the agent is terminated.
    /// </summary>
    public event Action<float, Environment> agentTerminated;
    /// <summary>
    /// Event triggered to reset the agent.
    /// </summary>
    public event Action<RLAgent> resetAgent;

    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    private string uniqueID;
    /// <summary>
    /// Number of steps in the environment.
    /// </summary>
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
    [NonSerialized] public Environment env;

    /// <summary>
    /// Value used for entry direction calculation.
    /// </summary>
    float entryValue;
    /// <summary>
    /// Value used for exit direction calculation.
    /// </summary>
    float exitValue;
    /// <summary>
    /// Environment identifier.
    /// </summary>
    [NonSerialized] public string envID;

    private float lastYRotation;

    /// <summary>
    /// True if the task is completed.
    /// </summary>
    [NonSerialized] public bool taskCompleted = false; // CAMBIA DA true A false
    /// <summary>
    /// True if the environment is ready.
    /// </summary>
    private bool isEnvReady = false;

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
    private static readonly List<ProxemicRange> ProxemicRanges = new List<ProxemicRange>
    {
        new ProxemicRange { Start = MyConstants.proxemic_medium_distance, End = MyConstants.proxemic_large_distance, Reward = MyConstants.proxemic_large_agent_reward, StatsTag = "Large", RaysNumber = MyConstants.proxemic_large_ray },
        new ProxemicRange { Start = MyConstants.proxemic_small_distance, End = MyConstants.proxemic_medium_distance, Reward = MyConstants.proxemic_medium_agent_reward, StatsTag = "Medium", RaysNumber = MyConstants.proxemic_medium_ray },
        new ProxemicRange { Start = 0, End = MyConstants.proxemic_small_distance, Reward = MyConstants.proxemic_small_agent_reward, StatsTag = "Small", RaysNumber = MyConstants.proxemic_small_ray }
    };

    /**
     * \brief Called when the script instance is being loaded.
     * Initializes the agent's components and sets its initial position and rotation.
     */
    private void Awake()
    {
        // Inizializza TUTTI i componenti prima di usarli
        animator = GetComponent<Animator>();
        animationManager = GetComponent<AgentAnimationManager>();
        agentSensorsManager = GetComponent<AgentSensorsManager>();
        agentGizmosDrawer = GetComponent<AgentGizmosDrawer>();
        agentObserver = GetComponent<AgentObserver>();
        objectiveObserver = GetComponent<ObjectiveObserver>();
        objectiveHandler = GetComponent<ObjectiveInteractionHandler>();
        rigidBody = GetComponent<Rigidbody>();
        
        // Verifica che tutti i componenti necessari siano presenti
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
        
        // Assicurati che invisibleTargets sia inizializzato
        if (agentSensorsManager != null)
        {
            if (agentSensorsManager.invisibleTargets == null)
            {
                Debug.LogWarning($"invisibleTargets was null, initializing empty list for {gameObject.name}");
                agentSensorsManager.invisibleTargets = new List<GameObject>();
            }
            
            // Tutti i target sono ora sempre visibili - rimuovi tutti gli elementi dalla lista invisibleTargets
            agentSensorsManager.invisibleTargets.Clear();
            
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
        
        // RIMUOVI: Non inizializzare ObjectiveObserver qui!
        // L'inizializzazione avverrà in MakeListenEnvReady quando l'environment è pronto
    
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Imposta l'animazione di walking come default
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
            Debug.Log("Group is not recognized");
            break;
    }
    }

    private void Update()
    {
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

            // Turn da fermo (rotazione minima) - usa turn speed dinamica
            if (Mathf.Abs(deltaY) > 10f)
            {
                Debug.Log("Animation State: Turn");
                float normalizedTurnSpeed = Mathf.Clamp(angularSpeed / 90f, 0.5f, 1.0f); 
                if (deltaY > 0)
                    animationManager.PlayTurn(true, normalizedTurnSpeed); // TurnRight
                else
                    animationManager.PlayTurn(false, normalizedTurnSpeed); // TurnLeft
            }
        }
        else
        {
            //Debug.Log("Animation State: Walking");
            // Se sta camminando, ferma immediatamente le animazioni di turn
            animationManager.StopTurn();
            animationManager.SetWalking(true);
        }
        
        lastYRotation = transform.eulerAngles.y;
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
        minMaxSpeed.y = RandomGaussian(speedMaxRange.x, speedMaxRange.y);
        resetAgent?.Invoke(this);
    }

    /**
     * \brief Makes the agent listen for environment readiness.
     * \param action Reference to the action to subscribe to.
     */
    public void MakeListenEnvReady(ref Action<RLAgent> action)
    {
        action += (agent) =>
        {
            if (agent == this)
            {
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
        Dictionary<AgentSensorsManager.Sensore, RaycastHit[]> sensorsResults = agentSensorsManager.ComputeSensorResults();

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
        vectorSensor.AddObservation(taskCompleted); //TODO controllaree se ha senso
        
        rewardsWallsAndTargetsObservations(wallsAndTargets);
        rewardsWallsAndAgentsObservations(wallsAndAgents);
        //rewardsWallsAndObjectivesObservations(wallsAndObjectives);
    }

    /**
     * \brief Applies the actions received from the neural network.
     * \param vectorAction Array of actions.
     */
    public override void OnActionReceived(float[] vectorAction)
    {
        if (run)
        {
            float realSpeed = rigidBody.velocity.magnitude;
            float actionSpeed;
            float actionAngle;
            if (MyConstants.discrete)
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
            
            if (!lockPosition)
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
                (actionAngle * MyConstants.angleRange),
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
        // Logica screenshot per TRAINING (già presente)
        if (curriculum != null && videoRecorderField != null && waitingExtraEpisodesField != null)
        {
            var videoRecorder = videoRecorderField.GetValue(curriculum) as EnvironmentVideoRecorder;
            bool waitingExtraEpisodes = (bool)waitingExtraEpisodesField.GetValue(curriculum);
            if (waitingExtraEpisodes && videoRecorder != null && videoRecorder.IsRecording)
            {
                videoRecorder.TakeScreenshot();
            }
        }
        // Logica screenshot per TESTING
        
        if (testing != null)
        {
            testing.OnAgentStep();
        }
#endif
        AddReward(MyConstants.step_reward);
        stepLeft--;
        if (!taskCompleted) 
        {
            AddReward(MyConstants.incomplete_task_step_reward * objectiveHandler.GetRemainingObjectivesPercentage()); /// inomplete_task_step_reward is moltiplied by the percentage of objectives not completed
        }
        if (stepLeft <= 0)
        {
            AddReward(MyConstants.step_finished_reward);
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

    public void MoveToNextTargetWithDelay(float delay)
    {
        // Metodo mantenuto per compatibilità ma non più utilizzato
        Debug.LogWarning("MoveToNextTargetWithDelay is deprecated with the new objectives system");
    }

    public Rigidbody GetRigidBody() => rigidBody;

    public void MoveToNextTarget()
    {
        // Metodo mantenuto per compatibilità ma non più utilizzato
        Debug.LogWarning("MoveToNextTarget is deprecated with the new objectives system");
    }

    /**
     * \brief Triggered when the agent enters a collider.
     * \param other The collider entered.
     */
    private void OnTriggerEnter(Collider other)
    {
        GameObject reachedTarget = other.gameObject;
        
        // Gestisce solo i target generici con il nuovo sistema objectives
        if (other.gameObject.CompareTag("Target"))
        {
            Target target = other.gameObject.GetComponent<Target>();
            entryValue = Vector3.Dot(transform.forward, other.gameObject.transform.forward);
            
            if (IsFinalTarget(target))
            {
                if (taskCompleted)
                {
                    Debug.Log("Final target and all objectives completed!");
                    AddReward(MyConstants.finale_target_all_objectives_completed_reward);
                    Finished();
                }
                else
                {
                    Debug.Log("Final target reached but not all objectives completed!");
                    AddReward(MyConstants.finale_target_incomplete_objectives_reward);
                    Finished();
                }
            }
        }
        else if (fleeing && other.gameObject.name.Contains("Flee"))
        {
            Target target = reachedTarget.GetComponent<Target>();
            entryValue = Vector3.Dot(transform.forward, reachedTarget.transform.forward);
            if ((target.group == group || target.group == Group.Generic) && target.targetType == TargetType.Final)
            {
                if (taskCompleted)
                {
                    AddReward(MyConstants.finale_target_all_objectives_completed_reward);
                }
                else
                {
                    AddReward(MyConstants.finale_target_incomplete_objectives_reward);
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
                HandleIntermediateTarget(triggerObject);
            }
        }
    }

    public void flee()
    {
        animationManager.SetRunning();
        GameObject[] fleeTargets = GameObject.FindGameObjectsWithTag("Target");
        fleeing = true;
        
        // Nel nuovo sistema, tutti i target normali diventano invisibili durante il flee
        // e solo i target flee diventano visibili
        var sensors = gameObject.GetComponent<AgentSensorsManager>();
        
        foreach (GameObject target in fleeTargets)
        {
            if (target.name.Contains("Flee"))
            {
                sensors.invisibleTargets.Remove(target);
            }
            else
            {
                if (!sensors.invisibleTargets.Contains(target))
                    sensors.invisibleTargets.Add(target);
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
        if (objectiveHandler != null)
        {
            return objectiveHandler.GetRemainingObjectives();
        }
        return new List<GameObject>();
    }

    /// <summary>
    /// Controlla se una direzione è valida basandosi sull'array restituito da DetermineVisualizationDirection.
    /// </summary>
    /// <param name="direction">Array con informazioni sulla direzione</param>
    /// <returns>True se la direzione è valida</returns>
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
            // Controlla se c'è corrispondenza tra direzione e obiettivi rimanenti
            for (int i = 0; i < direction.Length - 1; i++)
            {
                if (direction[i] == 1 && direction[i] == objectives[i])
                {
                    Debug.Log($"Valid direction found at index {i}");
                    return true;
                }
            }
        }
        else
        {
            // Task completato: controlla solo l'ultimo elemento (target finale)
            if (direction[lastIndex] == 1 && direction[lastIndex] == objectives[lastIndex])
            {
                Debug.Log("Valid direction for final target");
                return true;
            }
        }
        
        Debug.Log("No valid direction found");
        return false;
    }

    /// <summary>
    /// Determina se la direzione verso un target è valida basandosi sugli obiettivi rimanenti.
    /// </summary>
    /// <param name="targetObject">Il target da valutare</param>
    /// <returns>Array con informazioni sulla direzione (compatibilità con sistema originale)</returns>
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
     * \brief Determines the passing direction for a trigger object.
     * \param triggerObject The trigger object.
     * \return Array of direction objectives.
     */
    public float[] DeterminePassingDirection(GameObject triggerObject)
    {
        // Compatibilità con il nuovo sistema
        return DetermineVisualizationDirection(triggerObject);
    }

    /**
     * \brief Provides manual control for the agent (for debugging).
     * \param actionsOut Output actions array.
     */
    public override void Heuristic(float[] actionsOut)
    {
        //move agent by keyboard
        var continuousActionsOut = actionsOut;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    public void SpeedChange(float deltaSpeed)
    {
        currentSpeed += (minMaxSpeed.y * deltaSpeed / 2f);
        currentSpeed = Mathf.Clamp(currentSpeed, minMaxSpeed.x, minMaxSpeed.y);
        Vector3 velocityChange = (transform.forward * currentSpeed * 5) - rigidBody.velocity;
        rigidBody.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void AngleChange(float deltaAngle)
    {
        newAngle = Mathf.Round((deltaAngle * MyConstants.angleRange) + transform.rotation.eulerAngles.y);
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
        return target != null && target.targetType == TargetType.Final;
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
    private void rewardsWallsAndTargetsObservations(List<(GizmosTag, Vector3)> wallsAndTargets)
    {
        bool proxemic_small_wall = false;

        for (int i = 0; i < wallsAndTargets.Count; i++)
        {
            (GizmosTag wallsAndTargetTag, Vector3 wallsAndTargetVector) = wallsAndTargets[i];
            float agentAndWallsAndTargetDistance = Vector3.Distance(transform.position + Vector3.up, wallsAndTargetVector);
            if ((agentAndWallsAndTargetDistance < MyConstants.proxemic_small_distance + MyConstants.rayOffset) &&
                (wallsAndTargetTag == GizmosTag.Wall))
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
            AddReward(MyConstants.proxemic_small_wall_reward);
            print("proxemic_small_wall_reward");
        }
    }

    /**
     * \brief Checks if the ray index is within the limit.
     * \param rayIndex Index of the ray.
     * \param proxemicRaysNumber Number of proxemic rays.
     * \return True if within the limit.
     */
    private bool IsRayWithinTheLimit(int rayIndex, float proxemicRaysNumber){
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
    private bool IsWithinProxemicRange(float distance, float start, float end, float RaysNumber, GizmosTag tag, GizmosTag expectedTag){
        return (distance >= start + MyConstants.rayOffset &&
            distance < end + MyConstants.rayOffset &&
            tag == expectedTag);
    }

    /**
     * \brief Checks proxemic ranges and applies rewards.
     * \param distance Distance to the object.
     * \param tag Tag of the object.
     * \param uniqueID Unique agent ID.
     * \param id Ray index.
     */
    private void CheckProxemicRanges(float distance, GizmosTag tag, string uniqueID, int id)
    {
        foreach (var range in ProxemicRanges)
        {
            if (IsWithinProxemicRange(distance, range.Start, range.End, range.RaysNumber, tag, GizmosTag.Agent)
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
    private void rewardsWallsAndAgentsObservations(List<(GizmosTag, Vector3)> wallsAndAgents)
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

            float[] directionObjectives = DeterminePassingDirection(triggerObject);

            if (directionObjectives != null && directionObjectives.Length > 0)
            {
                if (CheckForValidDirection(directionObjectives))
                {
                    //Debug.Log($"Correct direction taken (entryValue: {entryValue})");
                    //AddReward(MyConstants.correct_direction_reward);
                }
                else
                {
                    //Debug.Log($"Wrong direction taken (entryValue: {entryValue})");
                    AddReward(MyConstants.wrong_direction_reward);
                }
            }
            /*if (targetsTaken.Contains(triggerObject))
            {
                AddReward(MyConstants.already_taken_target_reward);
                Debug.Log("Target already taken");
            }*/
        }
        else
        {
            AddReward(MyConstants.target_taken_incorrectly_reward);
            Debug.Log("Target taken incorrectly");
        }
    }

    public void SetRun(bool value)
    {
        run = value;
    }
}
