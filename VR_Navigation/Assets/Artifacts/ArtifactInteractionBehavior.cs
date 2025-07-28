using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArtifactInteractionBehavior : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDuration = 2f;
    [SerializeField] private bool stopAgentDuringInteraction = true;
    
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
    
    /// <summary>
    /// Called by ArtifactNavigationHandler when agent reaches the artifact
    /// </summary>
    public void StartInteraction(GameObject agent, System.Action onInteractionComplete)
    {
        if (isInteracting) return;
        
        StartCoroutine(HandleInteraction(agent, onInteractionComplete));
    }
    
    private IEnumerator HandleInteraction(GameObject agent, System.Action onInteractionComplete)
    {
        isInteracting = true;
        
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