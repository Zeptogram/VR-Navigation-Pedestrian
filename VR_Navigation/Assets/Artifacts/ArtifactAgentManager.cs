using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ArtifactInteractionEvent : UnityEvent<Artifact> { }

[System.Serializable]
public class ArtifactEventSubscription
{
    public Artifact artifact;
    public bool subscribeToPropertyChanged;
}

[System.Serializable]
public class PropertyChangeEvent
{
    public string propertyName;
    public UnityEvent<object> onChanged;
}

public class ArtifactAgentManager : MonoBehaviour
{
    [Header("Artifacts Interaction")]
    public ArtifactInteractionEvent onTotemInteraction;
    public ArtifactInteractionEvent onMonitorInteraction;
    public ArtifactInteractionEvent onGenericArtifactInteraction;

    [Header("Artifacts Subscriptions")]
    public List<ArtifactEventSubscription> artifactEventSubscriptions = new List<ArtifactEventSubscription>();

    [Header("Artifacts Observable Properties")]
    public List<PropertyChangeEvent> artifactPropertyEvents = new List<PropertyChangeEvent>();

    // Reference to the agent
    private RLAgentPlanning agent;

    private void Awake()
    {
        agent = GetComponent<RLAgentPlanning>();
        if (agent == null)
        {
            Debug.LogError($"ArtifactAgentManager requires RLAgentPlanning component on {gameObject.name}");
        }
    }

    private void Start()
    {
        SetupArtifactEventListeners();
    }

    /// <summary>
    /// Sets up event listeners for all assigned artifacts
    /// </summary>
    private void SetupArtifactEventListeners()
    {
        foreach (var subscription in artifactEventSubscriptions)
        {
            if (subscription.subscribeToPropertyChanged)
            {
                subscription.artifact.OnPropertyChanged += HandleObsPropertyChanged;
                Debug.Log($"[ArtifactAgentManager] Subscribed to property changed on {subscription.artifact.ArtifactName}");
            }
        }
    }

    /// <summary>
    /// Removes event listeners for all assigned artifacts
    /// </summary>
    private void RemoveArtifactEventListeners()
    {
        foreach (var subscription in artifactEventSubscriptions)
        {
            if (subscription.subscribeToPropertyChanged)
            {
                subscription.artifact.OnPropertyChanged -= HandleObsPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles property changes for artifacts
    /// </summary>
    public void HandleObsPropertyChanged(string propertyName, object value)
    {
        foreach (var obsProp in artifactPropertyEvents)
        {
            if (obsProp.propertyName == propertyName)
            {
                obsProp.onChanged?.Invoke(value);
                break;
            }
        }
    }

    /// <summary>
    /// Automatically handles artifact interaction based on artifact type
    /// </summary>
    public void HandleArtifactInteraction(Artifact artifact)
    {
        if (!agent.assignedArtifacts.Contains(artifact))
        {
            Debug.LogWarning($"[Agent {gameObject.name}] Trying to interact with unassigned artifact: {artifact.ArtifactName}");
            return;
        }

        switch (artifact)
        {
            case TotemArtifact totemArtifact:
                onTotemInteraction?.Invoke(artifact);
                Debug.Log($"[Agent {gameObject.name}] Interacted with Totem: {totemArtifact.ArtifactName}");
                break;

            case MonitorArtifact monitorArtifact:
                onMonitorInteraction?.Invoke(artifact);
                Debug.Log($"[Agent {gameObject.name}] Interacted with Monitor: {monitorArtifact.ArtifactName}");
                break;

            default:
                onGenericArtifactInteraction?.Invoke(artifact);
                Debug.Log($"[Agent {gameObject.name}] Generic interaction with {artifact.ArtifactName}");
                break;
        }
    }

    private void OnDestroy()
    {
        RemoveArtifactEventListeners();
    }
}