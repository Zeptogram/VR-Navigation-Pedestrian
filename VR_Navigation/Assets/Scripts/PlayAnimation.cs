using UnityEngine;
using UnityEngine.AI;

public class PlayAnimation : MonoBehaviour
{
    [Tooltip("Animation State Name")]
    public string stateName = "Running";
    private Animator animator;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            Debug.LogError("No animator found on this GameObject.");
            return;
        }

        animator.Play(stateName, 0, 0f);
    }
}
