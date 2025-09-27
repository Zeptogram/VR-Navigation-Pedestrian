// ArtifactConnectionManager.cs
// Manages connections between artifacts in the scene.
// Supports 1:1, 1:N, N:N, and N:1 relationships.

using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Defines a connection rule between artifact types.
/// </summary>
[System.Serializable]
public class ConnectionRule
{
    public System.Type sourceType;
    public System.Type targetType;
    public ConnectionType connectionType;
}

/// <summary>
/// Types of connections supported.
/// </summary>
public enum ConnectionType
{
    OneToOne,    // 1:1
    OneToMany,   // 1:N
    ManyToOne,   // N:1
    ManyToMany   // N:N
}


public class ArtifactConnectionManager : MonoBehaviour
{
    [Header("Debug Options")]
    [SerializeField] private bool debugConnections = true;

    // Dictionary to store all artifacts by type
    private Dictionary<System.Type, List<Artifact>> artifactsByType = new Dictionary<System.Type, List<Artifact>>();

    // Connection rules
    private List<ConnectionRule> connectionRules = new List<ConnectionRule>();

    private void Start()
    {
        // Wait a frame to allow all artifacts to initialize
        StartCoroutine(DelayedSetup());
    }

    private System.Collections.IEnumerator DelayedSetup()
    {
        yield return null;
        SetupConnections();
    }

    /// <summary>
    /// Discovers all artifacts in the scene and creates connections based on rules.
    /// </summary>
    public void SetupConnections()
    {
        DiscoverArtifacts();
        SetupConnectionRules();
        ApplyConnectionRules();
    }

    /// <summary>
    /// Discovers all artifacts in the scene and categorizes them by type.
    /// </summary>
    private void DiscoverArtifacts()
    {
        artifactsByType.Clear();

        Artifact[] allArtifacts = FindObjectsOfType<Artifact>();

        foreach (Artifact artifact in allArtifacts)
        {
            System.Type artifactType = artifact.GetType();

            if (!artifactsByType.ContainsKey(artifactType))
            {
                artifactsByType[artifactType] = new List<Artifact>();
            }

            artifactsByType[artifactType].Add(artifact);

            if (debugConnections)
            {
                Debug.Log($"[ArtifactConnectionManager] Discovered {artifactType.Name}: {artifact.ArtifactName}");
            }
        }
    }

    /// <summary>
    /// Sets up connection rules for common artifact types.
    /// </summary>
    private void SetupConnectionRules()
    {
        // Clear existing rules
        connectionRules.Clear();

        // Rule: N Totems to 1 Monitor (N:1)
        connectionRules.Add(new ConnectionRule
        {
            sourceType = typeof(TotemArtifact),
            targetType = typeof(MonitorArtifact),
            connectionType = ConnectionType.ManyToOne,
        });

    }

    /// <summary>
    /// Applies all connection rules to discovered artifacts.
    /// </summary>
    private void ApplyConnectionRules()
    {
        foreach (ConnectionRule rule in connectionRules)
        {
            ApplyConnectionRule(rule);
        }
    }

    /// <summary>
    /// Applies a specific connection rule.
    /// </summary>
    private void ApplyConnectionRule(ConnectionRule rule)
    {
        // Check if both source and target artifact types are present
        if (!artifactsByType.ContainsKey(rule.sourceType) ||
            !artifactsByType.ContainsKey(rule.targetType))
        {
            if (debugConnections)
            {
                Debug.LogWarning($"[ArtifactConnectionManager] Cannot apply rule: {rule.sourceType.Name} -> {rule.targetType.Name}. Missing artifacts.");
            }
            return;
        }

        List<Artifact> sources = artifactsByType[rule.sourceType];
        List<Artifact> targets = artifactsByType[rule.targetType];

        // Connect artifacts based on the connection type
        switch (rule.connectionType)
        {
            case ConnectionType.OneToOne:
                ConnectOneToOne(sources, targets, rule);
                break;

            case ConnectionType.OneToMany:
                ConnectOneToMany(sources, targets, rule);
                break;

            case ConnectionType.ManyToOne:
                ConnectManyToOne(sources, targets, rule);
                break;

            case ConnectionType.ManyToMany:
                ConnectManyToMany(sources, targets, rule);
                break;
        }
    }

    private void ConnectOneToOne(List<Artifact> sources, List<Artifact> targets, ConnectionRule rule)
    {
        int connectionCount = Mathf.Min(sources.Count, targets.Count);

        for (int i = 0; i < connectionCount; i++)
        {
            ConnectArtifacts(sources[i], targets[i], rule);
        }
    }

    private void ConnectOneToMany(List<Artifact> sources, List<Artifact> targets, ConnectionRule rule)
    {
        if (sources.Count > 0)
        {
            Artifact source = sources[0]; // Take first source
            foreach (Artifact target in targets)
            {
                ConnectArtifacts(source, target, rule);
            }
        }
    }

    private void ConnectManyToOne(List<Artifact> sources, List<Artifact> targets, ConnectionRule rule)
    {
        if (targets.Count > 0)
        {
            Artifact target = targets[0]; // Take first target
            foreach (Artifact source in sources)
            {
                ConnectArtifacts(source, target, rule);
            }
        }
    }

    private void ConnectManyToMany(List<Artifact> sources, List<Artifact> targets, ConnectionRule rule)
    {
        foreach (Artifact source in sources)
        {
            foreach (Artifact target in targets)
            {
                ConnectArtifacts(source, target, rule);
            }
        }
    }

    /// <summary>
    /// Connects two artifacts using reflection to call the connection method.
    /// </summary>
    private void ConnectArtifacts(Artifact source, Artifact target, ConnectionRule rule)
    {
        if (target is IArtifactConnectable connectable)
        {
            connectable.ConnectTo(source);
            if (debugConnections)
                Debug.Log($"[ArtifactConnectionManager] Connected {source.ArtifactName} -> {target.ArtifactName}");
            
        }
        else
        {
            Debug.LogError($"[ArtifactConnectionManager] Could not connect to {target.GetType().Name}, Rule: {rule}");
        }
    }
}

