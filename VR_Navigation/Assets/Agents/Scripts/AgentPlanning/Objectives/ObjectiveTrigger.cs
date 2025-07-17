using UnityEngine;
using System.Collections.Generic;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Multi-Artifact Support")]
    [Tooltip("Artifacts to trigger (supports multiple)")]
    public List<Artifact> linkedArtifacts = new List<Artifact>();
    
    [Header("Auto-Discovery")]
    [Tooltip("Auto-find artifacts by type")]
    public bool autoFindTotems = true;
    public bool autoFindMonitors = true;
    
    [Header("Order System")]
    [Tooltip("If true, will trigger PlaceOrder() on RLAgentPlanning")]
    public bool triggerPlaceOrder = false;
    
    [Tooltip("If true, will trigger PickUpOrder() on RLAgentPlanning")]
    public bool triggerPickUpOrder = false;

    private void Start()
    {
        if (autoFindTotems || autoFindMonitors)
        {
            AutoDiscoverArtifacts();
        }
    }

    private void AutoDiscoverArtifacts()
    {
        linkedArtifacts.Clear();
        
        if (autoFindTotems)
        {
            TotemArtifact[] totems = FindObjectsOfType<TotemArtifact>();
            linkedArtifacts.AddRange(totems);
            Debug.Log($"[ObjectiveTrigger] Auto-discovered {totems.Length} totems");
        }
        
        if (autoFindMonitors)
        {
            MonitorArtifact[] monitors = FindObjectsOfType<MonitorArtifact>();
            linkedArtifacts.AddRange(monitors);
            Debug.Log($"[ObjectiveTrigger] Auto-discovered {monitors.Length} monitors");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            RLAgentPlanning rlAgent = other.gameObject.GetComponent<RLAgentPlanning>();
            if (rlAgent != null)
            {
                // Trigger order actions
                if (triggerPlaceOrder)
                {
                    rlAgent.PlaceOrder();
                }

                if (triggerPickUpOrder)
                {
                    rlAgent.PickUpOrder();
                }
                
                // Optional: Trigger Use() on all linked artifacts
                //TriggerLinkedArtifacts(rlAgent);
            }
        }
    }
    
    private void TriggerLinkedArtifacts(RLAgentPlanning agent)
    {
        int agentId = agent.gameObject.GetInstanceID();
        
        foreach (Artifact artifact in linkedArtifacts)
        {
            if (artifact != null)
            {
                artifact.Use(agentId);
                Debug.Log($"[ObjectiveTrigger] Triggered {artifact.ArtifactName} for agent {agent.gameObject.name}");
            }
        }
    }
}
