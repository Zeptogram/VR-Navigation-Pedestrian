/* ArtifactTrigger.cs
This class notifies agents about the artifact's presence and provides navigation data for interaction.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ArtifactNavigationData
{
    public Artifact artifact;
    public Transform destination;
    public Transform exitDestination;
    public float stoppingDistance;
}

public class ArtifactTrigger : MonoBehaviour
{
    [SerializeField] private bool debugging = false;
    [SerializeField] private Artifact targetArtifact;

    public Transform destinationTransform;
    public Transform exitDestination;
    
    [Header("NavMesh Configuration")]
    [SerializeField] private float stoppingDistance = 1.5f;

    // Events for agent entering and exiting the trigger
    public static event Action<GameObject, ArtifactNavigationData> OnAgentTriggerEntered;
    public static event Action<GameObject> OnAgentTriggerExited;

    private HashSet<GameObject> agentsInNavigation = new HashSet<GameObject>();

    private Artifact GetTargetArtifact()
    {
        if (targetArtifact == null)
        {
            targetArtifact = GetComponentInParent<Artifact>();
            if (targetArtifact == null)
                targetArtifact = GetComponent<Artifact>();
        }
        return targetArtifact;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (agentsInNavigation.Contains(other.gameObject))
                return;

            Artifact artifact = GetTargetArtifact();
            if (artifact != null)
            {
                if (debugging)
                    Debug.Log($"[ArtifactTrigger] Artifact '{artifact.ArtifactName}' triggered by Agent {other.gameObject.name}");

                // Check if the agent has already used this artifact
                if (ShouldSkipNavigation(other.gameObject))
                {
                    if (debugging)
                        Debug.Log($"[ArtifactTrigger] Agent {other.name} has already used this artifact - skipping navigation");
                    return;
                }

                // Create navigation data
                var navigationData = new ArtifactNavigationData
                {
                    artifact = artifact,
                    destination = destinationTransform,
                    exitDestination = exitDestination,
                    stoppingDistance = stoppingDistance
                };

                // Emit the event
                OnAgentTriggerEntered?.Invoke(other.gameObject, navigationData);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            if (debugging)
                Debug.Log($"[ArtifactTrigger] Agent {other.name} exited trigger zone");

            // Emit the exit event
            OnAgentTriggerExited?.Invoke(other.gameObject);
        }
    }

    private bool ShouldSkipNavigation(GameObject agent)
    {
        Artifact artifact = GetTargetArtifact();
        if (artifact == null) return false;

        ArtifactInteractionBehavior interactionBehavior = artifact.GetComponentInChildren<ArtifactInteractionBehavior>();
        
        if (interactionBehavior != null)
        {
            return interactionBehavior.HasAgentUsedInteraction(agent);
        }
        
        return false;
    }

    public void AddAgentToNavigation(GameObject agent)
    {
        agentsInNavigation.Add(agent);
    }

    public void RemoveAgentFromNavigation(GameObject agent)
    {
        agentsInNavigation.Remove(agent);
    }

    public bool IsAgentInNavigation(GameObject agent)
    {
        return agentsInNavigation.Contains(agent);
    }
}
