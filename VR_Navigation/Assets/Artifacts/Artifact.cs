// Artifact.cs
// Base class for all artifacts in the system

using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class Artifact : MonoBehaviour {
    public string ArtifactName { get; private set; }

    // Signal system for communication
    public event Action<string, object> OnSignal;

    // Observable properties system
    private Dictionary<string, object> observableProperties = new Dictionary<string, object>();
    public event Action<string, object> OnPropertyChanged;

    protected virtual void Start()
    {
        ArtifactName = gameObject.name;
        Init();
    }

    // Init(): Initialization method to be overridden by subclasses
    protected abstract void Init();

    // Use(int agentId, params object[] args): Method used by agents to interact with the artifact
    public abstract void Use(int agentId, params object[] args);

    // DefineObsProperty(string propertyName, object initialValue): Define an observable property with an initial value
    protected void DefineObsProperty(string propertyName, object initialValue)
    {
        observableProperties[propertyName] = initialValue;
    }

    // UpdateObsProperty(string propertyName, object value): Update an observable property and notify listeners
    protected void UpdateObsProperty(string propertyName, object value)
    {
        if (observableProperties.ContainsKey(propertyName))
        {
            observableProperties[propertyName] = value;
            OnPropertyChanged?.Invoke(propertyName, value);
        }
        else
            Debug.LogWarning($"[{ArtifactName}] Tried to update undefined observable property: {propertyName}");
    }

    // GetObsProperty(string propertyName): Get the value of an observable property
    protected object GetObsProperty(string propertyName)
    {
        if (observableProperties.TryGetValue(propertyName, out var value))
            return value;
        Debug.LogWarning($"[{ArtifactName}] Observable property not found: {propertyName}");
        return null;
    }

    // EmitSignal(string name, object data): Method to emit direct signals to other artifacts
    public void EmitSignal(string name, object data)
    {
        Debug.Log($"[{ArtifactName}] Emitting signal {name}");
        OnSignal?.Invoke(name, data);
    }
}
