using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObserver : MonoBehaviour
{
    private RLAgentPlanning agent;
    
    [SerializeField] private float[] objectivesObservation;

    private void Awake()
    {
        agent = GetComponent<RLAgentPlanning>();
        
        AutoSetGlobalArraySize();
        
        Debug.Log($"ObjectiveObserver awakened for {agent.gameObject.name} with global size {objectivesObservation.Length}");
    }

    /// <summary>
    /// Automatically sets array size based on ALL objectives in the environment + final target
    /// </summary>
    private void AutoSetGlobalArraySize()
    {
        GameObject[] allObjectives = GameObject.FindObjectsOfType<GameObject>();
        
        int maxObjectiveIndex = -1;
        
        foreach (GameObject obj in allObjectives)
        {
            if (IsObjectiveGameObject(obj))
            {
                int index = GetObjectiveIndexFromName(obj.name);
                if (index > maxObjectiveIndex && index != -1)
                {
                    maxObjectiveIndex = index;
                }
            }
        }

        // Global size = max objective index + 2 (+1 for 0-based, +1 for final target)
        int globalSize = Mathf.Max(5, maxObjectiveIndex + 2); // Minimum 5 as fallback
        objectivesObservation = new float[globalSize];
        
        // Initialize all to 0
        for (int i = 0; i < objectivesObservation.Length; i++)
            objectivesObservation[i] = 0;
            
        Debug.Log($"[ObjectiveObserver] Global array size set to {globalSize} (max objective index found: {maxObjectiveIndex})");
    }

    /// <summary>
    /// Checks if a GameObject is an objective (customize this logic based on your naming/tagging)
    /// </summary>
    private bool IsObjectiveGameObject(GameObject obj)
    {
        return obj.CompareTag("Obiettivo");
    }

    public void InitializeObjectives(List<GameObject> objectives)
    {
        Debug.Log($"[{agent.gameObject.name}] Initializing objectives with {objectives?.Count ?? 0} objectives.");

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
            if (objective != null)
            {
                int index = GetObjectiveIndexFromName(objective.name);

                if (index >= 0 && index < objectivesObservation.Length - 1) // Don't overwrite final target slot
                {
                    objectivesObservation[index] = 1;
                    Debug.Log($"Objective {objective.name} initialized with index {index}");
                }
                else if (index == objectivesObservation.Length - 1)
                {
                    Debug.Log($"Final target {objective.name} detected but not set in observation (handled separately)");
                }
            }
        }

        agent.taskCompleted = false;
        objectivesObservation[objectivesObservation.Length - 1] = 0; // Final target initially not available
    }

    /**
     * \brief Extracts objective index from GameObject name.
     * \param objectiveName The name of the objective GameObject.
     * \return The index of the objective, or -1 if not found.
     */
    private int GetObjectiveIndexFromName(string objectiveName)
    {
        // Special case for final target names
        if (objectiveName.ToLower().Contains("final") || objectiveName.ToLower().Contains("fina"))
        {
            return objectivesObservation.Length - 1; // Always goes to last position
        }

        // Try to parse from name (e.g., "Objective (2)")
        if (objectiveName.Contains("(") && objectiveName.Contains(")"))
        {
            string indexStr = objectiveName.Split('(', ')')[1];
            if (int.TryParse(indexStr, out int index))
            {
                // Handle legacy -1 index for final target
                if (index == -1)
                {
                    return objectivesObservation.Length - 1; // Map to last position
                }
                return index;
            }
        }

        Debug.LogWarning($"Could not determine index for objective: {objectiveName}");
        return -1;
    }

    public float[] GetObjectivesObservation()
    {
        return objectivesObservation;
    }

    /// <summary>
    /// Returns the global array size (used by DirectionsObjectives for padding)
    /// </summary>
    public static int GetGlobalArraySize()
    {
        // Find any ObjectiveObserver in the scene and get its size
        ObjectiveObserver observer = FindObjectOfType<ObjectiveObserver>();
        if (observer != null && observer.objectivesObservation != null)
        {
            return observer.objectivesObservation.Length;
        }
        
        // Fallback: calculate manually
        GameObject[] allObjectives = GameObject.FindObjectsOfType<GameObject>();
        int maxIndex = -1;
        
        foreach (GameObject obj in allObjectives)
        {
            if (obj.name.ToLower().Contains("objective") || obj.name.ToLower().Contains("final"))
            {
                string objName = obj.name;
                if (objName.Contains("(") && objName.Contains(")"))
                {
                    string indexStr = objName.Split('(', ')')[1];
                    if (int.TryParse(indexStr, out int index) && index > maxIndex)
                    {
                        maxIndex = index;
                    }
                }
            }
        }
        
        return Mathf.Max(5, maxIndex + 2);
    }

    public void MarkObjectiveAsCompleted(GameObject objective)
    {
        int index = GetObjectiveIndexFromName(objective.name);
        
        if (index >= 0 && index < objectivesObservation.Length - 1) // Regular objective
        {
            objectivesObservation[index] = 0;
            Debug.Log($"Objective {objective.name} completed at index {index}");
            
            CheckIfAllObjectivesCompleted();
        }
        else if (index == objectivesObservation.Length - 1) // Final target
        {
            Debug.Log($"Final target {objective.name} reached");
            // Final target completion is handled elsewhere
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
        // Check if all objectives are marked as completed (0) except the last one (final target)
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
            objectivesObservation[objectivesObservation.Length - 1] = 1; // Enable final target
            Debug.Log($"[{agent.gameObject.name}] All {objectivesObservation.Length - 1} objectives completed! Final target enabled.");
        }
        else
        {
            // Objectives still active
            agent.taskCompleted = false;
            objectivesObservation[objectivesObservation.Length - 1] = 0; // Keep final target disabled
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
            Debug.Log($"[{agent.gameObject.name}] Task completed manually (all {objectivesObservation.Length - 1} objectives were already completed)");
        }
        else
        {
            Debug.LogWarning($"[{agent.gameObject.name}] Attempted to set task completed but there are still active objectives!");
        }
    }
}