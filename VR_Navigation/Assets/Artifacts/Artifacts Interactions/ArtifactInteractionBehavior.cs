/* ArtifactInteractionBehavior.cs
    This script handles the interaction behavior of artifacts in the game.
    Assigned to artifacts.
    Called right before an agent uses an artifact, it manages the interaction process,
    including animations, stopping the agent, and checking if the interaction is one-time use.
*/



using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArtifactInteractionBehavior : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDuration = 2f;
    [SerializeField] private bool stopAgentDuringInteraction = true;
    [SerializeField] private bool oneTimeUsePerAgent = false; 
    
    [Header("Animation Settings")]
    [SerializeField] private string animationTrigger = "Point";
    [SerializeField] private bool playAnimation = true;
    
    [Header("Look At Settings")]
    [SerializeField] private bool enableLookAt = false;
    [SerializeField] private Transform lookAtTarget = null;
    [SerializeField] private float lookAtSpeed = 90f;
    
    [Header("Debug")]
    [SerializeField] private bool debugging = false;
    
    private bool isInteracting = false;
    
    // Track which agents have already used this interaction behavior
    private HashSet<int> usedByAgents = new HashSet<int>();
    
    /// <summary>
    /// Called by ArtifactNavigationHandler when agent reaches the artifact
    /// </summary>
    public void StartInteraction(GameObject agent, System.Action onInteractionComplete)
    {
        if (isInteracting) 
        {
            if (debugging)
                Debug.Log($"[ArtifactInteractionBehavior] Already interacting - skipping for {agent.name}");
            onInteractionComplete?.Invoke();
            return;
        }
        
        // Check if this agent has already used the artifact, skip
        int agentId = agent.GetInstanceID();
        if (oneTimeUsePerAgent && usedByAgents.Contains(agentId))
        {
            if (debugging)
                Debug.Log($"[ArtifactInteractionBehavior] Agent {agent.name} has already used this interaction - skipping");
            
            onInteractionComplete?.Invoke();
            return;
        }
        
        StartCoroutine(HandleInteraction(agent, onInteractionComplete));
    }
    
    private IEnumerator HandleInteraction(GameObject agent, System.Action onInteractionComplete)
    {
        isInteracting = true;
        
        // For one time user per agent, track usage
        int agentId = agent.GetInstanceID();
        if (oneTimeUsePerAgent)
        {
            usedByAgents.Add(agentId);
            if (debugging)
                Debug.Log($"[ArtifactInteractionBehavior] Marked agent {agent.name} as having used this interaction");
        }
        
        if (debugging)
            Debug.Log($"[ArtifactInteractionBehavior] Starting interaction for {agent.name}");
        
        // Components
        UnityEngine.AI.NavMeshAgent navAgent = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
        RLAgentPlanning rlAgent = agent.GetComponent<RLAgentPlanning>();
        Animator animator = agent.GetComponent<Animator>();
        
        // Stop agent if the boolean is true
        if (stopAgentDuringInteraction && navAgent != null)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
            navAgent.isStopped = true;
        }
        
        // Look at (should be ok to not use it)
        if (enableLookAt && lookAtTarget != null)
        {
            yield return StartCoroutine(LookAtTarget(agent.transform));
        }
        
        // Animation trigger (animatior has to set the transitions for the triggers etc.)
        if (playAnimation && !string.IsNullOrEmpty(animationTrigger) && animator != null)
        {
            if (debugging)
                Debug.Log($"[ArtifactInteractionBehavior] Playing animation trigger: {animationTrigger}");
            
            animator.SetTrigger(animationTrigger);
        }
        
        // Wait for interaction duration
        yield return new WaitForSeconds(interactionDuration);
        
        // Resume agent movement
        if (stopAgentDuringInteraction && navAgent != null)
        {
            navAgent.isStopped = false;
        }
        
        if (debugging)
            Debug.Log($"[ArtifactInteractionBehavior] Interaction completed for {agent.name}");
        
        isInteracting = false;
        
        // Notify completion
        onInteractionComplete?.Invoke();
    }
    
    /// <summary>
    /// Checks if a specific agent has already used this interaction 
    /// </summary>
    public bool HasAgentUsedInteraction(GameObject agent)
    {
        return oneTimeUsePerAgent && usedByAgents.Contains(agent.GetInstanceID());
    }
    
    /// <summary>
    /// Coroutine to make the agent look at the target smoothly
    /// </summary>
    /// <param name="agentTransform">The transform of the agent</param>
    private IEnumerator LookAtTarget(Transform agentTransform)
    {
        if (lookAtTarget == null) yield break;

        Vector3 targetDirection = lookAtTarget.position - agentTransform.position;
        targetDirection.y = 0f;
    
        if (targetDirection.magnitude < 0.1f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion startRotation = agentTransform.rotation;

        float rotationTime = 0f;
        float totalRotationTime = Quaternion.Angle(startRotation, targetRotation) / lookAtSpeed;

        while (rotationTime < totalRotationTime)
        {
            rotationTime += Time.deltaTime;
            float t = rotationTime / totalRotationTime;
            agentTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        agentTransform.rotation = targetRotation;
    }
}