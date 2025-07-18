// Artifact.cs
// Base class for all artifacts in the system

using UnityEngine;
using System;

public abstract class Artifact : MonoBehaviour {
    public string ArtifactName { get; private set; }

    // Generic signal system for communication
    public event Action<string, object> OnSignal;

    protected virtual void Start()
    {
        ArtifactName = gameObject.name;
        Init();
    }

    // Initialization method to be overridden by subclasses
    protected virtual void Init() {
        // Override for initialization
    }

    // Use: Method used by agents to interact with the artifact
    public virtual void Use(int agentId, params object[] args) {
        Debug.Log($"[{ArtifactName}] Use by agent {agentId}");
    }

    // Observe: Method to observe changes in properties
    public virtual object Observe(string propertyName) {
        Debug.Log($"[{ArtifactName}] Observed property: {propertyName}");
        return null;
    }

    // EmitSignal: Method to emit signals to other artifacts
    protected void EmitSignal(string name, object data)
    {
        Debug.Log($"[{ArtifactName}] Emitting signal {name}");
        OnSignal?.Invoke(name, data);
    }
}
