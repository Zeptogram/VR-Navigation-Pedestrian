using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TotemArtifact : Artifact
{
    [Header("Order Configuration")]
    [SerializeField] private float preparationTimeMin = 5f;
    [SerializeField] private float preparationTimeMax = 10f;
    
    private int orderCounter = 0;
    private Dictionary<int, bool> orders = new Dictionary<int, bool>(); // orderId -> isReady

    protected override void Init()
    {
        orderCounter = 0; 
    }

    public override void Use(int agentId, params object[] args)
    {
        // Call the interface method (for logging)
        base.Use(agentId, args);

        // Order ID set, uses a counter
        int orderId = ++orderCounter;
        Debug.Log($"[ORDER {ArtifactName}] Agent {agentId} placed order:  {orderId}");

        // Add to active orders
        orders[orderId] = false;
        
        // Emit signal with structured data
        EmitSignal("orderPlaced", new OrderPlacedData(orderId, agentId));
        
        // Start preparation coroutine
        StartCoroutine(PrepareOrder(orderId));
    }
    
    private IEnumerator PrepareOrder(int orderId)
    {
        float preparationTime = Random.Range(preparationTimeMin, preparationTimeMax);
        Debug.Log($"ORDER [{ArtifactName}] Order {orderId} ready in {preparationTime:F1} seconds");
        
        yield return new WaitForSeconds(preparationTime);
        
        // Mark order as ready
        if (orders.ContainsKey(orderId))
        {
            orders[orderId] = true;
            EmitSignal("orderReady", orderId);
            Debug.Log($"[{ArtifactName}] Ordine {orderId} è pronto!");
        }
    }
    
    // Method called when an order is picked up
    public void OrderPickedUp(int orderId)
    {
        if (orders.ContainsKey(orderId))
        {
            orders.Remove(orderId);
            Debug.Log($"[{ArtifactName}] Ordine {orderId} rimosso dagli ordini attivi");
        }
        else
        {
            Debug.LogWarning($"[{ArtifactName}] Tentativo di rimuovere ordine {orderId} non esistente");
        }
    }
    
    public override object Observe(string propertyName)
    {
        switch (propertyName)
        {
            case "orders":
                return new Dictionary<int, bool>(orders);
            case "totalOrders":
                return orderCounter;
            default:
                return base.Observe(propertyName);
        }
    }
}

