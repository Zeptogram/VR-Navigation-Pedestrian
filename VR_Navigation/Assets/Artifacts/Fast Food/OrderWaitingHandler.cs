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

        RLAgentPlanning agent = other.GetComponent<RLAgentPlanning>();
        if (agent != null)
        {
            StartCoroutine(HandleAgentWait(agent));
        }
    }

    private IEnumerator HandleAgentWait(RLAgentPlanning agent)
    {
        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Agent {agent.name} entered wait zone");

        if (useFixedWaitTime)
        {
            yield return StartCoroutine(WaitFixedTime(agent));
        }
        else
        {
            yield return StartCoroutine(WaitForOrderReady(agent));
        }

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Agent {agent.name} finished waiting");
    }

    private IEnumerator WaitFixedTime(RLAgentPlanning agent)
    {
        float waited = 0f;

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Starting fixed wait for {fixedWaitDuration}s");

        // Stop agent movement
        DeactivateAgent(agent);

        AgentAnimationManager animationManager = agent.GetComponent<AgentAnimationManager>();

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

    private IEnumerator WaitForOrderReady(RLAgentPlanning agent)
    {
        // Check if agent has placed an order
        if (!agent.MyOrderId.HasValue)
        {
            Debug.LogWarning($"[OrderWaitingHandler] Agent {agent.name} has no order ID - cannot wait for order!");
            yield break;
        }

        float waited = 0f;

        if (debugging)
            Debug.Log($"[OrderWaitingHandler] Starting to wait for order {agent.MyOrderId.Value}");

        // Stop agent movement
        DeactivateAgent(agent);

        AgentAnimationManager animationManager = agent.GetComponent<AgentAnimationManager>();

        // Check if order is ready
        while (!agent.IsMyOrderReady)
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
            agent.SetRun(true);

    }
    
    /// <summary>
    /// Deactivates the agent by setting its run state to false and stopping its rigidbody velocity.
    /// </summary>
    private void DeactivateAgent(IAgentRL agent)
    {
        if (agent != null)
        {
            agent.SetRun(false);
            agent.GetRigidBody().velocity = Vector3.zero;
        }
    }
}