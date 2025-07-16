using UnityEngine;

public class TotemArtifact : Artifact
{
    private int orderCounter = 0;

    protected override void Init()
    {
        orderCounter = 1; 
    }

    public override void Use(int agentId, params object[] args)
    {
        base.Use(agentId, args);

        int orderId = ++orderCounter;
        Debug.Log($"[{ArtifactName}] Agente {agentId} ha piazzato ordine {orderId}");

        EmitSignal("orderPlaced", orderId);
    }
}

