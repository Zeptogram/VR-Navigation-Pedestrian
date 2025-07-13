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
    public IAgentConstants constants;


    private List<(GizmosTag, Vector3)> wallsAndTargetsObservations = new List<(GizmosTag, Vector3)>();
    private List<(GizmosTag, Vector3)> wallsAndAgentsObservations = new List<(GizmosTag, Vector3)>();
    public void SetObservationsResults(List<(GizmosTag, Vector3)> wallsAndTargetsObservations, List<(GizmosTag, Vector3)> wallsAndAgentsObservations)
    {
        this.wallsAndTargetsObservations = wallsAndTargetsObservations;
        this.wallsAndAgentsObservations = wallsAndAgentsObservations;
    }

    private Dictionary<GizmosTag, Color> _tagColorDict = new Dictionary<GizmosTag, Color>()
        {
            {GizmosTag.Wall, _wallColor},
            {GizmosTag.Agent, _agentColor},
            {GizmosTag.NewTarget, _targetNewColor},
            {GizmosTag.TakenTarget, _targetTakenColor}
        };

    private static readonly Color _wallColor = new Color(1, 1, 1, 0.05f);
    private static readonly Color _agentColor = Color.cyan;
    private static readonly Color _targetNewColor = Color.green;
    private static readonly Color _targetTakenColor = Color.red;

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
        if (constants == null) return;
        Vector3 newPosition = transform.position + (Vector3.up * constants.verticalRayOffset); 
        newPosition.y += 1;
        for (int i = 0; i < wallsAndTargetsObservations.Count && i < wallsAndAgentsObservations.Count; i++)
        {
            (GizmosTag wallsAndTargetTag, Vector3 wallsAndTargetVector) = wallsAndTargetsObservations[i];
            (GizmosTag wallsAndAgentTag, Vector3 wallsAndAgentVector) = wallsAndAgentsObservations[i];

            float agentAndWallsAndTargetDistance = Vector3.Distance(newPosition, wallsAndTargetVector);
            float agentAndwallsAndAgentDistance = Vector3.Distance(newPosition, wallsAndAgentVector);

            if (agentAndWallsAndTargetDistance < agentAndwallsAndAgentDistance)
            {
                Gizmos.color = _tagColorDict[wallsAndTargetTag];
                Gizmos.DrawLine(newPosition, wallsAndTargetVector);

                Gizmos.color = _tagColorDict[wallsAndAgentTag];
                Gizmos.DrawLine(newPosition, wallsAndAgentVector);
            }
            else
            {
                Gizmos.color = _tagColorDict[wallsAndAgentTag];
                Gizmos.DrawLine(newPosition, wallsAndAgentVector);

                Gizmos.color = _tagColorDict[wallsAndTargetTag];
                Gizmos.DrawLine(newPosition, wallsAndTargetVector);
            }
        }
    }
    private void DrawGizmosProxemics()
    {
        if (constants == null) return;
        Gizmos.color = Color.red;
        Vector3 newPosition = transform.position + (Vector3.up * constants.verticalRayOffset);
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
