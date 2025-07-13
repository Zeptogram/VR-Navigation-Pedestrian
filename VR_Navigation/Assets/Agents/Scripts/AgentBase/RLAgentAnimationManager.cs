using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            agent.SetRun(false);
            agent.GetRigidBody().velocity = Vector3.zero;
            yield return new WaitForSeconds(delay);
        }
        SetWalking(true);
        agent.SetRun(true);
        if (agent is RLAgent rlAgent)
            rlAgent.MoveToNextTarget();
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

        agent.SetRun(false);
        agent.GetRigidBody().velocity = Vector3.zero;

        foreach (var anim in animations)
        {
            PlayActionTrigger(anim.animationName);
            yield return new WaitForSeconds(anim.delay);
        }

        SetWalking(true);
        agent.SetRun(true);
        if (agent is RLAgent rlAgent)
            rlAgent.MoveToNextTarget();
    }
}