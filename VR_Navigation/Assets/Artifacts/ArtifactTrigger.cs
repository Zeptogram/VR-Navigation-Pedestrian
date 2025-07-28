using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ArtifactTrigger : MonoBehaviour
{
    [SerializeField] private bool debugging = false;
    [SerializeField] private Artifact targetArtifact;

    public Transform destinationTransform;
    public Transform exitDestination;
    
    [Header("NavMesh Configuration")]
    [SerializeField] private bool enableNavMeshOnTrigger = true;
    [SerializeField] private float stoppingDistance = 1.5f;

    private HashSet<GameObject> agentsInNavigation = new HashSet<GameObject>();

    void Start()
    {
        if (targetArtifact == null)
        {
            targetArtifact = GetComponentInParent<Artifact>();
            if (targetArtifact == null)
                targetArtifact = GetComponent<Artifact>();
        }

        if (targetArtifact == null)
            Debug.LogWarning($"[ArtifactTrigger] No Artifact component found for {gameObject.name}");

        if (exitDestination == null)
            Debug.LogWarning($"[ArtifactTrigger] No exit destination set for {gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (agentsInNavigation.Contains(other.gameObject))
                return;

            if (targetArtifact != null)
            {
                if (debugging)
                    Debug.Log($"Artifact '{targetArtifact.ArtifactName}' triggered by Agent {other.gameObject.name}");

                if (enableNavMeshOnTrigger)
                {
                    RLAgentPlanning rlAgent = other.GetComponent<RLAgentPlanning>();
                    NavMeshAgent navAgent = other.GetComponent<NavMeshAgent>();

                    if (rlAgent != null && navAgent != null)
                    {
                        // IMPORTANT CHECK: Here i check if the artifact is assigned to the agent, so that not every agent can navigate and use the artifact!
                        if (rlAgent.assignedArtifacts.Contains(targetArtifact))
                        {
                            agentsInNavigation.Add(other.gameObject);
                            SwitchToNavMesh(rlAgent, navAgent, other.gameObject);
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
        }
    }

    private void SwitchToNavMesh(RLAgentPlanning rlAgent, NavMeshAgent navAgent, GameObject agentObj)
    {
        if (debugging)
            Debug.Log($"[ArtifactTrigger] Switching agent {rlAgent.name} to NavMesh mode");

        
        // For transition (animation)
        rlAgent.StartExitTransition();
        
        // Enable NavMesh mode in RLAgent (disables movement)
        rlAgent.EnableNavMeshMode();

        // Enable NavMeshAgent
        navAgent.enabled = true;
        navAgent.stoppingDistance = stoppingDistance;


        // Add navigation handler
        ArtifactNavigationHandler handler = agentObj.GetComponent<ArtifactNavigationHandler>();
        if (handler == null)
        {
            handler = agentObj.AddComponent<ArtifactNavigationHandler>();
        }

        handler.StartNavigation(targetArtifact, destinationTransform, exitDestination, this);
        
        if (navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destinationTransform.position);
        }
        else
        {
            Debug.LogWarning($"[ArtifactTrigger] Agent {rlAgent.name} is not on NavMesh");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (agentsInNavigation.Contains(other.gameObject))
            {
                agentsInNavigation.Remove(other.gameObject);
                SwitchBackToRLAgent(other.gameObject);
                
                if (debugging)
                    Debug.Log($"[ArtifactTrigger] Agent {other.name} exited trigger zone - switched back to RL mode");
            }
        }
    }

    private void SwitchBackToRLAgent(GameObject agentObj)
    {
        RLAgentPlanning rlAgent = agentObj.GetComponent<RLAgentPlanning>();
        NavMeshAgent navAgent = agentObj.GetComponent<NavMeshAgent>();

        if (rlAgent != null && navAgent != null)
        {
            if (debugging)
                Debug.Log($"[ArtifactTrigger] Switching agent {agentObj.name} back to RL mode");

            // For transition (animation)
            rlAgent.StartExitTransition();

            // Disable NavMeshAgent
            navAgent.enabled = false;

            // Disable NavMesh mode in RLAgent (re-enables movement)
            rlAgent.DisableNavMeshMode();

            // Clean up navigation handler
            ArtifactNavigationHandler handler = agentObj.GetComponent<ArtifactNavigationHandler>();
            if (handler != null)
            {
                Destroy(handler);
            }
        }
    }
}
