using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AgentSensorsManager;

[RequireComponent(typeof(AgentPlanningSensorsManager))]
public class AgentPlanningGizmosDrawer : MonoBehaviour
{
    public bool gizmosDrawer;

    public IAgentConstants constants;


    private List<(GizmosTagPlanning, Vector3)> wallsAndTargetsObservations = new List<(GizmosTagPlanning, Vector3)>();
    private List<(GizmosTagPlanning, Vector3)> wallsAndAgentsObservations = new List<(GizmosTagPlanning, Vector3)>();
    private List<(GizmosTagPlanning, Vector3)> wallsAndObjectivesObservations = new List<(GizmosTagPlanning, Vector3)>();
    public void SetObservationsResults(List<(GizmosTagPlanning, Vector3)> wallsAndTargetsObservations, List<(GizmosTagPlanning, Vector3)> wallsAndAgentsObservations, List<(GizmosTagPlanning, Vector3)> wallsAndObjectivesObservations)
    {
        this.wallsAndTargetsObservations = wallsAndTargetsObservations;
        this.wallsAndAgentsObservations = wallsAndAgentsObservations;
        this.wallsAndObjectivesObservations = wallsAndObjectivesObservations;
    }

    private Dictionary<GizmosTagPlanning, Color> _tagColorDict = new Dictionary<GizmosTagPlanning, Color>()
        {
            {GizmosTagPlanning.Wall, _wallColor},
            {GizmosTagPlanning.Agent, _agentColor},
            {GizmosTagPlanning.ValidObjective, _validObjectiveColor},
            {GizmosTagPlanning.InvalidObjective, _invalidObjectiveColor},
            {GizmosTagPlanning.ValidDirectionIntermediateTarget, _validDirectionIntermediateColor},
            {GizmosTagPlanning.InvalidDirectionIntermediateTarget, _invalidDirectionIntermediateColor},
            {GizmosTagPlanning.ValidDirectionFinalTarget, _validDirectionFinalColor},
            {GizmosTagPlanning.InvalidDirectionFinalTarget, _invalidDirectionFinalColor}
        };

    private static readonly Color _wallColor = new Color(1, 1, 1, 0.05f);
    private static readonly Color _agentColor = Color.cyan;
    private static readonly Color _validObjectiveColor = Color.blue;
    private static readonly Color _invalidObjectiveColor = Color.magenta;

    private static readonly Color _validDirectionIntermediateColor = Color.white;
    private static readonly Color _invalidDirectionIntermediateColor = Color.yellow;
    private static readonly Color _validDirectionFinalColor = Color.green;
    private static readonly Color _invalidDirectionFinalColor = Color.red;
    

    private AgentPlanningSensorsManager agentSensorsManager;

    private void Start()
    {
        agentSensorsManager = GetComponent<AgentPlanningSensorsManager>();
    }
    private void OnDrawGizmos()
    {
        if (gizmosDrawer)
        {
            DrawObservationsGizmos();
            DrawGizmosProxemics();
        }
    }

    private void DrawObservationsGizmos() 
    {
        Vector3 newPosition = transform.position; 
        newPosition.y += 1;

        // Disegna TUTTI i raggi per WallsAndTargets
        foreach (var (tag, position) in wallsAndTargetsObservations)
        {
            if (_tagColorDict.ContainsKey(tag))
            {
                Gizmos.color = _tagColorDict[tag];
                Gizmos.DrawLine(newPosition, position);
            }
        }

        // Disegna TUTTI i raggi per WallsAndAgents
        foreach (var (tag, position) in wallsAndAgentsObservations)
        {
            if (_tagColorDict.ContainsKey(tag))
            {
                Gizmos.color = _tagColorDict[tag];
                Gizmos.DrawLine(newPosition, position);
            }
        }

        // Disegna TUTTI i raggi per WallsAndObjectives
        foreach (var (tag, position) in wallsAndObjectivesObservations)
        {
            if (_tagColorDict.ContainsKey(tag))
            {
                Gizmos.color = _tagColorDict[tag];
                Gizmos.DrawLine(newPosition, position);
            }
        }
    }
    private void DrawGizmosProxemics()
    {
        Gizmos.color = Color.red;
        Vector3 newPosition = transform.position;
        newPosition.y += 1;
        if (agentSensorsManager != null)
        {
            foreach (Proxemic proxemic in constants.Proxemics)
            {
                float d = proxemic.Distance + constants.rayOffset;

                Gizmos.DrawRay(newPosition, agentSensorsManager.CalculateRayDirection(proxemic.RaysNumberPerSide, 1) * d);
                Gizmos.DrawRay(newPosition, agentSensorsManager.CalculateRayDirection(proxemic.RaysNumberPerSide, -1) * d);
                for (int i = 0; i < proxemic.RaysNumberPerSide; i++)
                {
                    Vector3 tp = newPosition;
                    Vector3 a1 = tp + (agentSensorsManager.CalculateRayDirection(i, 1) * d);
                    Vector3 b1 = tp + (agentSensorsManager.CalculateRayDirection(i + 1, 1) * d);
                    Vector3 a2 = tp + (agentSensorsManager.CalculateRayDirection(i, -1) * d);
                    Vector3 b2 = tp + (agentSensorsManager.CalculateRayDirection(i + 1, -1) * d);

                    Gizmos.DrawLine(a1, b1);
                    Gizmos.DrawLine(a2, b2);
                }
            }
        }
    }

}
