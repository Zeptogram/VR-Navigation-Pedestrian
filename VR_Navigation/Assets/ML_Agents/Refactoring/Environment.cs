using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;

public class Environment : MonoBehaviour
{
    // Start is called before the first frame update
    private List<RLAgent> agents;
    private int agentsTerminated = 0;
    [SerializeField] int maxSteps;

    public Action<float, Environment> environmentTerminated;
    public Action<RLAgent> agentInitialized;
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
            Debug.LogError("Canvas non assegnato! Assegna il Canvas nell'Editor.");
            return;
        }
        Invoke(nameof(InitializeAgents), 0.5f);
    }

    private void Update()
    {
        if (agents != null)
        {
            cumulativeRewards = 0;

            foreach (RLAgent agent in agents)
            {
                cumulativeRewards += agent.GetCumulativeReward();
            }
            canvasScore.text = "Score: " + (cumulativeRewards /= agents.Count).ToString();
        }
    }


    private void InitializeAgents()
    {
        envStartedInitialization?.Invoke();
        envID = Guid.NewGuid().ToString();
        agents = GetComponentsInChildren<RLAgent>(includeInactive: true).ToList();
        
        InitializeObjectives();

        foreach (RLAgent agent in agents)
        {
            agent.MakeListenEnvReady(ref agentInitialized);
            print("Agent " + agent.name + " initialized");
            agent.env = this;
            agent.envID = envID;
            agent.envStep = maxSteps;
            agent.agentTerminated += HandleAgentTerminated;
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
        if (objectiveActivator != null)
        {
            objectives = objectiveActivator.GetActiveObjectives();
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
            ObjectiveColorManager colorManager = GetComponent<ObjectiveColorManager>();
            if (colorManager == null)
            {
                colorManager = gameObject.AddComponent<ObjectiveColorManager>();
            }
            colorManager.ClearAllAssignments();

            if (objectiveActivator == null && objectives != null && objectives.Count > 0 && agents != null && agents.Count > 0)
            {
                foreach (var agent in agents)
                {
                    colorManager.RegisterAgentObjectives(agent, objectives);
                }
            }
        }
    }

    public List<GameObject> GetObjectives()
    {
        if (objectives == null || objectives.Count == 0)
        {
            return null;
        }
        return objectives;
    }

    private void HandleAgentTerminated(float agentCumulativeReward, Environment env)
    {
        agentsTerminated++;
        envReward += agentCumulativeReward;
        if (agents.Count == agentsTerminated)
        {
            envReward /= agents.Count;

            environmentTerminated.Invoke(envReward, env);
            StatsWriter.WriteEnvRewards(agents.Count, envReward);
            ResetEpisode();
        }
    }

    private void ResetEpisode()
    {
        StatsWriter.WriteEnvTimeStats((int)(Time.time - tempoIniziale));

        agentsTerminated = 0;
        envReward = 0f;
        cumulativeRewards = 0;
        envID = Guid.NewGuid().ToString();
        tempoIniziale = (int)Time.time;

        // Riattiva/Resetta agenti
        foreach (RLAgent agent in agents)
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
            }
        }
    }
}