using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the animation states and triggers for the RL agent.
/// Handles walking, idle, turning, and custom animation triggers.
/// </summary>
public class AgentAnimationManager : MonoBehaviour
{
    /// <summary>
    /// Reference to the Animator component.
    /// </summary>
    private Animator animator;


    /// <summary>
    /// Reference to the IAgentRL component.
    /// </summary>
    private IAgentRL agent;

    /// <summary>
    /// Initializes references to Animator and RLAgent components.
    /// </summary>
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<IAgentRL>();
        if (animator == null)
            Debug.LogWarning("Animator not found on " + gameObject.name);
        if (agent == null)
            Debug.LogWarning("IAnimatableAgent not found on " + gameObject.name);
    }

    /// <summary>
    /// Sets the walking animation state.
    /// </summary>
    /// <param name="walking">True to set walking, false to unset.</param>
    public void SetWalking(bool walking)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on " + gameObject.name);
                return;
            }
        }

        animator.SetBool("isWalking", walking);
        animator.SetBool("isIdle", !walking);  // Opposite of isWalking
        //Debug.Log($"Set isWalking: {walking}, isIdle: {!walking}");
    }

    /// <summary>
    /// Sets the idle animation state.
    /// </summary>
    /// <param name="idle">True to set idle, false to unset.</param>
    public void SetIdle(bool idle)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on " + gameObject.name);
                return;
            }
        }

        animator.SetBool("isIdle", idle);
        animator.SetBool("isWalking", !idle);
        Debug.Log($"Set isIdle: {idle}, isWalking: {!idle}");
    }

    /// <summary>
    /// Plays the turn animation (left or right) with a specified angular speed.
    /// </summary>
    /// <param name="right">True for right turn, false for left turn.</param>
    /// <param name="angularSpeed">Speed of the turn animation.</param>
    public void PlayTurn(bool right, float angularSpeed = 1f)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        // Reset triggers only if they exist
        if (HasParameter("TurnRight", AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger("TurnRight");
        if (HasParameter("TurnLeft", AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger("TurnLeft");
        if (HasParameter("TurnSpeed", AnimatorControllerParameterType.Float))
            animator.SetFloat("TurnSpeed", angularSpeed);

        string trigger = right ? "TurnRight" : "TurnLeft";
        if (HasParameter(trigger, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(trigger);
    }

    /// <summary>
    /// Stops any turn animation immediately.
    /// </summary>
    public void StopTurn()
    {
        if (HasParameter("TurnRight", AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger("TurnRight");
        if (HasParameter("TurnLeft", AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger("TurnLeft");
    }


    /// <summary>
    /// Updates the speed parameter in the animator.
    /// </summary>
    /// <param name="speed">Speed value to set.</param>
    public void UpdateSpeed(float speed)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// Checks if a parameter exists in the animator.
    /// </summary>
    /// <param name="paramName">Name of the parameter to check.</param>
    /// <param name="type">Type of the parameter to check.</param>
    /// <returns>True if the parameter exists, false otherwise.</returns>
    
    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var param in animator.parameters)
            if (param.type == type && param.name == paramName)
                return true;
        return false;
    }


    // FOR RLPLANNING



    /// <summary>
    /// Resets all trigger parameters in the animator.
    /// This is more scalable than resetting individual triggers.
    /// </summary>
    public void ResetAllTriggers()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on " + gameObject.name);
                return;
            }
        }

        // Reset all triggers in the animator
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }

        Debug.Log("All animation triggers reset");
    }


    /// <summary>
    /// Resets the animator to a clean idle state.
    /// Useful when transitioning between different animation systems.
    /// </summary>
    public void ResetToIdleState()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on " + gameObject.name);
                return;
            }
        }

        // Reset all triggers
        ResetAllTriggers();

        // Idle state
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", true);
        animator.SetFloat("Speed", 0f);

        Debug.Log("Animator reset to idle state");
    }


    /// <summary>
    /// Plays an animation trigger by name.
    /// If the trigger name is empty, it resets all triggers.
    /// For boolean states like walking or idle, it sets them directly.
    /// If the trigger does not exist, it logs a warning.
    /// </summary>
    /// <param name="triggerName">Name of the trigger to play.</param>
    public void PlayActionTrigger(string triggerName)
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

        // If no trigger name is provided, reset all triggers
        if (string.IsNullOrEmpty(triggerName))
        {
            ResetAllTriggers();
            return;
        }

        // For boolean states like walking or idle, set them directly
        if (triggerName == "isWalking")
        {
            SetWalking(true);
            return;
        }
        else if (triggerName == "isIdle")
        {
            SetIdle(true);
            return;
        }

        // For triggers, check if the trigger exists in the animator
        bool triggerExists = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                triggerExists = true;
                break;
            }
        }

        if (!triggerExists)
        {
            Debug.LogWarning($"Trigger '{triggerName}' not found in animator controller");
            return;
        }

        // Reset for security
        ResetAllTriggers();

        // Set the trigger
        animator.SetTrigger(triggerName);
        Debug.Log($"Animation trigger '{triggerName}' set successfully");
    }


    // FOR RLAGENT BASE


    /// <summary>
    /// Moves the agent to the next target after a specified delay.
    /// </summary>
    /// <param name="delay">Delay in seconds before moving to the next target.</param>
    public void MoveToNextTargetWithDelay(float delay)
    {
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