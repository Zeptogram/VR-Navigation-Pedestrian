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

    void Awake()
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
                    IAgentRL rlAgent = other.GetComponent<IAgentRL>();
                    NavMeshAgent navAgent = other.GetComponent<NavMeshAgent>();

                    if (rlAgent != null && navAgent != null)
                    {
                        // IMPORTANT CHECK: Here i check if the artifact is assigned to the agent
                        if (rlAgent.assignedArtifacts.Contains(targetArtifact))
                        {
                            // Check if interaction behavior prevents navigation
                            if (ShouldSkipNavigation(other.gameObject))
                            {
                                if (debugging)
                                    Debug.Log($"[ArtifactTrigger] Agent {other.name} has already used this artifact - skipping navigation");
                                return;
                            }

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

    /// <summary>
    /// Checks if navigation should be skipped for this agent
    /// </summary>
    private bool ShouldSkipNavigation(GameObject agent)
    {
        // Check if there's an ArtifactInteractionBehavior that would prevent interaction
        ArtifactInteractionBehavior interactionBehavior = targetArtifact.GetComponentInChildren<ArtifactInteractionBehavior>();
        
        if (interactionBehavior != null)
        {
            // If the interaction behavior has onetime use enabled and this agent has already used it
            if (interactionBehavior.HasAgentUsedInteraction(agent))
            {
                return true; // Skip navigation
            }
        }
        
        return false; // Activate navmesh
    }

    private void SwitchToNavMesh(IAgentRL rlAgent, NavMeshAgent navAgent, GameObject agentObj)
    {
        if (debugging)
            Debug.Log($"[ArtifactTrigger] Switching agent {agentObj.name} to NavMesh mode");

        // Capture current movement data
        Vector3 currentVelocity = rlAgent.GetRigidBody().velocity;
        
        // Enable NavMesh mode
        rlAgent.EnableNavMeshMode();
        
        // Pre-configure NavMeshAgent before enabling
        navAgent.stoppingDistance = stoppingDistance;
                
        // Enable NavMeshAgent
        navAgent.enabled = true;
        
        // Fix velocity for NavMesh control
        Vector3 agentVelocity = Vector3.ClampMagnitude(currentVelocity, navAgent.speed);
        navAgent.velocity = agentVelocity;
        
        
        // Set destination
        if (navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destinationTransform.position);
        }
        else
        {
            Debug.LogWarning($"[ArtifactTrigger] Agent {agentObj.name} is not on NavMesh");
        }
        
        // Handle navigation handler setup
        SetupNavigationHandler(agentObj);
    }

    private void SetupNavigationHandler(GameObject agentObj)
    {
        // Check if handler already exists to prevent duplicates (without this weird things happen)
        ArtifactNavigationHandler handler = agentObj.GetComponent<ArtifactNavigationHandler>();
        if (handler == null)
        {
            handler = agentObj.AddComponent<ArtifactNavigationHandler>();
            Debug.Log($"[ArtifactTrigger] Added new ArtifactNavigationHandler to {agentObj.name}");
        }
        else
        {
            Debug.Log($"[ArtifactTrigger] ArtifactNavigationHandler already exists on {agentObj.name}");
        }

        handler.StartNavigation(targetArtifact, destinationTransform, exitDestination);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (agentsInNavigation.Contains(other.gameObject))
            {
                // Check if the agent has a navigation handler that's still navigating to the artifact
                ArtifactNavigationHandler handler = other.gameObject.GetComponent<ArtifactNavigationHandler>();
                if (handler != null && handler.IsNavigatingToArtifact)
                {
                    // Don't remove agent from navigation if still going to artifact
                    if (debugging)
                        Debug.Log($"[ArtifactTrigger] Agent {other.name} exited trigger but still navigating to artifact - keeping in NavMesh mode");
                    return;
                }

                // Agent has either reached artifact or is navigating to exit - return control to RL
                agentsInNavigation.Remove(other.gameObject);
                SwitchBackToRLAgent(other.gameObject);
                
                if (debugging)
                    Debug.Log($"[ArtifactTrigger] Agent {other.name} exited trigger zone - switched back to RL mode");
            }
        }
    }

    private void SwitchBackToRLAgent(GameObject agentObj)
    {
        IAgentRL rlAgent = agentObj.GetComponent<IAgentRL>();
        NavMeshAgent navAgent = agentObj.GetComponent<NavMeshAgent>();

        if (rlAgent != null && navAgent != null)
        {
            if (debugging)
                Debug.Log($"[ArtifactTrigger] Switching agent {agentObj.name} back to RL mode");

            // Capture NavMesh velocity before disabling
            Vector3 navMeshVelocity = navAgent.velocity;

            // Disable NavMesh mode and apply preserved velocity
            rlAgent.DisableNavMeshMode();

            // Disable NavMeshAgent
            navAgent.enabled = false;
            
            // Fix velocity for RL control
            Vector3 agentVelocity = Vector3.ClampMagnitude(navMeshVelocity, navAgent.speed);
            rlAgent.GetRigidBody().velocity = agentVelocity;
            

            // Clean up navigation handler
            ArtifactNavigationHandler handler = agentObj.GetComponent<ArtifactNavigationHandler>();
            if (handler != null)
            {
                Debug.Log($"[ArtifactTrigger] Destroying ArtifactNavigationHandler on {agentObj.name}");
                Destroy(handler);
            }
        }
    }
}
