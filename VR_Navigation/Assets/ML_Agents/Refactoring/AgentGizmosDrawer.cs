using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AgentSensorsManager;

[RequireComponent(typeof(AgentSensorsManager))]
public class AgentGizmosDrawer : MonoBehaviour
{
    public bool gizmosDrawer;

    private List<(GizmosTag, Vector3)> wallsAndTargetsObservations = new List<(GizmosTag, Vector3)>();
    private List<(GizmosTag, Vector3)> wallsAndAgentsObservations = new List<(GizmosTag, Vector3)>();
    private List<(GizmosTag, Vector3)> wallsAndObjectivesObservations = new List<(GizmosTag, Vector3)>();
    public void SetObservationsResults(List<(GizmosTag, Vector3)> wallsAndTargetsObservations, List<(GizmosTag, Vector3)> wallsAndAgentsObservations, List<(GizmosTag, Vector3)> wallsAndObjectivesObservations)
    {
        this.wallsAndTargetsObservations = wallsAndTargetsObservations;
        this.wallsAndAgentsObservations = wallsAndAgentsObservations;
        this.wallsAndObjectivesObservations = wallsAndObjectivesObservations;
    }

    private Dictionary<GizmosTag, Color> _tagColorDict = new Dictionary<GizmosTag, Color>()
        {
            {GizmosTag.Wall, _wallColor},
            {GizmosTag.Agent, _agentColor},
            {GizmosTag.ValidObjective, _validObjectiveColor},
            {GizmosTag.InvalidObjective, _invalidObjectiveColor},
            {GizmosTag.ValidDirectionIntermediateTarget, _validDirectionIntermediateColor},
            {GizmosTag.InvalidDirectionIntermediateTarget, _invalidDirectionIntermediateColor},
            {GizmosTag.ValidDirectionFinalTarget, _validDirectionFinalColor},
            {GizmosTag.InvalidDirectionFinalTarget, _invalidDirectionFinalColor}
        };

    private static readonly Color _wallColor = new Color(1, 1, 1, 0.05f);
    private static readonly Color _agentColor = Color.cyan;
    private static readonly Color _validObjectiveColor = Color.blue;
    private static readonly Color _invalidObjectiveColor = Color.magenta;

    private static readonly Color _validDirectionIntermediateColor = Color.white;
    private static readonly Color _invalidDirectionIntermediateColor = Color.yellow;
    private static readonly Color _validDirectionFinalColor = Color.green;
    private static readonly Color _invalidDirectionFinalColor = Color.red;
    

    private AgentSensorsManager agentSensorsManager;

    private void Start()
    {
        agentSensorsManager = GetComponent<AgentSensorsManager>();
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
            foreach (Proxemic proxemic in MyConstants.Proxemics)
            {
                float d = proxemic.Distance + MyConstants.rayOffset;

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
