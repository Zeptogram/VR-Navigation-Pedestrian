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
            Debug.Log("Animation State: Idle");
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
            Debug.Log("Animation State: Walking");
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
        // Inizializza osservazioni di default per garantire che il numero di osservazioni sia sempre corretto
        List<float> defaultWallsAndTargetsObs = new List<float>();
        List<float> defaultWallsAndAgentsObs = new List<float>();
        List<float> defaultWallsAndObjectivesObs = new List<float>();
        float defaultSpeed = 0f;
        float[] defaultObjectives = new float[0];

        // Verifica che i componenti necessari siano inizializzati
        if (agentSensorsManager == null || agentObserver == null || objectiveObserver == null || agentGizmosDrawer == null)
        {
            Debug.LogError($"One or more required components are null in {gameObject.name}");
            
            // Aggiungi osservazioni di default per mantenere la dimensione corretta
            vectorSensor.AddObservation(defaultWallsAndTargetsObs);
            vectorSensor.AddObservation(defaultWallsAndAgentsObs);
            vectorSensor.AddObservation(defaultWallsAndObjectivesObs);
            vectorSensor.AddObservation(defaultSpeed);
            vectorSensor.AddObservation(defaultObjectives);
            return;
        }

        // Ottieni i risultati dei sensori
        Dictionary<AgentSensorsManager.Sensore, RaycastHit[]> sensorsResults = null;
        try
        {
            sensorsResults = agentSensorsManager.ComputeSensorResults();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ComputeSensorResults for {gameObject.name}: {e.Message}");
            
            // Aggiungi osservazioni di default
            vectorSensor.AddObservation(defaultWallsAndTargetsObs);
            vectorSensor.AddObservation(defaultWallsAndAgentsObs);
            vectorSensor.AddObservation(defaultWallsAndObjectivesObs);
            vectorSensor.AddObservation(defaultSpeed);
            vectorSensor.AddObservation(defaultObjectives);
            return;
        }
        
        // Verifica che i risultati dei sensori siano validi
        if (sensorsResults == null || sensorsResults.Count == 0)
        {
            Debug.LogError($"SensorsResults is null or empty in {gameObject.name}");
            wallsAndTargetsObservations = defaultWallsAndTargetsObs;
            wallsAndAgentsObservations = defaultWallsAndAgentsObs;
            wallsAndObjectivesObservations = defaultWallsAndObjectivesObs;
            return;
        }

        // Verifica e pulisci ogni array di RaycastHit
        bool hasValidSensors = true;
        foreach (var kvp in sensorsResults)
        {
            if (kvp.Value == null)
            {
                Debug.LogError($"Sensor {kvp.Key.SensorName} has null RaycastHit array in {gameObject.name}");
                continue;
            }

            for (int i = 0; i < kvp.Value.Length; i++)
            {
                var hit = kvp.Value[i];
                if (hit.collider == null)
                {
                    //Debug.LogWarning($"RaycastHit at index {i} for sensor {kvp.Key.SensorName} has null collider in {gameObject.name}");
                    kvp.Value[i] = new RaycastHit
                    {
                        distance = MyConstants.rayLength,
                        point = transform.position + transform.forward * MyConstants.rayLength
                    };
                }
            }
        }

        if (!hasValidSensors)
        {
            // Aggiungi osservazioni di default
            vectorSensor.AddObservation(defaultWallsAndTargetsObs);
            vectorSensor.AddObservation(defaultWallsAndAgentsObs);
            vectorSensor.AddObservation(defaultWallsAndObjectivesObs);
            vectorSensor.AddObservation(defaultSpeed);
            vectorSensor.AddObservation(defaultObjectives);
            return;
        }

        // Calcola le osservazioni per i muri e gli agenti
        bool observationsComputed = false;
        try
        {
            (wallsAndTargetsObservations, wallsAndAgentsObservations, wallsAndObjectivesObservations) = agentObserver.ComputeObservations(sensorsResults);
            observationsComputed = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ComputeObservations for {gameObject.name}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            
            // Stampa informazioni dettagliate sui sensori per debug
            foreach (var kvp in sensorsResults)
            {
                Debug.LogError($"Sensor {kvp.Key.SensorName}: Array length = {kvp.Value?.Length ?? -1}");
                if (kvp.Value != null)
                {
                    for (int i = 0; i < Math.Min(kvp.Value.Length, 5); i++) // Stampa solo i primi 5 per evitare spam
                    {
                        var hit = kvp.Value[i];
                        Debug.LogError($"  Hit {i}: collider = {hit.collider?.name ?? "null"}, distance = {hit.distance}");
                    }
                }
            }
        }

        // Se le osservazioni non sono state calcolate correttamente, usa valori di default
        if (!observationsComputed)
        {
            wallsAndTargetsObservations = defaultWallsAndTargetsObs;
            wallsAndAgentsObservations = defaultWallsAndAgentsObs;
            wallsAndObjectivesObservations = defaultWallsAndObjectivesObs;
        }
        else
        {
            // Aggiorna le osservazioni degli oggetti dopo il calcolo (solo se il calcolo è riuscito)
            try
            {
                wallsAndTargets = agentObserver.WallsAndTargetsGizmos ?? new List<(GizmosTag, Vector3)>();
                wallsAndAgents = agentObserver.WallsAndAgentsGizmos ?? new List<(GizmosTag, Vector3)>();
                wallsAndObjectives = agentObserver.WallsAndObjectivesGizmos ?? new List<(GizmosTag, Vector3)>();

                // Setta i risultati delle osservazioni per il disegno delle gizmos
                agentGizmosDrawer.SetObservationsResults(wallsAndTargets, wallsAndAgents, wallsAndObjectives);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating gizmos for {gameObject.name}: {e.Message}");
            }
        }

        // Calcola la velocità normalizzata
        float normalizedSpeed = defaultSpeed;
        if (minMaxSpeed.y > minMaxSpeed.x)
        {
            normalizedSpeed = (currentSpeed - minMaxSpeed.x) / (minMaxSpeed.y - minMaxSpeed.x);
        }
        else
        {
            Debug.LogWarning($"Invalid speed range in {gameObject.name}: min={minMaxSpeed.x}, max={minMaxSpeed.y}");
        }

        // Aggiungi le osservazioni (garantendo sempre che vengano aggiunte)
        vectorSensor.AddObservation(wallsAndTargetsObservations ?? defaultWallsAndTargetsObs);
        vectorSensor.AddObservation(wallsAndAgentsObservations ?? defaultWallsAndAgentsObs);
        vectorSensor.AddObservation(wallsAndObjectivesObservations ?? defaultWallsAndObjectivesObs);
        vectorSensor.AddObservation(normalizedSpeed);
        
        // Verifica che objectiveObserver non sia null prima di usarlo
        try
        {
            var objectivesObservation = objectiveObserver.GetObjectivesObservation();
            if (objectivesObservation != null && objectivesObservation.Length > 0)
            {
                vectorSensor.AddObservation(objectivesObservation);
            }
            else
            {
                Debug.LogWarning($"ObjectivesObservation is null or empty in {gameObject.name}");
                vectorSensor.AddObservation(defaultObjectives);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting ObjectivesObservation for {gameObject.name}: {e.Message}");
            vectorSensor.AddObservation(defaultObjectives);
        }

        // Aggiungi ricompense per le osservazioni solo se le osservazioni sono state calcolate correttamente
        if (observationsComputed)
        {
            try
            {
                if (wallsAndTargets != null && wallsAndTargets.Count > 0)
                    rewardsWallsAndTargetsObservations(wallsAndTargets);
                if (wallsAndAgents != null && wallsAndAgents.Count > 0)
                    rewardsWallsAndAgentsObservations(wallsAndAgents);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in rewards calculation for {gameObject.name}: {e.Message}");
            }
        }
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
        // Se ho NavMeshAgent per ora lo ignoro
        NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            return;
        }

        GameObject reachedTarget = other.gameObject;
        
        // Gestisce gli objectives (nuovo sistema) - AGGIUNGI NULL CHECK
        if (objectiveHandler != null && objectiveHandler.IsValidObjective(reachedTarget))
        {
            objectiveHandler.HandleObjectiveTrigger(reachedTarget);
            return;
        }

        // If there is no target, exit
        Target target = other.GetComponent<Target>();

        if (target == null)
        {
            return;
        }

        // If there is a target
        exitValue = Vector3.Dot(transform.forward, reachedTarget.transform.forward);
        float resultValue = entryValue * exitValue;

        if ((target.group == Group.Generic || target.group == group) && target.targetType == TargetType.Intermediate)
        {
            if (!targetsTaken.Contains(reachedTarget))
            {
                if (resultValue >= 0)
                {
                    targetsTaken.Add(reachedTarget);
                    
                    // Nuovo sistema di direzioni con objectives
                    float[] directionObjectives = DeterminePassingDirection(reachedTarget);
                    
                    if (directionObjectives != null && directionObjectives.Length > 0)
                    {
                        if (CheckForValidDirection(directionObjectives))
                        {
                            AddReward(MyConstants.new_target_reward);
                            print("Target intermedio: " + reachedTarget.name);
                        }
                        else
                        {
                            AddReward(MyConstants.new_target_reward + MyConstants.wrong_direction_reward);
                            print("Target intermedio con direzione sbagliata: " + reachedTarget.name);
                        }
                    }
                    else
                    {
                        AddReward(MyConstants.new_target_reward);
                        print("Target intermedio: " + reachedTarget.name);
                    }
                }
                else
                {
                    AddReward(MyConstants.target_taken_incorrectly_reward);
                    print("Target intermedio preso in modo scorretto: " + reachedTarget.name);
                }
            }
            else
            {
                if (env != null && env.penaltyTakesTargetsAgain)
                {
                    AddReward(MyConstants.already_taken_target_reward);
                    print("Already_taken_target_reward: " + reachedTarget.name);
                }
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

    /**
     * \brief Checks if the direction is valid.
     * \param direction Array of direction values.
     * \return True if direction is valid.
     */
    public bool CheckForValidDirection(float[] direction)
    {
        // Nel nuovo sistema, tutti i target intermedi sono sempre validi se non già presi
        // La logica di direzione è gestita dagli objectives
        return true;
    }

    /**
     * \brief Determines the visualization direction for a target object.
     * \param targetObject The target object.
     * \return Array of direction objectives.
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
            Debug.LogWarning($"DirectionsObjectives component not found on {targetObject.name}. Returning empty array.");
            return new float[0]; // Restituisce un array vuoto invece di causare errori
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

    public void SetRun(bool value)
    {
        run = value;
    }
}