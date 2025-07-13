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
    public Action<RLAgent> agentInizialized;
    private float envReward = 0f;
    private float cumulativeRewards;
    public Text canvasScore;
    private string envID;
    private int tempoIniziale;
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
        envID = Guid.NewGuid().ToString();
        agents = GetComponentsInChildren<RLAgent>(includeInactive: true).ToList();
        foreach (RLAgent agent in agents)
        {
            //agentInizialized.Invoke(agent);
            agent.env = this;
            agent.envID = envID;
            agent.envStep = maxSteps;
            agent.agentTerminated += HandleAgentTerminated;
            agent.gameObject.SetActive(true);
        }
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

        foreach (RLAgent agent in agents)
        {
            agent.envID = envID;
            agent.gameObject.SetActive(true);
        }
    }
}