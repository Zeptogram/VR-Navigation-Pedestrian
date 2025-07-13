using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * \class ObjectiveInteractionHandler
 * \brief Handles the logic for interacting with objectives in the environment for an RL agent.
 * 
 * This component manages the objectives assigned to the agent, tracks which objectives have been reached,
 * and updates the agent's reward and completion status accordingly.
 */
[RequireComponent(typeof(ObjectiveObserver))]
public class ObjectiveInteractionHandler : MonoBehaviour
{
    /// <summary>
    /// Reference to the RLAgentPlanning component.
    /// </summary>
    private RLAgentPlanning agent;

    /// <summary>
    /// Reference to the ObjectiveObserver component.
    /// </summary>
    private ObjectiveObserver observer;

    /// <summary>
    /// If true, objectives must be completed in the specified order.
    /// </summary>
    [Header("Objective Order")]
    public bool orderedObjectives = false;

    /// <summary>
    /// List of all objectives assigned to the agent.
    /// </summary>
    public List<GameObject> objectives = new List<GameObject>();

    /// <summary>
    /// List of objectives that have been reached by the agent.
    /// </summary>
    public List<GameObject> reachedObjectives = new List<GameObject>();

    /// <summary>
    /// Reference to the agent's animation manager.
    /// </summary>
    private AgentAnimationManager animationManager;

    /// <summary>
    /// Indicates if the agent is currently executing objective animations.
    /// </summary>
    private bool isExecutingObjectiveAnimations = false;

    /**
     * \brief Initializes references to required components.
     */
    private void Awake()
    {
        agent = GetComponent<RLAgentPlanning>();
        observer = GetComponent<ObjectiveObserver>();
        animationManager = GetComponent<AgentAnimationManager>();
    }

    /**
     * \brief Handles the logic when the agent triggers an objective.
     * Adds the objective to the reached list, gives a reward, marks it as completed, and checks if all objectives are done.
     * \param triggerObject The objective GameObject that was triggered.
     */
    public void HandleObjectiveTrigger(GameObject triggerObject)
    {
        // If order is on, accept only the next objective in the list
        if (orderedObjectives)
        {
            int nextIndex = reachedObjectives.Count;
            if (nextIndex >= objectives.Count || objectives[nextIndex] != triggerObject)
            {
                Debug.LogWarning($"[ORDERED OBJECTIVES] Tried to take {triggerObject.name}, but next should be {objectives[nextIndex].name}");
                return;
            }
        }
        else
        {
            // If alredy taken, ignore
            if (reachedObjectives.Contains(triggerObject))
                return;
        }

        reachedObjectives.Add(triggerObject);
        agent.AddReward(agent.constants.objective_completed_reward);

        observer.MarkObjectiveAsCompleted(triggerObject);
        //triggerObject.SetActive(false); // TODO: make visible again if needed

        // Run the animations associated with the objective
        StartCoroutine(ExecuteObjectiveAnimations(triggerObject));

        if (reachedObjectives.Count == objectives.Count && objectives.Count > 0)
        {
            agent.taskCompleted = true;
            observer.SetTaskCompleted();
            Debug.Log($"Agent {agent.name} has completed all objectives!");
        }
    }

    /**
     * \brief Executes the animations associated with the reached objective.
     * \param objectiveObject The objective GameObject that contains animation data.
     */
    private IEnumerator ExecuteObjectiveAnimations(GameObject objectiveObject)
    {
        ObjectiveAnimationData animationData = objectiveObject.GetComponent<ObjectiveAnimationData>();
        
        if (animationData == null)
        {
            Debug.LogWarning($"No ObjectiveAnimationData found on objective {objectiveObject.name}. Skipping animations.");
            yield break;
        }

        isExecutingObjectiveAnimations = true;
        Debug.Log($"[OBJECTIVE ANIMATION] Starting objective animations for {objectiveObject.name}");

        // If setted to stop the agent during animations
        if (animationData.stopAgentDuringAnimations)
        {
            agent.SetRun(false);
            agent.GetRigidBody().velocity = Vector3.zero;
            
            // Wait a frame to ensure the agent is stopped
            yield return null;
            
            // Stop any ongoing animations
            if (animationManager != null)
            {
                animationManager.SetWalking(false);  
                animationManager.UpdateSpeed(0f);     
            }
            
            // Wait a moment to ensure the agent is fully stopped
            yield return new WaitForSeconds(0.1f);
        }

        // If total duration is set, use it
        if (animationData.totalDuration > 0)
        {
            yield return ExecuteAnimationsWithTotalDuration(animationData);
        }
        else
        {
            // Else individually execute animations
            if (animationData.playInSequence)
            {
                yield return ExecuteAnimationsInSequence(animationData);
            }
            else
            {
                yield return ExecuteAnimationsInParallel(animationData);
            }
        }

        Debug.Log("[OBJECTIVE ANIMATION] All objective animations completed, preparing to reactivate agent");

        // Reset the animator if needed
        if (animationData.stopAgentDuringAnimations && animationManager != null)
        {
            // Reset the animator to idle state
            animationManager.ResetToIdleState();
            
            // Wait a frame to ensure the animator is reset
            yield return null;
            
            Debug.Log("[OBJECTIVE ANIMATION] Animator reset completed, reactivating agent");
        }

        // Toggle the state of isExecutingObjectiveAnimations
        // This is to ensure that the agent can continue its normal operations after animations
        isExecutingObjectiveAnimations = false;

        // Reactivate the agent if it was stopped during animations
        if (animationData.stopAgentDuringAnimations)
        {
            agent.SetRun(true);
            Debug.Log("[OBJECTIVE ANIMATION] Agent reactivated after animations completed");
            
            // Wait a moment before allowing further actions
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            isExecutingObjectiveAnimations = false;
        }

        Debug.Log($"[OBJECTIVE ANIMATION] Completed all animations for objective {objectiveObject.name}");
    }

    /**
     * \brief Executes animations in sequence with individual durations.
     */
    private IEnumerator ExecuteAnimationsInSequence(ObjectiveAnimationData animationData)
    {
        foreach (var action in animationData.animationActions)
        {
            if (animationManager != null && !string.IsNullOrEmpty(action.animationTrigger))
            {
                Debug.Log($"[OBJECTIVE ANIMATION] Setting animation: {action.animationTrigger} for {action.duration} seconds");
                
                // Start the coroutine to maintain the animation for the specified duration
                StartCoroutine(MaintainAnimationForDuration(action.animationTrigger, action.duration));
                
                Debug.Log($"[OBJECTIVE ANIMATION] Animation {action.animationTrigger} set, waiting {action.duration} seconds...");
            }
            
            // Wait for the duration of the animation
            yield return new WaitForSeconds(action.duration);
            Debug.Log($"[OBJECTIVE ANIMATION] Finished waiting for {action.animationTrigger}");
            
            // Delay between animations if specified
            if (animationData.delayBetweenAnimations > 0)
            {
                Debug.Log($"[OBJECTIVE ANIMATION] Waiting delay: {animationData.delayBetweenAnimations} seconds");
                yield return new WaitForSeconds(animationData.delayBetweenAnimations);
            }
        }
        
        Debug.Log("[OBJECTIVE ANIMATION] All animations in sequence completed");
    }

    /**
     * \brief Maintains an animation active for a specified duration.
     */
    private IEnumerator MaintainAnimationForDuration(string animationTrigger, float duration)
    {
        // Trigger the animation
        SetAnimationTrigger(animationTrigger);
        Debug.Log($"[OBJECTIVE ANIMATION] Started {animationTrigger} for {duration} seconds");
        
        float elapsedTime = 0f;
        
        // If bool (isWalking, isIdle), keep it active
        if (animationTrigger == "isWalking" || animationTrigger == "isIdle")
        {
            while (elapsedTime < duration)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
                
                // For "isWalking" and "isIdle", toggle every 2 seconds (a refresh)
                if (Mathf.FloorToInt(elapsedTime) % 2 == 0 && elapsedTime % 2 < 0.1f)
                {
                    SetAnimationTrigger(animationTrigger);
                }
            }
        }
        // For triggers
        else
        {
            float animationDuration = GetEstimatedAnimationDuration(animationTrigger);
            float nextTriggerTime = animationDuration;
            
            while (elapsedTime < duration)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
                
                // Restart the animation trigger every estimated duration
                if (elapsedTime >= nextTriggerTime)
                {
                    SetAnimationTrigger(animationTrigger);
                    nextTriggerTime += animationDuration;
                    Debug.Log($"[OBJECTIVE ANIMATION] Restarting {animationTrigger} at {elapsedTime:F2}s (next at {nextTriggerTime:F2}s)");
                }
            }
            
            // Reset the turn animations if they were used
            if (animationTrigger == "TurnRight" || animationTrigger == "TurnLeft")
            {
                animationManager.StopTurn();
                Debug.Log($"[OBJECTIVE ANIMATION] Stopped {animationTrigger} after {duration} seconds");
            }
            // Reset triggers (generic)
            if (animationTrigger == "TurnRight" || animationTrigger == "TurnLeft" || 
                (!animationTrigger.StartsWith("is"))) // Per tutti i trigger che non sono parametri bool
            {
                animationManager.ResetAllTriggers();
                Debug.Log($"[OBJECTIVE ANIMATION] Reset all triggers after {animationTrigger} completed");
            }
        }
        
        Debug.Log($"[OBJECTIVE ANIMATION] Finished maintaining {animationTrigger} for {duration} seconds");
    }

    /**
     * \brief Returns the estimated duration of an animation in seconds.
     */
    private float GetEstimatedAnimationDuration(string animationTrigger)
    {
        switch (animationTrigger)
        {
            case "TurnRight":
            case "TurnLeft":
                return 1.0f; // Durata tipica di una rotazione (1 secondo)
            default:
                return 2.0f; // Durata di default
        }
    }

    /**
     * \brief Helper method to set animation triggers consistently.
     */
    private void SetAnimationTrigger(string animationTrigger)
    {
        if (animationManager == null) return;
        
        if (animationTrigger == "isWalking")
        {
            animationManager.SetWalking(true);
        }
        else if (animationTrigger == "isIdle")
        {
            animationManager.SetWalking(false);
        }
        else if (animationTrigger == "TurnRight")
        {
            animationManager.PlayTurn(true);
        }
        else if (animationTrigger == "TurnLeft")
        {
            animationManager.PlayTurn(false);
        }
        else
        {
            animationManager.PlayActionTrigger(animationTrigger);
        }
    }

    /**
     * \brief Executes animations using a total duration instead of individual durations.
     */
    private IEnumerator ExecuteAnimationsWithTotalDuration(ObjectiveAnimationData animationData)
    {
        if (animationData.animationActions.Length == 0)
        {
            yield return new WaitForSeconds(animationData.totalDuration);
            yield break;
        }

        float timePerAnimation = animationData.totalDuration / animationData.animationActions.Length;
        
        foreach (var action in animationData.animationActions)
        {
            if (animationManager != null && !string.IsNullOrEmpty(action.animationTrigger))
            {
                Debug.Log($"Attempting to play animation: {action.animationTrigger} for {timePerAnimation} seconds");
                
                // Start the coroutine to maintain the animation for the specified duration
                StartCoroutine(MaintainAnimationForDuration(action.animationTrigger, timePerAnimation));
                
                Debug.Log($"Animation trigger {action.animationTrigger} set successfully");
            }
            
            yield return new WaitForSeconds(timePerAnimation);
        }
    }

    /**
     * \brief Executes animations in parallel (all at once) with individual durations.
     */
    private IEnumerator ExecuteAnimationsInParallel(ObjectiveAnimationData animationData)
    {
        // Probably to remove, won't be used in parallel
        float maxDuration = 0f;
        
        // Max duration
        foreach (var action in animationData.animationActions)
        {
            if (action.duration > maxDuration)
                maxDuration = action.duration;
        }
        
        // Run all animations in parallel
        foreach (var action in animationData.animationActions)
        {
            if (animationManager != null && !string.IsNullOrEmpty(action.animationTrigger))
            {
                StartCoroutine(MaintainAnimationForDuration(action.animationTrigger, action.duration));
                Debug.Log($"Playing animation in parallel: {action.animationTrigger} for {action.duration} seconds");
            }
        }
        
        // Wait max duration for all animations to finish
        yield return new WaitForSeconds(maxDuration);
    }

    /**
     * \brief Returns true if the agent is currently executing objective animations.
     */
    public bool IsExecutingObjectiveAnimations()
    {
        return isExecutingObjectiveAnimations;
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
        objectives = newObjectives ?? new List<GameObject>();
        reachedObjectives.Clear();
        
        // Initialize the observer with the new objectives
        if (observer != null)
        {
            observer.InitializeObjectives(objectives);
            Debug.Log($"Set {objectives.Count} objectives for agent {agent.gameObject.name}");
        }
    }

    /**
     * \brief Initializes objectives from the environment, if available.
     */
    public void InitializeObjectivesFromEnvironment()
    {
        if (agent.env != null)
        {
            // Obtain the objectives from the environment
            List<GameObject> envObjectives = agent.env.GetObjectives(); 
            
            if (envObjectives != null && envObjectives.Count > 0)
            {
                SetObjectives(envObjectives);
                Debug.Log($"Initialized {envObjectives.Count} objectives from environment for agent {agent.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"No objectives found in environment for agent {agent.gameObject.name}");
                // Initialize with an empty list if no objectives found
                SetObjectives(new List<GameObject>());
            }
        }
        else
        {
            Debug.LogError($"Agent {agent.gameObject.name} has no environment reference");
        }
    }

    /**
     * \brief Resets the list of completed objectives and updates the observer.
     */
    public void ResetCompletedObjectives()
    {
        reachedObjectives.Clear();
        isExecutingObjectiveAnimations = false;

        if (observer != null)
        {
            observer.SetTaskCompleted();
        }
    }

    /**
     * \brief Returns the percentage of objectives that have been reached.
     * 
     * This method calculates the percentage of objectives that have been reached by the agent.
     * If there are no objectives, it returns 0.
     * 
     * \return The percentage (0-1) of reached objectives, or 0 if there are no objectives.
     */
    public float GetRemainingObjectivesPercentage()
    {
        if (objectives.Count == 0)
            return 0f;
        //Debug.Log($"Agent {agent.name} remaining objectives: {objectives.Count - reachedObjectives.Count} / {objectives.Count}");
        return (float)(objectives.Count - reachedObjectives.Count) / objectives.Count;
    }

    /**
     * \brief Returns the list of objectives that have not been reached yet.
     * \return List of remaining objectives.
     */
    public List<GameObject> GetRemainingObjectives()
    {
        List<GameObject> remaining = new List<GameObject>();
        
        foreach (GameObject objective in objectives)
        {
            if (!reachedObjectives.Contains(objective))
            {
                remaining.Add(objective);
            }
        }
        
        return remaining;
    }

    /**
     * \brief Returns the total number of objectives assigned to this agent.
     * \return Total count of objectives.
     */
    public int GetTotalObjectivesCount()
    {
        return objectives.Count;
    }

    /**
     * \brief Returns the number of objectives that have been reached.
     * \return Count of reached objectives.
     */
    public int GetReachedObjectivesCount()
    {
        return reachedObjectives.Count;
    }

    /**
     * \brief Checks if a specific objective is currently available for interaction.
     * This method considers the order of objectives if orderedObjectives is true.
     * \param obj The GameObject to check.
     * \return True if the objective is available, false otherwise.
     */
    public bool IsObjectiveCurrentlyAvailable(GameObject obj)
    {
        if (orderedObjectives)
        {
            int nextIndex = reachedObjectives.Count;
            return (nextIndex < objectives.Count && objectives[nextIndex] == obj);
        }
        else
        {
            return !reachedObjectives.Contains(obj) && objectives.Contains(obj);
        }
    }
}