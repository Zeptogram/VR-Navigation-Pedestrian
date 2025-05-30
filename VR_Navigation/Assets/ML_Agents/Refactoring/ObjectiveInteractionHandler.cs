using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * \class ObjectiveInteractionHandler
 * \brief Handles the logic for interacting with objectives in the environment for an RL agent.
 * 
 * This component manages the objectives assigned to the agent, tracks which objectives have been reached,
 * and updates the agent's reward and completion status accordingly.
 */
[RequireComponent(typeof(RLAgent))]
[RequireComponent(typeof(ObjectiveObserver))]
public class ObjectiveInteractionHandler : MonoBehaviour
{
    /// <summary>
    /// Reference to the RLAgent component.
    /// </summary>
    private RLAgent agent;

    /// <summary>
    /// Reference to the ObjectiveObserver component.
    /// </summary>
    private ObjectiveObserver observer;

    /// <summary>
    /// List of objectives that have been reached by the agent.
    /// </summary>
    public List<GameObject> reachedObjectives = new List<GameObject>();

    /// <summary>
    /// List of all objectives assigned to the agent.
    /// </summary>
    public List<GameObject> objectives = new List<GameObject>();

    /**
     * \brief Initializes references to required components.
     */
    private void Awake()
    {
        agent = GetComponent<RLAgent>();
        observer = GetComponent<ObjectiveObserver>();
    }

    /**
     * \brief Handles the logic when the agent triggers an objective.
     * Adds the objective to the reached list, gives a reward, marks it as completed, and checks if all objectives are done.
     * \param triggerObject The objective GameObject that was triggered.
     */
    public void HandleObjectiveTrigger(GameObject triggerObject)
    {
        if (!reachedObjectives.Contains(triggerObject))
        {
            reachedObjectives.Add(triggerObject);
            agent.AddReward(MyConstants.objective_completed_reward);

            observer.MarkObjectiveAsCompleted(triggerObject);
            //triggerObject.SetActive(false); // TODO: make visible again if needed

            if (reachedObjectives.Count == objectives.Count && objectives.Count > 0)
            {
                agent.taskCompleted = true;
                observer.SetTaskCompleted();
                Debug.Log($"Agent {agent.name} has completed all objectives!");
            }
        }
    }

    /**
     * \brief Checks if a GameObject is a valid objective for this agent.
     * \param triggerObject The GameObject to check.
     * \return True if the object is a valid objective, false otherwise.
     */
    public bool IsValidObjective(GameObject triggerObject)
    {
        return triggerObject.CompareTag("Obiettivo") && objectives.Contains(triggerObject);
    }

    /**
     * \brief Sets the list of objectives for the agent and initializes them in the observer.
     * \param newObjectives The list of new objectives.
     */
    public void SetObjectives(List<GameObject> newObjectives)
    {
        objectives = new List<GameObject>(newObjectives ?? new List<GameObject>());
        reachedObjectives.Clear();

        observer.InitializeObjectives(objectives);

        Debug.Log($"Agent {agent.name}: set {objectives.Count} objectives");
    }

    /**
     * \brief Initializes objectives from the environment, if available.
     */
    public void InitializeObjectivesFromEnvironment()
    {
        if (agent.env != null)
        {
            List<GameObject> envObjectives = agent.env.GetObjectives();
            SetObjectives(envObjectives);
        }
    }

    /**
     * \brief Resets the list of completed objectives and updates the observer.
     */
    public void ResetCompletedObjectives()
    {
        reachedObjectives.Clear();

        if (observer != null)
        {
            observer.SetTaskCompleted();
        }
    }

    /// <summary>
    /// Returns the percentage of objectives that have not been reached yet.
    /// </summary>
    /// <returns>The percentage (0-1) of remaining objectives, or 0 if there are no objectives.</returns>
    public float GetRemainingObjectivesPercentage()
    {
        if (objectives.Count == 0)
            return 0f;
        //Debug.Log($"Agent {agent.name} remaining objectives: {objectives.Count - reachedObjectives.Count} / {objectives.Count}");
        return (float)(objectives.Count - reachedObjectives.Count) / objectives.Count;
    }
}