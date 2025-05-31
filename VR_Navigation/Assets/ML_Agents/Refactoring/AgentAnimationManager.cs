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
            if (animator == null) return;
        }
        animator.SetBool("isWalking", walking);
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
        animator.SetFloat("TurnSpeed", angularSpeed); // Devi avere il parametro TurnSpeed nell'Animator
        animator.SetTrigger(right ? "TurnRight" : "TurnLeft");
    }

    public void StopTurn()
    {
        // Stop immediately any turn animation
        animator.ResetTrigger("TurnRight");
        animator.ResetTrigger("TurnLeft");
    }

    public void PlayActionTrigger(string triggerName)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(param.name);
        }
        if (!string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
    }

    public void SetRunning()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) return;
        }
        animator.SetTrigger("isRunning");
        animator.SetFloat("Speed", 2);
    }

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