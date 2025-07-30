using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ArtifactInteractionEvent : UnityEvent<Artifact> { }

[System.Serializable]

// Mini class for artifact + boolean for property change subscription
public class ArtifactEventSubscription
{
    public Artifact artifact;
    public bool subscribeToPropertyChanged;
}

// Mini class for artifact property change event (from inspector i can use onChanged like onClick and put a method)
[System.Serializable]
public class PropertyChangeEvent
{
    public string propertyName;
    public UnityEvent<object> onChanged;
}

[RequireComponent(typeof(IAgentRL))]
public class ArtifactAgentManager : MonoBehaviour
{
    // Event for handling interactions with different artifact types
    [Header("Artifacts Interaction")]
    public ArtifactInteractionEvent onTotemInteraction;
    public ArtifactInteractionEvent onMonitorInteraction;
    public ArtifactInteractionEvent onGenericArtifactInteraction;

    [Header("Artifacts Subscriptions")]
    public List<ArtifactEventSubscription> artifactEventSubscriptions = new List<ArtifactEventSubscription>();

    [Header("Artifacts Observable Properties")]
    public List<PropertyChangeEvent> artifactPropertyEvents = new List<PropertyChangeEvent>();

    // Reference to the agent
    private IAgentRL agent;

    private void Awake()
    {
        agent = GetComponent<IAgentRL>();
        if (agent == null)
        {
            Debug.LogError($"ArtifactAgentManager requires IAgentRL component on {gameObject.name}");
        }
    }

    private void Start()
    {
        // For obs properties
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
                // OnPropertyChanged is the one in the Artifact class
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
        // Here i call the events for each artifact type
        switch (artifact)
        {
            // Add more artifact types as needed in the future (here the switch case for each artifact)
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