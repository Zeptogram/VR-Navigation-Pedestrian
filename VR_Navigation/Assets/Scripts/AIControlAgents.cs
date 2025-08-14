using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AIControlAgents : MonoBehaviour
{
    public action[] goalAction;
    private int currentTargetIndex = 0;
    private NavMeshAgent agent;
    private Animator animator;
    private int uniqueID;
    private bool reversed = false;
    private bool fleeing = false;
    private Transform fleeLocation;

    // Struct defining the path that the agent need to follow and all animation that need to be performed along it
    [Serializable]
    public struct action
    {
        public GameObject goalLocation;
        public float wait;
        public String animationName;
    }

    void Start()
    {
        uniqueID = Guid.NewGuid().ToString().GetHashCode();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        CompleteStop();
        if (goalAction.Length > 0)
        {
            Resume();
            InvokeRepeating("WritePedpyStats", 0, 0.3f);
            animator.SetTrigger("isWalking");
            agent.SetDestination(goalAction[currentTargetIndex].goalLocation.transform.position);
        }
    }

    private void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude/2);
    }

    // Start the evacuation for the agent by making it follow the evac point
    public void Flee(Transform EvacPoint)
    {
        fleeing = true;
        Resume();
        animator.SetTrigger("isWalking");
        fleeLocation = EvacPoint;
        agent.SetDestination(EvacPoint.position);
        agent.isStopped = false;
        agent.stoppingDistance = 2;
    }

    // Stops the agents completely so it can be still for certain animations
    // (e.g. when the agent start sitting in a chair outside the navmesh)
    public void CompleteStop()
    {
        agent.enabled = false;
        Collider[] colliders = GetComponents<Collider>();
        foreach(Collider c in colliders)
        {
            if (!c.isTrigger)
            {
                c.enabled = false;
            }
        }
    }

    // Reactivate the agent after CompleteStop is called
    public void Resume()
    {
        agent.enabled = true;
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider c in colliders)
        {
            if (!c.isTrigger)
            {
                c.enabled = true;
            }
        }
    }

    private void WritePedpyStats()
    {
        StatsWriter.WritePedPyStats(
                transform.position.x,
                transform.position.y,
                transform.position.z,
                uniqueID);
    }

    IEnumerator MoveToNextTargetWithWait(float wait)
    {
        for (float timer = wait; timer >= 0; timer -= Time.deltaTime)
        {
            if (fleeing)
            {
                timer = 0;
            }
            yield return null;
        }
        if (!fleeing)
        {
            MoveToNextTarget();
        }
        else
        {
            agent.isStopped = false;
            animator.SetTrigger("isWalking");
            agent.angularSpeed = 180;
            agent.SetDestination(fleeLocation.position);
            agent.stoppingDistance = 2;
        }
    }

    private void MoveToNextTarget()
    {
        agent.isStopped = false;
        animator.SetTrigger("isWalking");
        agent.angularSpeed = 180;
        agent.SetDestination(goalAction[currentTargetIndex].goalLocation.transform.position);
    }
    
    // When the agent reach the next target in the action list wait for the set time and perform the animation corresponding to the reached target
    // and set the agent to reach the following target in the list.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("waypoint") || other.CompareTag("Target"))
        {
            if (currentTargetIndex >= 0 && currentTargetIndex < goalAction.Length && 
                other.gameObject.Equals(goalAction[currentTargetIndex].goalLocation))
            {
                agent.isStopped = true;
                
                if (goalAction[currentTargetIndex].animationName != "")
                {
                    animator.SetTrigger(goalAction[currentTargetIndex].animationName);
                }
                else
                {
                    animator.SetTrigger("isIdle");
                }
                
                agent.angularSpeed = 0;
                float wait = goalAction[currentTargetIndex].wait;

                if (reversed) 
                    currentTargetIndex--;
                else 
                    currentTargetIndex++;

                if (currentTargetIndex >= goalAction.Length)
                {
                    reversed = true;
                    currentTargetIndex = Mathf.Max(0, goalAction.Length - 2);
                }
                else if (currentTargetIndex < 0)
                {
                    reversed = false;
                    currentTargetIndex = Mathf.Min(1, goalAction.Length - 1);
                }
                
                currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, goalAction.Length - 1);
                
                StartCoroutine(MoveToNextTargetWithWait(wait));
            }
        }
        else if (other.CompareTag("evac"))
        {
            animator.SetTrigger("isIdle");
        }
    }
}



