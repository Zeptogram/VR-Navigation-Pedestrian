using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentAnimationManager : MonoBehaviour
{
    private Animator animator;
    private RLAgent agent;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<RLAgent>();
        if (animator == null)
            Debug.LogWarning("Animator not found on " + gameObject.name);
    }

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
        Debug.Log($"Set isWalking: {walking}, isIdle: {!walking}");
    }

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

    public void PlayTurn(bool right, float angularSpeed = 1f)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        animator.ResetTrigger("TurnRight");
        animator.ResetTrigger("TurnLeft");
        animator.SetFloat("TurnSpeed", angularSpeed); // Animator Parameter for turn speed
        animator.SetTrigger(right ? "TurnRight" : "TurnLeft");
    }

    public void StopTurn()
    {
        // Stop immediately any turn animation
        animator.ResetTrigger("TurnRight");
        animator.ResetTrigger("TurnLeft");
    }

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
        
        // Reset tutti i trigger dell'animator
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

    /*public void SetRunning()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        animator.SetTrigger("isRunning");
        animator.SetFloat("Speed", 2);
    }*/

    public void UpdateSpeed(float speed)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        animator.SetFloat("Speed", speed);
    }

    public void MoveToNextTargetWithDelay(float delay)
    {
        agent.StartCoroutine(MoveToNextTargetWithDelayCoroutine(delay));
    }

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
        agent.MoveToNextTarget();
    }
}