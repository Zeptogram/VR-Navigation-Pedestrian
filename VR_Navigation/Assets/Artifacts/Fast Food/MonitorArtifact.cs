using UnityEngine;
using System.Collections.Generic;

public class MonitorArtifact : Artifact
{
    // Track connected totems
    private List<TotemArtifact> connectedTotems = new List<TotemArtifact>();
    
    protected override void Init()
    {
        Debug.Log($"[{ArtifactName}] Monitor initialized");
    }

    public void ConnectTo(TotemArtifact totem)
    {
        if (!connectedTotems.Contains(totem))
        {
            totem.OnSignal += HandleSignal;
            connectedTotems.Add(totem);
            Debug.Log($"[{ArtifactName}] Connected to totem: {totem.ArtifactName}");
        }
    }
    
    public void DisconnectFrom(TotemArtifact totem)
    {
        if (connectedTotems.Contains(totem))
        {
            totem.OnSignal -= HandleSignal;
            connectedTotems.Remove(totem);
            Debug.Log($"[{ArtifactName}] Disconnected from totem: {totem.ArtifactName}");
        }
    }

    private void HandleSignal(string signal, object data)
    {
        if (signal == "orderPlaced")
        {
            Debug.Log($"[{ArtifactName}] Ordine #{data} in preparazione");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up connections when monitor is destroyed
        foreach (TotemArtifact totem in connectedTotems)
        {
            totem.OnSignal -= HandleSignal;
        }
        connectedTotems.Clear();
    }
}
