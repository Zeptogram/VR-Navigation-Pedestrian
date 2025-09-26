using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for IAgentRL
/// </summary>
public static class IAgentRLExtensions
{
    public static void SetAnimationSequenceMode(this IAgentRL agent, bool isPlaying)
    {
        if (agent is RLAgent rlAgent)
            rlAgent.SetAnimationSequenceMode(isPlaying);
    }
    
    public static void MoveToNextTarget(this IAgentRL agent)
    {
        if (agent is RLAgent rlAgent)
            rlAgent.MoveToNextTarget();
    }
}

/// <summary>
/// Extension of AgentAnimationManager for RL agents.
/// Adds RL-specific functionality like delayed movement and animation sequences.
/// </summary>
public class RLAgentAnimationManager : AgentAnimationManager
{
    /// <summary>
    /// Reference to the IAgentRL component.
    /// </summary>
    private IAgentRL agent;

    /// <summary>
    /// Initializes references to Animator and RLAgent components.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<IAgentRL>();
        if (agent == null)
            Debug.LogWarning("IAgentRL not found on " + gameObject.name);
    }

    /// <summary>
    /// Moves the agent to the next target after a specified delay.
    /// </summary>
    /// <param name="delay">Delay in seconds before moving to the next target.</param>
    public void MoveToNextTargetWithDelay(float delay)
    {
        if (agent != null)
            agent.StartCoroutine(MoveToNextTargetWithDelayCoroutine(delay));
    }

    /// <summary>
    /// Coroutine to move the agent to the next target after a delay.
    /// </summary>
    /// <param name="delay">Delay in seconds.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator MoveToNextTargetWithDelayCoroutine(float delay)
    {
        yield return new WaitUntil(() => animator != null);

        if (delay > 0)
        {
            agent.SetWalking(false);
            agent.GetRigidBody().velocity = Vector3.zero;
            yield return new WaitForSeconds(delay);
        }
        this.SetWalking(true);
        agent.SetWalking(true);
        if (agent is RLAgent rlAgent)
            rlAgent.MoveToNextTarget();
    }

    /// <summary>
    /// Plays an animation trigger by name without resetting all triggers.
    /// Useful for animation sequences.
    /// </summary>
    /// <param name="triggerName">Name of the trigger to play.</param>
    public void PlayActionTriggerInSequence(string triggerName)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found!");
                return;
            }
        }

        // If no trigger name is provided, do nothing
        if (string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        // For boolean states like walking or idle, set them directly
        if (triggerName == "isWalking")
        {
            this.SetWalking(true);
            return;
        }
        else if (triggerName == "isIdle")
        {
            SetIdle(true);
            return;
        }

        // For triggers, check if the trigger exists in the animator
        bool triggerExists = HasParameter(triggerName, AnimatorControllerParameterType.Trigger);

        if (!triggerExists)
        {
            Debug.LogWarning($"Trigger '{triggerName}' not found in animator controller");
            return;
        }

        // Reset trigger
        animator.ResetTrigger(triggerName);
        
        // Set the trigger
        animator.SetTrigger(triggerName);
        
        Debug.Log($"Animation trigger '{triggerName}' set in sequence");
    }

    /// <summary>
    /// Plays a sequence of animations with delays.
    /// </summary>
    /// <param name="animations">List of animation actions to play.</param>
    /// <param name="agent">The agent to control.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    public IEnumerator PlayAnimationsSequence(List<RLAgent.AnimationAction> animations, IAgentRL agent)
    {
        yield return new WaitUntil(() => animator != null);

        agent.SetAnimationSequenceMode(true);
        agent.SetWalking(false);
        agent.GetRigidBody().velocity = Vector3.zero;

        foreach (var anim in animations)
        {
            Debug.Log($"Playing animation: {anim.animationName} with delay: {anim.delay}");
            
            animator.SetBool("isIdle", false);
            animator.SetBool("isWalking", false);
            
            PlayActionTriggerInSequence(anim.animationName);
            yield return new WaitForSeconds(anim.delay);
        }

        agent.SetAnimationSequenceMode(false);
        this.SetWalking(true);
        agent.SetWalking(true);
        agent.MoveToNextTarget();
    }
}