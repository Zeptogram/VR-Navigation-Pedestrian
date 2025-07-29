using UnityEngine;
using System.Collections.Generic;

public class ObjectiveTrigger : MonoBehaviour
{

    [Header("Order System")]
    public bool triggerPlaceOrder = false;
    public bool triggerPickUpOrder = false;
    public bool triggerWaitForOrderReady = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            RLAgentPlanning rlAgent = other.gameObject.GetComponent<RLAgentPlanning>();
            var handler = rlAgent.GetComponent<ObjectiveInteractionHandler>();
            if (rlAgent != null && handler != null)
            {
                if (triggerPlaceOrder)
                {
                    //handler.HandleObjectiveTrigger(gameObject, () => rlAgent.PlaceOrder());
                    // Qui disattivo RLAgent e uso navmesh (oppure uso stati dell'animator)
                }
                if (triggerPickUpOrder)
                {
                   // handler.HandleObjectiveTrigger(gameObject, () => rlAgent.PickUpOrder());
                }
            }
        }
    }
    
}
