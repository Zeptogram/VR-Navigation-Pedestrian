using System.Collections.Generic;
using UnityEngine;

public class OrderAgentManager : MonoBehaviour
{
    // Order tracking (referenced by the Agent too)
    private int? myOrderId = null;
    public int? MyOrderId => myOrderId;
    private bool hasPlacedOrder = false;
    private bool isMyOrderReady = false;
    public bool IsMyOrderReady => isMyOrderReady;
    private bool wasMyOrderInPreparation = false;

    // References
    private IAgentRL agent;
    private ArtifactAgentManager artifactManager;

    private void Awake()
    {
        agent = GetComponent<IAgentRL>();
        artifactManager = GetComponent<ArtifactAgentManager>();
        
        if (agent == null)
        {
            Debug.LogError($"OrderAgentManager requires IAgentRL component on {gameObject.name}");
        }
        if (artifactManager == null)
        {
            Debug.LogError($"OrderAgentManager requires ArtifactAgentManager component on {gameObject.name}");
        }
    }

    // Base Methods that call the Artifact use method

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
        wasMyOrderInPreparation = false; 

        Debug.Log($"[Agent {gameObject.name}] Picked up order from {monitorArtifact.ArtifactName} and reset order tracking");
    }

    // Observable Properties Methods, called by ArtifactAgentManager when properties change

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
                    wasMyOrderInPreparation = false; // Reset flag when getting new order
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
            wasMyOrderInPreparation = false; 
            Debug.Log($"[Agent {gameObject.name}] (Event) My order {myOrderId.Value} ready!");
        }
    }

    /// <summary>
    /// Handles when orders are in preparation (triggered by artifact events)
    /// </summary>
    public void OnOrdersInPreparationChanged(object value)
    {
        var prepOrderIds = value as List<int>;
        if (myOrderId.HasValue && prepOrderIds != null)
        {
            bool isMyOrderCurrentlyInPrep = prepOrderIds.Contains(myOrderId.Value);
            
            if (isMyOrderCurrentlyInPrep && !wasMyOrderInPreparation)
            {
                isMyOrderReady = false;
                wasMyOrderInPreparation = true;
                Debug.Log($"[Agent {gameObject.name}] (Event) My order {myOrderId.Value} is now in preparation");
            }
            else if (!isMyOrderCurrentlyInPrep && wasMyOrderInPreparation)
            {
                wasMyOrderInPreparation = false;
            }
        }
    }

    /// <summary>
    /// Resets order state (call when episode begins if needed)
    /// </summary>
    public void ResetOrderState()
    {
        myOrderId = null;
        hasPlacedOrder = false;
        isMyOrderReady = false;
        wasMyOrderInPreparation = false; 
        Debug.Log($"[Agent {gameObject.name}] Order state reset");
    }
}