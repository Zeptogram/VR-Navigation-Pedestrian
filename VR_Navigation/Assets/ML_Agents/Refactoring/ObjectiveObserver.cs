using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObserver : MonoBehaviour
{
    private RLAgent agent;
    [SerializeField] private float[] objectivesObservation = new float[10]; // default to 0

    private void Awake()
    {
        agent = GetComponent<RLAgent>();
        // Initialize the last element to 1 (flag for all completed)
        objectivesObservation[objectivesObservation.Length - 1] = 1;
        Debug.Log($"ObjectiveObserver awakened for {agent.gameObject.name}");

    }

    public void InitializeObjectives(List<GameObject> objectives)
    {
        // Reset all elements to 0
        for (int i = 0; i < objectivesObservation.Length; i++)
            objectivesObservation[i] = 0;

        // Handle the case where there are no objectives
        if (objectives == null || objectives.Count == 0)
        {
            agent.taskCompleted = true;
            objectivesObservation[objectivesObservation.Length - 1] = 1; // Set last element to 1 (completed)
            return;
        }

        // Set indicators for active objectives
        foreach (GameObject objective in objectives)
        {
            if (int.TryParse(objective.name.Split('(', ')')[1], out int index))
            {
                if (index < objectivesObservation.Length - 1)
                {
                    objectivesObservation[index] = 1;
                    Debug.Log($"Objective {objective.name} initialized with index {index}");
                }
            }
        }

        // If there are objectives, the agent has tasks to complete
        agent.taskCompleted = false;
        // The last element remains 0 (incomplete)
    }

    public float[] GetObjectivesObservation()
    {
        return objectivesObservation;
    }

    public void MarkObjectiveAsCompleted(GameObject objective)
    {
        if (int.TryParse(objective.name.Split('(', ')')[1], out int index))
        {
            if (index < objectivesObservation.Length - 1)
            {
                objectivesObservation[index] = 0;
                Debug.Log($"Objective {objective.name} completed");
            }
        }
        
        // Gestione colore (se presente)
        if (agent != null && agent.env != null)
        {
            var colorManager = agent.env.GetComponent<ObjectiveColorManager>();
            if (colorManager != null)
            {
                colorManager.UnregisterAgentFromObjective(agent, objective);
            }
        }
    }

    public void SetTaskCompleted()
    {
        objectivesObservation[objectivesObservation.Length - 1] = 1;
    }
}