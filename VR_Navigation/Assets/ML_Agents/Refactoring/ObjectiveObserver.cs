using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * \class ObjectiveObserver
 * \brief Observes and manages the state of objectives for an RL agent.
 * 
 * This component tracks which objectives are active or completed and provides an observation array
 * representing the current state of objectives for the agent.
 */
public class ObjectiveObserver : MonoBehaviour
{
    /// <summary>
    /// Reference to the RLAgent component.
    /// </summary>
    private RLAgent agent;
    /// <summary>
    /// Array representing the observation state of objectives (1 = active, 0 = inactive).
    /// The last element is used as a flag for all objectives completed.
    /// </summary>
    [SerializeField] private float[] objectivesObservation = new float[10]; // default to 0

    /**
     * \brief Initializes the observer and sets the completed flag if no objectives are present.
     */
    private void Awake()
    {
        agent = GetComponent<RLAgent>();
        // Initialize the last element to 1 (flag for all completed)
        objectivesObservation[objectivesObservation.Length - 1] = 1;
    }

    /**
     * \brief Initializes the objectives observation array based on the provided objectives list.
     * Sets the agent's taskCompleted flag accordingly.
     * \param objectives List of objective GameObjects.
     */
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

    /**
     * \brief Returns the current objectives observation array.
     * \return Array of floats representing objectives state.
     */
    public float[] GetObjectivesObservation()
    {
        return objectivesObservation;
    }

    /**
     * \brief Marks an objective as completed in the observation array.
     * \param objective The completed objective GameObject.
     */
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
    }

    /**
     * \brief Sets the last element of the observation array to 1, indicating all objectives are completed.
     */
    public void SetTaskCompleted()
    {
        objectivesObservation[objectivesObservation.Length - 1] = 1;
    }
}