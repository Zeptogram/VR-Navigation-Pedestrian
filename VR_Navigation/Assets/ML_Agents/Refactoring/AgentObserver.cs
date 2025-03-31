using System.Collections.Generic;
using UnityEngine;
using static AgentSensorsManager;
using static UnityEditor.Progress;

[RequireComponent(typeof(Rigidbody))]
public class AgentObserver : MonoBehaviour
{
    private List<GameObject> _targetsAlreadyTaken = new List<GameObject>();


    private Rigidbody _rigidbody;
    private List<float> wallsAndTargetsObservations = new List<float>();
    private List<float> wallsAndAgentsObservations = new List<float>();

    private List<(GizmosTag, Vector3)> wallsAndTargetsGizmos = new List<(GizmosTag, Vector3)>();
    private List<(GizmosTag, Vector3)> wallsAndAgentsGizmos = new List<(GizmosTag, Vector3)>();

    public List<(GizmosTag, Vector3)> WallsAndTargetsGizmos { get => wallsAndTargetsGizmos; set => wallsAndTargetsGizmos = value; }
    public List<(GizmosTag, Vector3)> WallsAndAgentsGizmos { get => wallsAndAgentsGizmos; set => wallsAndAgentsGizmos = value; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public (List<float> wallsAndTargetsObservations, List<float> wallsAndAgentsObservations) ComputeObservations(Dictionary<Sensore, RaycastHit[]> obsDict)
    {

        foreach (KeyValuePair<Sensore, RaycastHit[]> item in obsDict)
        {
            switch (item.Key.SensorName)
            {
                case SensorName.WallsAndTargets:
                    ComputeWallsAndTargetsObservations(item.Value);
                    break;
                case SensorName.WallsAndAgents:
                    ComputeWallsAndAgentsObservations(item.Value);
                    break;
            }
        }
        return (wallsAndTargetsObservations, wallsAndAgentsObservations);
    }



    private List<float> ComputeWallsAndTargetsObservations(RaycastHit[] results)
    {
        _targetsAlreadyTaken = GetComponent<RLAgent>().targetsTaken;
        wallsAndTargetsObservations.Clear();
        wallsAndTargetsGizmos.Clear();
        foreach (RaycastHit observation in results)
        {
            if(observation.collider == null) continue;
            GameObject seenObject = observation.collider.gameObject;      
            Tag objTag = seenObject.tag.ToMyTags();
            bool isTargetAlreadyTaken = false;
            int hitObjectIndex = -1;
            //0 if viewing a wall, 1 if viewing a target, 2 viewing a already catched target, -1 don't care
            switch (objTag)
            {
                case Tag.Wall:
                    hitObjectIndex = 0;
                    break;
                case Tag.Target:
                    isTargetAlreadyTaken = _targetsAlreadyTaken.Contains(seenObject);
                    hitObjectIndex = isTargetAlreadyTaken ? 2 : 1;
                    break;
                default:
                    Debug.LogError($"Error in {nameof(ComputeWallsAndTargetsObservations)} should't see the tag: " + seenObject.tag);
                    break;
            }
            wallsAndTargetsGizmos.Add((objTag.ToMyGizmosTag(isTargetAlreadyTaken), observation.point));
            float normalizedDistance = Mathf.Clamp(observation.distance / MyConstants.MAXIMUM_VIEW_DISTANCE, 0f, 1f);
            wallsAndTargetsObservations.Add(normalizedDistance);
            AddOneHotObservation(wallsAndTargetsObservations, hitObjectIndex, 3);
        }
        return wallsAndTargetsObservations;
    }


    private void ComputeWallsAndAgentsObservations(RaycastHit[] results)
    {
        wallsAndAgentsObservations.Clear();
        wallsAndAgentsGizmos.Clear();
        foreach (RaycastHit observation in results)
        {
            if (observation.collider == null) continue;
            GameObject seenObject = observation.collider.gameObject;
            Tag objTag = seenObject.tag.ToMyTags();

            int tagIndex = 0; //0 if viewing a wall, 1 if viewing an agent
            float normalizedDirection = 0f;
            float normalizedSpeed = 0f;
            float normalizedDistance = -1f;
            switch (objTag)
            {
                case Tag.Wall:
                    normalizedDistance = observation.distance / MyConstants.MAXIMUM_VIEW_DISTANCE;
                    break;
                case Tag.Agent:
                    tagIndex = 1;
                    float diffAng = Clamp0360(Clamp0360(seenObject.transform.eulerAngles.y) - Clamp0360(transform.eulerAngles.y));
                    normalizedDirection = Mathf.Clamp((diffAng / 180f) - 1, -1, 1);
                    normalizedSpeed = observation.rigidbody.velocity.magnitude / 1.7f;
                    normalizedDistance = observation.distance / MyConstants.MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE; //da vederificare

                    break;
                default:
                    Debug.LogError($"Error in {nameof(ComputeWallsAndAgentsObservations)} should't see the tag: " + seenObject.tag);
                    break;
            }
            wallsAndAgentsGizmos.Add((objTag.ToMyGizmosTag(), observation.point));
            normalizedDistance = Mathf.Clamp(normalizedDistance, 0f, 1f);
            wallsAndAgentsObservations.Add(normalizedDistance);
            wallsAndAgentsObservations.Add(tagIndex);
            wallsAndAgentsObservations.Add(normalizedDirection);
            wallsAndAgentsObservations.Add(normalizedSpeed);
        }
    }


    private void AddOneHotObservation(List<float> x,  int observation, int range)
    {   
        for (int i = 0; i < range; i++)
        {
            x.Add(i == observation ? 1.0f : 0.0f);
        }
    }

    public static float Clamp0360(float eulerAngles)
    {
        //float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        float result = eulerAngles % 360;
        if (result < 0) { result += 360f; }
        return result;
    }
}
