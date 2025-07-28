using UnityEngine;
using UnityEngine.AI;

public class ArtifactNavigationHandler : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Artifact targetArtifact;
    private Transform artifactDestination;
    private Transform exitDestination;

    [Header("Navigation Settings")]
    [SerializeField] private float reachedDistance = 1.0f;
    [SerializeField] private float interactionDelay = 0.2f;

    private bool isNavigatingToArtifact = false;
    private bool hasInteractedWithArtifact = false;

    // For external access
    public bool IsNavigatingToArtifact => isNavigatingToArtifact;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (navAgent == null || !navAgent.enabled) return;

        if (isNavigatingToArtifact && !hasInteractedWithArtifact)
        {
            CheckArtifactReached();
        }
    }

    public void StartNavigation(Artifact artifact, Transform artifactDest, Transform exitDest)
    {
       
        targetArtifact = artifact;
        artifactDestination = artifactDest;
        exitDestination = exitDest;

        isNavigatingToArtifact = true;
        hasInteractedWithArtifact = false;

        Debug.Log($"[ArtifactNavigationHandler] Started navigation to artifact {artifact.ArtifactName}");
    }

    private void CheckArtifactReached()
    {
        if (artifactDestination == null) return;

        float distanceToArtifact = Vector3.Distance(transform.position, artifactDestination.position);

        if (distanceToArtifact <= reachedDistance || (!navAgent.pathPending && navAgent.remainingDistance < 0.5f))
        {
            OnArtifactReached();
        }
    }

    private void OnArtifactReached()
    {
        Debug.Log($"[ArtifactNavigationHandler] Reached artifact {targetArtifact.ArtifactName}");

        isNavigatingToArtifact = false;
        hasInteractedWithArtifact = true;

        // Stop the agent momentarily
        if (navAgent != null)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }

        // Check for interaction behavior on the artifact
        ArtifactInteractionBehavior interactionBehavior = targetArtifact.GetComponentInChildren<ArtifactInteractionBehavior>();
        if (interactionBehavior != null)
        {
            // Animation and then use the artifact
            interactionBehavior.StartInteraction(gameObject, () => 
            {
                // Once the animation is done, handle the artifact use
                HandleArtifactUse();
                Invoke(nameof(StartExitNavigation), interactionDelay);
            });
        }
        else
        {
            // No animation
            HandleArtifactUse();
            Invoke(nameof(StartExitNavigation), interactionDelay);
        }
    }

    private void HandleArtifactUse()
    {
        // Let the agent handle the interaction based on artifact type
        RLAgentPlanning rlAgent = GetComponent<RLAgentPlanning>();
        if (rlAgent != null)
        {
            rlAgent.HandleArtifactInteraction(targetArtifact);
        }
        else
        {
            // Fallback to generic use
            int agentId = gameObject.GetInstanceID();
            targetArtifact.Use(agentId, "navigation_reached", gameObject);
        }
    }

    private void StartExitNavigation()
    {
        if (exitDestination == null)
        {
            Debug.LogWarning($"[ArtifactNavigationHandler] No exit destination set for {gameObject.name}, returning to RL control immediately");
            return;
        }

        Debug.Log($"[ArtifactNavigationHandler] Starting navigation to exit");

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(exitDestination.position);
        }
    }

    

    void OnDestroy()
    {
        // Clean up any pending invokes
        CancelInvoke();
        isNavigatingToArtifact = false;
    }
}