using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the dynamic coloring of objectives based on which agents should reach them.
/// </summary>
public class ObjectiveColorManager : MonoBehaviour
{
    /// <summary>
    /// Dictionary mapping objectives to the list of agents that should reach them.
    /// </summary>
    private Dictionary<GameObject, List<RLAgentPlanning>> objectiveToAgents = 
        new Dictionary<GameObject, List<RLAgentPlanning>>();
    
    /// <summary>
    /// Color for objectives that no agent needs to reach.
    /// </summary>
    [SerializeField] private Color unassignedObjectiveColor = Color.gray;
    
    /// <summary>
    /// Color for objectives shared by multiple agents.
    /// </summary>
    [SerializeField] private Color sharedObjectiveColor = Color.blue;

    /// <summary>
    /// Registers an agent's objectives for coloring.
    /// </summary>
    /// <param name="agent">The RLAgent</param>
    /// <param name="objectives">List of objectives assigned to this agent</param>
    public void RegisterAgentObjectives(RLAgentPlanning agent, List<GameObject> objectives)
    {
        foreach (GameObject objective in objectives)
        {
            if (objective == null) continue;
            
            if (!objectiveToAgents.ContainsKey(objective))
            {
                objectiveToAgents[objective] = new List<RLAgentPlanning>();
            }
            
            if (!objectiveToAgents[objective].Contains(agent))
            {
                objectiveToAgents[objective].Add(agent);
            }
        }
        
        UpdateObjectiveColors();
    }

    public void UpdateObjectiveColors()
    {
        // Trova solo gli obiettivi figli di QUESTO ambiente
        Transform[] allTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform t in allTransforms)
        {
            if (t.CompareTag("Obiettivo"))
            {
                GameObject objective = t.gameObject;
                Color targetColor = unassignedObjectiveColor;

                if (objectiveToAgents.ContainsKey(objective))
                {
                    List<RLAgentPlanning> assignedAgents = objectiveToAgents[objective];

                    if (assignedAgents.Count == 1)
                    {
                        targetColor = assignedAgents[0].GetAgentColor();
                    }
                    else if (assignedAgents.Count > 1)
                    {
                        if (AllAgentsSameGroup(assignedAgents))
                        {
                            targetColor = assignedAgents[0].GetAgentColor();
                        }
                        else
                        {
                            targetColor = sharedObjectiveColor;
                        }
                    }
                }

                ApplyColorToObjective(objective, targetColor);
            }
        }
    }

    public void ClearAllAssignments()
    {
        objectiveToAgents.Clear();

        // Trova solo gli obiettivi figli di QUESTO ambiente
        Transform[] allTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform t in allTransforms)
        {
            if (t.CompareTag("Obiettivo"))
            {
                ApplyColorToObjective(t.gameObject, unassignedObjectiveColor);
            }
        }
    }

    /// <summary>
    /// Checks if all agents belong to the same group.
    /// </summary>
    /// <param name="agents">List of agents</param>
    /// <returns>True if all agents are in the same group</returns>
    private bool AllAgentsSameGroup(List<RLAgentPlanning> agents)
    {
        if (agents.Count <= 1) return true;
        
        Group firstGroup = agents[0].group;
        for (int i = 1; i < agents.Count; i++)
        {
            if (agents[i].group != firstGroup)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Applies the specified color to an objective GameObject.
    /// </summary>
    /// <param name="objective">The objective to color</param>
    /// <param name="color">The color to apply</param>
    private void ApplyColorToObjective(GameObject objective, Color color)
    {
        Renderer renderer = objective.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Crea un nuovo materiale per evitare di modificare il materiale condiviso
            Material objectiveMaterial = new Material(renderer.material);
            objectiveMaterial.color = color;
            renderer.material = objectiveMaterial;
        }
    }

    /// <summary>
    /// Unregisters an agent from a specific objective and updates colors.
    /// </summary>
    /// <param name="agent">The RLAgentPlanning</param>
    /// <param name="objective">The objective GameObject</param>
    public void UnregisterAgentFromObjective(RLAgentPlanning agent, GameObject objective)
    {
        if (objectiveToAgents.ContainsKey(objective))
        {
            objectiveToAgents[objective].Remove(agent);
            if (objectiveToAgents[objective].Count == 0)
            {
                objectiveToAgents.Remove(objective);
            }
            UpdateObjectiveColors();
        }
    }
}