using System.Collections;
using UnityEngine;

public class OrderWaitingHandler : MonoBehaviour
{
    [Header("Wait Settings")]
    [SerializeField] private bool enableWaiting = true;
    [SerializeField] private bool useFixedWaitTime = true;
    [SerializeField] private float fixedWaitDuration = 5.0f;
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private bool enableWaitingAnimation = true;
    [SerializeField] private string animationTriggerName = "Text";

    [Header("Debug Settings")]
    [SerializeField] private bool debugging = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!enableWaiting || !other.CompareTag("Agente"))
            return;

        IAgentRL agent = other.GetComponent<IAgentRL>();
        if (agent != null)
        {
            StartCoroutine(HandleAgentWait(agent, other.gameObject));

        }
    }

    private IEnumerator HandleAgentWait(IAgentRL agent, GameObject agentObject)
    {
        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Agent {agentObject.name} entered wait zone");

        if (useFixedWaitTime)
        {
            yield return StartCoroutine(WaitFixedTime(agent, agentObject));
        }
        else
        {
            yield return StartCoroutine(WaitForOrderReady(agent, agentObject));
        }

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Agent {agentObject.name} finished waiting");
    }

    private IEnumerator WaitFixedTime(IAgentRL agent, GameObject agentObject)
    {
        float waited = 0f;

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Starting fixed wait for {fixedWaitDuration}s");

        // Stop agent movement
        DeactivateAgent(agent);

        AgentAnimationManager animationManager = agentObject.GetComponent<AgentAnimationManager>();

        while (waited < fixedWaitDuration)
        {
            // Text animation
            if (enableWaitingAnimation && animationManager != null)
                animationManager.PlayActionTrigger(animationTriggerName);

            yield return new WaitForSeconds(checkInterval);
            waited += checkInterval;
        }

        // Reactivate agent
        ActivateAgent(agent);

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Fixed wait completed ({waited:F1}s)");
    }

    private IEnumerator WaitForOrderReady(IAgentRL agent, GameObject agentObject)
    {
        var orderAgent = agent as IAgentOrder;
        if (orderAgent == null)
        {
            Debug.LogWarning($"[OrderWaitingHandler] Agent {agentObject.name} does not support orders!");
            yield break;
        }

        // Check if agent has placed an order
        if (!orderAgent.MyOrderId.HasValue)
        {
            Debug.LogWarning($"[OrderWaitingHandler] Agent {agentObject.name} has no order ID - cannot wait for order!");
            yield break;
        }

        float waited = 0f;

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Starting to wait for order {orderAgent.MyOrderId.Value}");

        // Stop agent movement
        DeactivateAgent(agent);

        AgentAnimationManager animationManager = agentObject.GetComponent<AgentAnimationManager>();

        // Check if order is ready
        while (!orderAgent.IsMyOrderReady)
        {
            // Text animation
            if (enableWaitingAnimation && animationManager != null)
            {
                animationManager.PlayActionTrigger(animationTriggerName);
                if (debugging)
                    Debug.Log($"[OrderWaitingHandler] Playing text animation while waiting for order");
            }

            yield return new WaitForSeconds(checkInterval);
            waited += checkInterval;
        }

        // Reactivate agent
        ActivateAgent(agent);

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Order wait completed ({waited:F1}s)");
    }

    /// <summary>
    /// Activates the agent by setting its run state to true.
    /// </summary>
    private void ActivateAgent(IAgentRL agent)
    {
        if (agent != null)
            agent.SetWalking(true);

    }
    
    /// <summary>
    /// Deactivates the agent by setting its run state to false and stopping its rigidbody velocity.
    /// </summary>
    private void DeactivateAgent(IAgentRL agent)
    {
        if (agent != null)
        {
            agent.SetWalking(false);
            agent.GetRigidBody().velocity = Vector3.zero;
        }
    }
}