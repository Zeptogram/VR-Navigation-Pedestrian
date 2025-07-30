using System.Collections.Generic;
using UnityEngine;

public class OrderAgentManager : MonoBehaviour
{
    // Order tracking
    private int? myOrderId = null;
    public int? MyOrderId => myOrderId;
    private bool hasPlacedOrder = false;
    private bool isMyOrderReady = false;
    public bool IsMyOrderReady => isMyOrderReady;

    // References
    private RLAgentPlanning agent;
    private ArtifactAgentManager artifactManager;

    private void Awake()
    {
        agent = GetComponent<RLAgentPlanning>();
        artifactManager = GetComponent<ArtifactAgentManager>();
        
        if (agent == null)
        {
            Debug.LogError($"OrderAgentManager requires RLAgentPlanning component on {gameObject.name}");
        }
        if (artifactManager == null)
        {
            Debug.LogError($"OrderAgentManager requires ArtifactAgentManager component on {gameObject.name}");
        }
    }

    /// <summary>
    /// Method for agent to place an order at the totem
    /// </summary>
    public void PlaceOrder(Artifact totemArtifact)
    {
        if (totemArtifact != null && !hasPlacedOrder)
        {
            hasPlacedOrder = true;
            int agentId = gameObject.GetInstanceID();
            totemArtifact.Use(agentId);
            Debug.Log($"[Agent {gameObject.name}] Placed Order at {totemArtifact.ArtifactName}");
        }
        else if (hasPlacedOrder)
        {
            Debug.Log($"[Agent {gameObject.name}] Already Placed Order");
        }
        else
        {
            Debug.LogWarning($"[Agent {gameObject.name}] No TotemArtifact assigned for placing order");
        }
    }

    /// <summary>
    /// Method for agent to pick up a ready order
    /// </summary>
    public void PickUpOrder(Artifact monitorArtifact)
    {
        if (monitorArtifact == null || !myOrderId.HasValue)
        {
            Debug.Log($"[Agent {gameObject.name}] Cannot pick up order: monitor artifact missing or orderId not set");
            return;
        }

        int agentId = gameObject.GetInstanceID();
        monitorArtifact.Use(agentId, myOrderId.Value);

        // Reset flags 
        isMyOrderReady = false;
        hasPlacedOrder = false;
        myOrderId = null;

        Debug.Log($"[Agent {gameObject.name}] Picked up order from {monitorArtifact.ArtifactName} and reset order tracking");
    }

    /// <summary>
    /// Handles when orders are placed (triggered by artifact events)
    /// </summary>
    public void OnPlacedOrdersChanged(object value)
    {
        var orders = value as List<OrderPlacedData>;
        if (orders != null && !myOrderId.HasValue && hasPlacedOrder)
        {
            foreach (var order in orders)
            {
                if (order.agentId == gameObject.GetInstanceID())
                {
                    myOrderId = order.orderId;
                    isMyOrderReady = false;
                    Debug.Log($"[Agent {gameObject.name}] (Event) My order ID set to {myOrderId.Value}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Handles when orders are ready (triggered by artifact events)
    /// </summary>
    public void OnOrdersReadyChanged(object value)
    {
        var readyOrderIds = value as List<int>;
        if (myOrderId.HasValue && readyOrderIds != null && readyOrderIds.Contains(myOrderId.Value))
        {
            isMyOrderReady = true;
            Debug.Log($"[Agent {gameObject.name}] (Event) My order {myOrderId.Value} ready!");
        }
    }

    /// <summary>
    /// Handles when orders are in preparation (triggered by artifact events)
    /// </summary>
    public void OnOrdersInPreparationChanged(object value)
    {
        var prepOrderIds = value as List<int>;
        if (myOrderId.HasValue && prepOrderIds != null && prepOrderIds.Contains(myOrderId.Value))
        {
            isMyOrderReady = false;
            Debug.Log($"[Agent {gameObject.name}] My order {myOrderId.Value} is now in preparation");
        }
    }

    /// <summary>
    /// Resets order state (call when episode begins)
    /// </summary>
    public void ResetOrderState()
    {
        myOrderId = null;
        hasPlacedOrder = false;
        isMyOrderReady = false;
        Debug.Log($"[Agent {gameObject.name}] Order state reset");
    }

    /// <summary>
    /// Gets the first totem artifact from assigned artifacts
    /// </summary>
    public TotemArtifact GetTotemArtifact()
    {
        if (agent?.assignedArtifacts == null) return null;

        foreach (var artifact in agent.assignedArtifacts)
        {
            if (artifact is TotemArtifact totem)
            {
                return totem;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the first monitor artifact from assigned artifacts
    /// </summary>
    public MonitorArtifact GetMonitorArtifact()
    {
        if (agent?.assignedArtifacts == null) return null;

        foreach (var artifact in agent.assignedArtifacts)
        {
            if (artifact is MonitorArtifact monitor)
            {
                return monitor;
            }
        }
        return null;
    }
}