using System.Collections;
using UnityEngine;

public class AgentAnimationManager : MonoBehaviour
{
    private Animator animator;
    private RLAgent agent;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<RLAgent>();
    }

    public void SetWalking(bool walking)
    {
        animator.SetBool("isWalking", walking);
        animator.SetBool("isIdle", !walking);
    }

    public void PlayActionTrigger(string triggerName)
    {
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
        animator.SetTrigger("isRunning");
        animator.SetFloat("Speed", 2);
    }

    public void UpdateSpeed(float speed)
    {
        animator.SetFloat("Speed", speed);
    }

    public void MoveToNextTargetWithDelay(float delay)
    {
        agent.StartCoroutine(MoveToNextTargetWithDelayCoroutine(delay));
    }

    private IEnumerator MoveToNextTargetWithDelayCoroutine(float delay)
    {
        if (delay > 0)
        {
            agent.SetRun(false);
            agent.SpeedChange(-agent.GetRigidBody().velocity.magnitude);
            yield return new WaitForSeconds(delay);
        }
        SetWalking(true);
        agent.SetRun(true);
        agent.MoveToNextTarget();
    }
}