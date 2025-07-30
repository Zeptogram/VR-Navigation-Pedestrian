/* 
    TotemArtifact.cs
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
public class TotemArtifact : Artifact
{
    [Header("Order Configuration")]
    [SerializeField] private float preparationTimeMin = 5f;
    [SerializeField] private float preparationTimeMax = 10f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI orderText; 

    private int orderCounter;
    private Dictionary<int, bool> orders = new Dictionary<int, bool>(); // orderId -> isReady

    protected override void Init()
    {
        orderCounter = 0;
        if (orderText != null)
            orderText.text = "";
    }

    // From Artifact Interface
    public override void Use(int agentId, params object[] args)
    {
        // Call the interface method (for logging)
        base.Use(agentId, args);

        // Order ID set, uses a counter
        int orderId = ++orderCounter;
        Debug.Log($"[{ArtifactName}] Agent {agentId} placed order:  {orderId}");

        // Add to active orders
        orders[orderId] = false;

        // UI Update
        if (orderText != null)
        {
            orderText.text = $"Order #{orderId} sent!";
            StartCoroutine(ClearOrderTextAfterDelay(3f));
        }

        // Emit signal with structured data
        EmitSignal("orderPlaced", new OrderPlacedData(orderId, agentId));

        // Start preparation coroutine
        StartCoroutine(PrepareOrder(orderId));
    }

    // Specific methods to handle order preparation


    // PrepareOrder(int orderId): Coroutine to simulate order preparation
    private IEnumerator PrepareOrder(int orderId)
    {
        float preparationTime = UnityEngine.Random.Range(preparationTimeMin, preparationTimeMax);
        Debug.Log($"[{ArtifactName}] Order {orderId} ready in {preparationTime:F1} seconds");

        yield return new WaitForSeconds(preparationTime);

        // Mark order as ready
        if (orders.ContainsKey(orderId))
        {
            orders[orderId] = true;
            EmitSignal("orderReady", orderId);
            Debug.Log($"[{ArtifactName}] Order {orderId} ready");
        }
    }

    // OrderPickedUp(int orderId): Method to remove an order when picked up
    public void OrderPickedUp(int orderId)
    {
        if (orders.ContainsKey(orderId))
        {
            orders.Remove(orderId);
            Debug.Log($"[{ArtifactName}] Order {orderId} removed after pickup");
        }
        else
        {
            Debug.LogWarning($"[{ArtifactName}] Order {orderId} doesn't exist or has already been picked up");
        }
    }

    // HasOrder(int orderId): Check if an order exists
    public bool HasOrder(int orderId)
    {
        return orders.ContainsKey(orderId);
    }
    
     // ClearOrderTextAfterDelay(float delay): Coroutine to clear the order text after a delay
    private IEnumerator ClearOrderTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (orderText != null)
            orderText.text = "";
    }
}

