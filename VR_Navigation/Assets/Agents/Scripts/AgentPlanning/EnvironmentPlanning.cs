using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;

public class EnvironmentPlanning : MonoBehaviour
{
    // Start is called before the first frame update
    private List<RLAgentPlanning> agents;
    private int agentsTerminated = 0;
    [SerializeField] int maxSteps;

    public Action<float, EnvironmentPlanning> environmentTerminated;
    public Action<RLAgentPlanning> agentInitialized;
    public Action allAgentsInitialized;

    public Action envStartedInitialization;
    private float envReward = 0f;
    private float cumulativeRewards;
    public Text canvasScore;
    private string envID;
    private int tempoIniziale;
    private List<GameObject> objectives;
    public bool penaltyTakesTargetsAgain;


    void Start()
    {
        tempoIniziale = (int)Time.time;
        if (canvasScore == null)
        {
            Debug.LogWarning("Canvas non assegnato! Assegna il Canvas nell'Editor.");
            //return;
        }

         Invoke(nameof(InitializeAgents), 0.5f);
    }

    private void Update()
    {
        if (agents != null)
        {
            cumulativeRewards = 0;

            foreach (RLAgentPlanning agent in agents)
            {
                cumulativeRewards += agent.GetCumulativeReward();
            }
            //canvasScore.text = "Score: " + (cumulativeRewards /= agents.Count).ToString();
        }
    }

    public int GetNumIntermediateTargets()
    {
        int count = 0;
        
        // Cerca tutti i GameObject figli che hanno il componente Target
        Target[] targets = GetComponentsInChildren<Target>(includeInactive: true); // Include anche quelli inattivi
        
        foreach (Target target in targets)
        {
            // Conta solo quelli con TargetType.Intermediate
            if (target.targetType == TargetType.Intermediate) // Usa targetType minuscolo come nel tuo codice
            {
                count++;
            }
        }
        
        Debug.Log($"Trovati {count} target intermedi nell'ambiente");
        return count;
    }
    private void InitializeAgents()
    {
        envStartedInitialization?.Invoke();
        envID = Guid.NewGuid().ToString();
        agents = GetComponentsInChildren<RLAgentPlanning>(includeInactive: true).ToList();
        
        InitializeObjectives();

        foreach (RLAgentPlanning agent in agents)
        {
            agent.MakeListenEnvReady(ref agentInitialized);
            print("Agent " + agent.name + " initialized");
            agent.env = this;
            agent.envID = envID;
            agent.envStep = maxSteps;
            agent.agentTerminated += HandleAgentTerminated;
            
            // Attiva l'agente SOLO DOPO che tutto è stato inizializzato
            agent.gameObject.SetActive(true);
            
            // Invoke the event that inform the agent that the environment is ready
            print("Agent " + agent.name + " informed that the environment is ready");
            agentInitialized.Invoke(agent);
        }
        allAgentsInitialized?.Invoke();
    }

    private void InitializeObjectives()
    {
        objectives = new List<GameObject>();
        ObjectiveActivator objectiveActivator = GetComponent<ObjectiveActivator>();
        ObjectiveColorManager colorManager = GetComponent<ObjectiveColorManager>();
        if (colorManager == null)
        {
            colorManager = gameObject.AddComponent<ObjectiveColorManager>();
        }
        colorManager.ClearAllAssignments();
        if (objectiveActivator != null)
        {
            objectives = objectiveActivator.GetActiveObjectives();
            AssignLocalTargetIds();
            if (objectives != null && objectives.Count > 0 && agents != null && agents.Count > 0)
            {
                foreach (var agent in agents)
                {
                    colorManager.RegisterAgentObjectives(agent, objectives);
                }
            }
        }
        else
        {
            Transform[] childTransforms = GetComponentsInChildren<Transform>();
            foreach (Transform child in childTransforms)
            {
                if (child.CompareTag("Obiettivo"))
                {
                    objectives.Add(child.gameObject);
                    Debug.Log($"Obiettivo trovato: {child.name} in ambiente {envID}");
                }
            }
            Debug.Log($"Ambiente {envID}: trovati {objectives.Count} obiettivi");
            AssignLocalTargetIds();
            if (objectiveActivator == null && objectives != null && objectives.Count > 0 && agents != null && agents.Count > 0)
            {
                foreach (var agent in agents)
                {
                    colorManager.RegisterAgentObjectives(agent, objectives);
                }
            }
        }

        // After all objectives are initialized and registered, ensure sensors are updated
        EnsureObjectiveSensorsForAgents();
        Debug.Log($"EnvironmentPlanning {envID} completed objective initialization and updated agent sensors");

        VerifyObjectiveLayers(); // Verifica e imposta i layer degli obiettivi
    }

    public List<GameObject> GetObjectives()
    {
        if (objectives == null || objectives.Count == 0)
        {
            return null;
        }
        return objectives;
    }

    private void HandleAgentTerminated(float agentCumulativeReward, EnvironmentPlanning env)
    {
        agentsTerminated++;
        envReward += agentCumulativeReward;
        if (agents.Count == agentsTerminated)
        {
            envReward /= agents.Count;

            /*environmentTerminated.Invoke(envReward, env);
            StatsWriter.WriteEnvRewards(agents.Count, envReward);
            ResetEpisode();*/
        }
    }

    private void ResetEpisode()
    {
        var colorManager = GetComponent<ObjectiveColorManager>();
        if (colorManager != null)
        {
            colorManager.ClearAllAssignments();
        }

        StatsWriter.WriteEnvTimeStats((int)(Time.time - tempoIniziale));

        agentsTerminated = 0;
        envReward = 0f;
        cumulativeRewards = 0;
        envID = Guid.NewGuid().ToString();
        tempoIniziale = (int)Time.time;

        // Riattiva/Resetta agenti
        foreach (RLAgentPlanning agent in agents)
        {
            agent.envID = envID;
            ObjectiveInteractionHandler handler = agent.GetComponent<ObjectiveInteractionHandler>();
            if (handler != null)
            {
                handler.ResetCompletedObjectives();
                handler.InitializeObjectivesFromEnvironment();
            }
            agent.gameObject.SetActive(true);
        }

        // Attiva nuovi obiettivi DOPO che gli agenti sono pronti
        var activators = GetComponentsInChildren<ObjectiveActivator>();
        if (activators != null && activators.Length > 0)
        {
            foreach (var activator in activators)
            {
                activator.ActivateRandomObjectives();
            }
            Debug.Log($"Ambiente {envID}: delegata attivazione obiettivi a {activators.Length} ObjectiveActivator");
            // Chiamata centralizzata per assegnazione e colorazione
            foreach (var activator in activators)
            {
                activator.NotifyAgentsOfObjectiveChange();
            }
        }
        else
        {
            if (objectives != null)
            {
                foreach (GameObject objective in objectives)
                {
                    if (objective != null)
                    {
                        objective.SetActive(true);
                        Debug.Log($"Obiettivo {objective.name} riattivato");
                    }
                }
                // Aggiorna la lista con i figli attivi
                objectives = new List<GameObject>();
                Transform[] childTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
                foreach (Transform child in childTransforms)
                {
                    if (child.CompareTag("Obiettivo"))
                    {
                        objectives.Add(child.gameObject);
                    }
                }
                // Assegna ID locali ai target intermedi
                AssignLocalTargetIds();
                // Registra nuovamente agenti-obiettivi per aggiornare colori e debug
                if (objectives.Count > 0 && agents != null && agents.Count > 0)
                {
                    var objColorManager = GetComponent<ObjectiveColorManager>();
                    if (objColorManager != null)
                    {
                        foreach (var agent in agents)
                        {
                            objColorManager.RegisterAgentObjectives(agent, objectives);
                        }
                    }
                }
            }
        }

        // At the very end, after all objectives are reactivated
        EnsureObjectiveSensorsForAgents();

        VerifyObjectiveLayers(); // Verifica e imposta i layer degli obiettivi
    }

    private void AssignLocalTargetIds()
    {
        var targets = GetComponentsInChildren<Target>(includeInactive: true)
            .Where(t => t.targetType == TargetType.Intermediate)
            .ToList();
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].id = i;
        }
        Debug.Log($"Ambiente {envID}: assegnati ID locali a {targets.Count} target intermedi");
    }

    public void EnsureObjectiveSensorsForAgents()
    {
        // Make sure agents can see objectives on appropriate layers
        if (agents != null && agents.Count > 0)
        {
            foreach (RLAgentPlanning agent in agents)
            {
                var sensorsManager = agent.GetComponent<AgentPlanningSensorsManager>();
                if (sensorsManager != null)
                {
                    // Force update the sensor vision for objectives
                    sensorsManager.UpdateObjectiveSensorVision(agent.group);
                    Debug.Log($"Updated objective sensors vision for agent {agent.name} (group {agent.group})");
                }
            }
        }
    }

    private void VerifyObjectiveLayers()
    {
        if (objectives != null)
        {
            foreach (GameObject obj in objectives)
            {
                // Get all unique agent groups
                HashSet<Group> uniqueGroups = new HashSet<Group>();
                foreach (RLAgentPlanning agent in agents)
                {
                    uniqueGroups.Add(agent.group);
                }
                
                // Make sure the objective is on the right layer for each group
                foreach (Group group in uniqueGroups)
                {
                    string layerName = group.GetObjectiveLayerName();
                    int layerIndex = LayerMask.NameToLayer(layerName);
                    
                    // Either add to existing layer or set the layer
                    if (layerIndex != -1)
                    {
                        obj.layer = layerIndex;
                        Debug.Log($"Set layer {layerName} for objective {obj.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Layer {layerName} not found for group {group}");
                    }
                }
            }
        }
    }
}