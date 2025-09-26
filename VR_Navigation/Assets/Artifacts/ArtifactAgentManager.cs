using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ArtifactInteractionEvent : UnityEvent<Artifact> { }

[System.Serializable]
public class PropertyChangeEvent
{
    public string propertyName;
    public UnityEvent<object> onPropertyChanged;
}

[System.Serializable]
public class ArtifactInteraction
{
    [Header("On Interaction Configuration")]
    public Artifact artifact;
    public ArtifactInteractionEvent onInteraction;
}

[RequireComponent(typeof(IAgentRL))]
public class ArtifactAgentManager : MonoBehaviour
{

    [Header("Artifact Interaction Configuration")]
    public bool generateAutomatically = true;

    public List<ArtifactInteraction> artifactInteractions = new List<ArtifactInteraction>();

    [Header("Artifacts Observable Properties (Focus)")]
    public List<Artifact> artifactSubscriptions = new List<Artifact>();

    public List<PropertyChangeEvent> OnPropertyChanged = new List<PropertyChangeEvent>();

    // Reference to the agent (needed for assignedArtifacts, if you dont use IAgentRL, then add that variable to a new script agent)
    private GameObject agentGameObject;
    private IAgentRL agent;

    private void Awake()
    {
        agentGameObject = gameObject;

        agent = agentGameObject.GetComponent<IAgentRL>();

        if (agent == null)
        {
            Debug.LogError($"ArtifactAgentManager requires IAgentRL component on {gameObject.name}");
        }
    }

    private void Start()
    {
        SetupArtifactEventListeners();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called in the editor when inspector values change
    /// </summary>
    private void OnValidate()
    {
        if (generateAutomatically && Application.isPlaying == false)
        {
            // Delay the execution to avoid issues during inspector updates
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) // Check if object still exists
                {
                    SyncMappingsWithAssignedArtifacts();
                }
            };
        }
    }

    /// <summary>
    /// Synchronizes mappings with assigned artifacts from RLAgentPlanning
    /// </summary>
    private void SyncMappingsWithAssignedArtifacts()
    {
        if (agent == null)
        {
            agent = GetComponent<IAgentRL>();
            if (agent == null) return;
        }

        if (agent.assignedArtifacts == null) return;

        // Remove mappings for artifacts that are no longer assigned
        for (int i = artifactInteractions.Count - 1; i >= 0; i--)
        {
            if (artifactInteractions[i].artifact == null || 
                !agent.assignedArtifacts.Contains(artifactInteractions[i].artifact))
            {
                artifactInteractions.RemoveAt(i);
            }
        }

        // Add mappings for new assigned artifacts
        foreach (var artifact in agent.assignedArtifacts)
        {
            if (artifact != null && FindMappingForArtifact(artifact) == null)
            {
                ArtifactInteraction newMapping = new ArtifactInteraction
                {
                    artifact = artifact,
                    onInteraction = new ArtifactInteractionEvent()
                };

                // Auto-configure the mapping
                ConfigureArtifactMapping(newMapping, artifact);
                artifactInteractions.Add(newMapping);
            }
        }

        // Mark the object as dirty so Unity saves the changes
        if (Application.isEditor && !Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Configures the artifact mapping based on artifact type or name
    /// </summary>
    private void ConfigureArtifactMapping(ArtifactInteraction mapping, Artifact artifact)
    {
        //string artifactName = artifact.ArtifactName.ToLower();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(mapping.onInteraction, CallGenericUseMethod);
    }
#endif

    /// <summary>
    /// Sets up event listeners for all assigned artifacts
    /// </summary>
    private void SetupArtifactEventListeners()
    {
        foreach (var artifact in artifactSubscriptions)
        {
            if (artifact != null)
            {
                artifact.OnPropertyChanged += HandleObsPropertyChanged;
                Debug.Log($"[ArtifactAgentManager] Subscribed to property changed on {artifact.ArtifactName}");
            }
        }
    }

    /// <summary>
    /// Removes event listeners for all assigned artifacts
    /// </summary>
    private void RemoveArtifactEventListeners()
    {
        foreach (var artifact in artifactSubscriptions)
        {
            if (artifact != null)
            {
                artifact.OnPropertyChanged -= HandleObsPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles property changes for artifacts
    /// </summary>
    public void HandleObsPropertyChanged(string propertyName, object value)
    {
        foreach (var obsProp in OnPropertyChanged)
        {
            if (obsProp.propertyName == propertyName)
            {
                obsProp.onPropertyChanged?.Invoke(value);
                break;
            }
        }
    }

    /// <summary>
    /// Automatically handles artifact interaction based on configured mappings
    /// </summary>
    public void HandleArtifactInteraction(Artifact artifact)
    {
        if (!agent.assignedArtifacts.Contains(artifact))
        {
            Debug.LogWarning($"[Agent {gameObject.name}] Trying to interact with unassigned artifact: {artifact.ArtifactName}");
            return;
        }

        // Look for specific mapping for this artifact
        ArtifactInteraction mapping = FindMappingForArtifact(artifact);
        
        if (mapping != null)
        {
            mapping.onInteraction?.Invoke(artifact);
            Debug.Log($"[Agent {gameObject.name}] Specific interaction with {artifact.ArtifactName}");
        }
    }

    /// <summary>
    /// Finds the interaction mapping for a specific artifact
    /// </summary>
    private ArtifactInteraction FindMappingForArtifact(Artifact artifact)
    {
        foreach (var mapping in artifactInteractions)
        {
            if (mapping.artifact == artifact)
            {
                return mapping;
            }
        }
        return null;
    }

    /// <summary>
    /// Generic artifact use method
    /// </summary>
    public void CallGenericUseMethod(Artifact artifact)
    {
        Debug.Log($"[Agent {gameObject.name}] Using artifact: {artifact.ArtifactName}");
        int agentId = gameObject.GetInstanceID();
        artifact.Use(agentId, gameObject);
    }

    private void OnDestroy()
    {
        RemoveArtifactEventListeners();
    }
}