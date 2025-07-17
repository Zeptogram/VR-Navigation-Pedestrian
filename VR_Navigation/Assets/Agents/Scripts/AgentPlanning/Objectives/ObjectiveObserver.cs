using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObserver : MonoBehaviour
{
    private RLAgentPlanning agent;
    [SerializeField] private float[] objectivesObservation = new float[10]; // default to 0

    private void Awake()
    {
        agent = GetComponent<RLAgentPlanning>();
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

        agent.taskCompleted = false;
        objectivesObservation[objectivesObservation.Length - 1] = 0;
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
                
                CheckIfAllObjectivesCompleted();
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

    /// <summary>
    /// Checks if all objectives are completed and updates the task status.
    /// </summary>
    private void CheckIfAllObjectivesCompleted()
    {
        // Check if all objectives are marked as completed (0) except the last one
        bool allCompleted = true;
        for (int i = 0; i < objectivesObservation.Length - 1; i++)
        {
            if (objectivesObservation[i] == 1)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            // All objectives are completed
            agent.taskCompleted = true;
            objectivesObservation[objectivesObservation.Length - 1] = 1; // Set finale a 1
            Debug.Log($"[{agent.gameObject.name}] Tutti gli obiettivi completati! Task finale abilitato.");
        }
        else
        {
            // Objectives still active
            agent.taskCompleted = false;
            objectivesObservation[objectivesObservation.Length - 1] = 0; // Mantieni finale a 0
        }
    }

    /// <summary>
    /// Method to manually set the task as completed.
    /// </summary>
    public void SetTaskCompleted()
    {
        // Check if all objectives are already completed
        bool allCompleted = true;
        for (int i = 0; i < objectivesObservation.Length - 1; i++)
        {
            if (objectivesObservation[i] == 1)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            agent.taskCompleted = true;
            objectivesObservation[objectivesObservation.Length - 1] = 1;
            Debug.Log($"[{agent.gameObject.name}] Task completato manualmente (tutti gli obiettivi erano già a 0)");
        }
        else
        {
            Debug.LogWarning($"[{agent.gameObject.name}] Tentativo di settare task completato ma ci sono ancora obiettivi attivi!");
        }
    }
}