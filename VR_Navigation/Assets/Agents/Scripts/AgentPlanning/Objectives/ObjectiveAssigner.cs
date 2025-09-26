using System.Collections.Generic;
using UnityEngine;

public class ObjectiveAssigner : MonoBehaviour
{
    [System.Serializable]
    public class AgentObjectiveMapping
    {
        public RLAgentPlanning agent;
        public List<GameObject> objectives = new List<GameObject>();
        
        [Header("Objective Order Settings")]
        [Tooltip("If true, this agent must complete objectives in the specified order")]
        public bool orderedObjectives = false;
    }
    
    [Header("Individual Agent Assignments")]
    [SerializeField] private List<AgentObjectiveMapping> individualAssignments = new List<AgentObjectiveMapping>();
    [SerializeField] private EnvironmentPlanning environment;
    
    private void Start()
    {
        if (environment != null)
        {
            environment.allAgentsInitialized += OnAllAgentsInitialized;
        }
    }
    
    private void OnAllAgentsInitialized()
    { 
        ApplyIndividualAssignments();
    }
    
    [ContextMenu("Apply Individual Assignments")]
    public void ApplyIndividualAssignments()
    {
        var colorManager = environment.GetComponent<ObjectiveColorManager>();
        if (colorManager == null)
        {
            colorManager = environment.gameObject.AddComponent<ObjectiveColorManager>();
        }
        
        colorManager.ClearAllAssignments();
        
        foreach (var mapping in individualAssignments)
        {
            if (mapping.agent == null) continue;

            // Assign objectives to the specific agent
            var handler = mapping.agent.GetComponent<ObjectiveInteractionHandler>();
            if (handler != null)
            {
                // Set the orderedObjectives flag for this agent
                handler.orderedObjectives = mapping.orderedObjectives;
                
                handler.SetObjectives(mapping.objectives);

                // Update colors
                colorManager.RegisterAgentObjectives(mapping.agent, mapping.objectives);
                
                string orderInfo = mapping.orderedObjectives ? " (ORDERED)" : " (ANY ORDER)";
                Debug.Log($"Assigned {mapping.objectives.Count} individual objectives to {mapping.agent.name}{orderInfo}");
            }
        }
    }

    // Method for assignment via code
    public void AssignObjectivesToAgent(RLAgentPlanning agent, List<GameObject> objectives, bool orderedObjectives = false)
    {
        var handler = agent.GetComponent<ObjectiveInteractionHandler>();
        if (handler != null)
        {
            // Set the orderedObjectives flag
            handler.orderedObjectives = orderedObjectives;
            
            handler.SetObjectives(objectives);
            
            var colorManager = environment.GetComponent<ObjectiveColorManager>();
            if (colorManager != null)
            {
                colorManager.RegisterAgentObjectives(agent, objectives);
            }
            
            string orderInfo = orderedObjectives ? " (ORDERED)" : " (ANY ORDER)";
            Debug.Log($"Assigned {objectives.Count} objectives to {agent.name} via code{orderInfo}");
        }
    }

    // Method to change the order of a specific agent at runtime
    [ContextMenu("Toggle Ordered Objectives for All Agents")]
    public void ToggleOrderedObjectivesForAllAgents()
    {
        foreach (var mapping in individualAssignments)
        {
            if (mapping.agent != null)
            {
                var handler = mapping.agent.GetComponent<ObjectiveInteractionHandler>();
                if (handler != null)
                {
                    mapping.orderedObjectives = !mapping.orderedObjectives;
                    handler.orderedObjectives = mapping.orderedObjectives;
                    
                    string status = mapping.orderedObjectives ? "ORDERED" : "ANY ORDER";
                    Debug.Log($"Toggled {mapping.agent.name} to {status}");
                }
            }
        }
    }

    // Method to set the order of a specific agent
    public void SetOrderedObjectivesForAgent(RLAgentPlanning agent, bool ordered)
    {
        var handler = agent.GetComponent<ObjectiveInteractionHandler>();
        if (handler != null)
        {
            handler.orderedObjectives = ordered;

            // Update the mapping if it exists
            var mapping = individualAssignments.Find(m => m.agent == agent);
            if (mapping != null)
            {
                mapping.orderedObjectives = ordered;
            }
            
            string status = ordered ? "ORDERED" : "ANY ORDER";
            Debug.Log($"Set {agent.name} objectives to {status}");
        }
    }
}