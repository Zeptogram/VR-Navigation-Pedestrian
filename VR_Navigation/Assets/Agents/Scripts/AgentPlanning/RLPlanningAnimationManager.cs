using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension of AgentAnimationManager for RL Planning agents.
/// Adds planning-specific functionality like trigger management and state resets.
/// </summary>
public class RLPlanningAnimationManager : AgentAnimationManager
{
    /// <summary>
    /// Resets all trigger parameters in the animator.
    /// This is more scalable than resetting individual triggers.
    /// </summary>
    public override void ResetAllTriggers()
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
    public override void ResetToIdleState()
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
    public override void PlayActionTrigger(string triggerName)
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
}