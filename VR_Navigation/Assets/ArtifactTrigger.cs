using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ArtifactTrigger : MonoBehaviour
{
    [SerializeField] private bool debugging = false;
    [SerializeField] private Artifact targetArtifact;

    public Transform destinationTransform;
    
    [Header("NavMesh Configuration")]
    [SerializeField] private bool enableNavMeshOnTrigger = true;
    [SerializeField] private float stoppingDistance = 1.5f;

    void Start()
    {
        // If no artifact is assigned, try to find it in parent or same GameObject
        if (targetArtifact == null)
        {
            targetArtifact = GetComponentInParent<Artifact>();
            if (targetArtifact == null)
                targetArtifact = GetComponent<Artifact>();
        }

        if (targetArtifact == null)
            Debug.LogWarning($"[ArtifactTrigger] No Artifact component found for {gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (targetArtifact != null)
            {
                if (debugging)
                    Debug.Log($"Artifact '{targetArtifact.ArtifactName}' triggered by Agent {other.gameObject.name}");

                if (enableNavMeshOnTrigger)
                {
                    // Find the RLAgentPlanning and NavMeshAgent components
                    RLAgentPlanning rlAgent = other.GetComponent<RLAgentPlanning>();
                    NavMeshAgent navAgent = other.GetComponent<NavMeshAgent>();

                    if (rlAgent != null && navAgent != null)
                    {
                        // Check if the agent is assigned to the artifact
                        if (rlAgent.assignedArtifacts.Contains(targetArtifact))
                        {
                            SwitchToNavMesh(rlAgent, navAgent, destinationTransform);
                        }
                        else if (debugging)
                        {
                            Debug.Log($"[ArtifactTrigger] Agent {other.name} not assigned to artifact {targetArtifact.ArtifactName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ArtifactTrigger] Agent {other.name} missing RLAgentPlanning or NavMeshAgent component");
                    }
                }
            }
            else if (debugging)
            {
                Debug.Log("Artifact Triggered by Agent (no artifact reference)");
            }
        }
    }

    private void SwitchToNavMesh(RLAgentPlanning rlAgent, NavMeshAgent navAgent, Transform destination)
    {
        if (debugging)
            Debug.Log($"[ArtifactTrigger] Switching agent {rlAgent.name} to NavMesh mode, destination: {targetArtifact.ArtifactName}");

        // Stop because else it keeps the momentum
        Rigidbody rb = rlAgent.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable RLAgent
        rlAgent.enabled = false;

        // NavMeshAgent enabled
        navAgent.enabled = true;
        navAgent.stoppingDistance = stoppingDistance;
        
        // Destination
        if (navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destination.position);
        }
        else
        {
            Debug.LogWarning($"[ArtifactTrigger] Agent {rlAgent.name} is not on NavMesh, cannot set destination");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agente") && debugging)
        {
            Debug.Log($"Agent {other.name} exited artifact trigger zone");
        }
    }
}

