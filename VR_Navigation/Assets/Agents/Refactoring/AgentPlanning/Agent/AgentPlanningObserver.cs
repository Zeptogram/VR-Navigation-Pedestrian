using System.Collections.Generic;
using UnityEngine;
using static AgentPlanningSensorsManager;

/// <summary>
/// Observes the environment for the RL agent, collecting and processing observations
/// about walls, targets, agents, and objectives for use by the neural network.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AgentPlanningObserver : MonoBehaviour
{


    public IAgentConstants constants;

    /// <summary>
    /// Reference to the agent's Rigidbody component.
    /// </summary>
    private Rigidbody _rigidbody;

    /// <summary>
    /// List of observations for walls and targets.
    /// </summary>
    private List<float> wallsAndTargetsObservations = new List<float>();
    /// <summary>
    /// List of observations for walls and agents.
    /// </summary>
    private List<float> wallsAndAgentsObservations = new List<float>();
    /// <summary>
    /// List of observations for walls and objectives.
    /// </summary>
    private List<float> wallsAndObjectivesObservations = new List<float>();

    /// <summary>
    /// Gizmo data for walls and targets.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndTargetsGizmos = new List<(GizmosTagPlanning, Vector3)>();
    /// <summary>
    /// Gizmo data for walls and agents.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndAgentsGizmos = new List<(GizmosTagPlanning, Vector3)>();
    /// <summary>
    /// Gizmo data for walls and objectives.
    /// </summary>
    private List<(GizmosTagPlanning, Vector3)> wallsAndObjectivesGizmos = new List<(GizmosTagPlanning, Vector3)>();

    /// <summary>
    /// Reference to the RLAgentPlanning component.
    /// </summary>
    private RLAgentPlanning rlAgent;

    /// <summary>
    /// Public accessor for walls and targets gizmo data.
    /// </summary>
    public List<(GizmosTagPlanning, Vector3)> WallsAndTargetsGizmos { get => wallsAndTargetsGizmos; set => wallsAndTargetsGizmos = value; }
    /// <summary>
    /// Public accessor for walls and agents gizmo data.
    /// </summary>
    public List<(GizmosTagPlanning, Vector3)> WallsAndAgentsGizmos { get => wallsAndAgentsGizmos; set => wallsAndAgentsGizmos = value; }
    /// <summary>
    /// Public accessor for walls and objectives gizmo data.
    /// </summary>
    public List<(GizmosTagPlanning, Vector3)> WallsAndObjectivesGizmos { get => wallsAndObjectivesGizmos; set => wallsAndObjectivesGizmos = value; }

    /// <summary>
    /// Reference to the ObjectiveObserver component.
    /// </summary>
    private ObjectiveObserver objectiveObserver;

    /// <summary>
    /// Initializes references to required components.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        rlAgent = GetComponent<RLAgentPlanning>();
    }

    /// <summary>
    /// Computes all observations (walls/targets, walls/agents, walls/objectives) from sensor data.
    /// </summary>
    /// <param name="obsDict">Dictionary mapping sensors to their raycast results.</param>
    /// <returns>Tuple of lists with observations for each sensor type.</returns>
    public (List<float> wallsAndTargetsObservations, List<float> wallsAndAgentsObservations, List<float> wallsAndObjectivesObservations) ComputeObservations(Dictionary<Sensore, RaycastHit[]> obsDict)
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
                case SensorName.WallsAndObjectives:
                    ComputeWallsAndObjectivesObservations(item.Value);
                    break;
            }
        }
        return (wallsAndTargetsObservations, wallsAndAgentsObservations, wallsAndObjectivesObservations);
    }

    /// <summary>
    /// Computes the observations related to walls and targets (both intermediate and final) based on Raycast results.
    /// For each detected object, it adds the normalized distance, the one-hot encoding of the detected class,
    /// and updates the gizmo list for visualization.
    /// </summary>
    /// <param name="results">Array of RaycastHit containing the sensor results.</param>
    /// <returns>List of floats containing the normalized and encoded observations.</returns>
    private List<float> ComputeWallsAndTargetsObservations(RaycastHit[] results)
    {
        wallsAndTargetsObservations.Clear();
        wallsAndTargetsGizmos.Clear();
        foreach (RaycastHit observation in results)
        {
            if (observation.collider == null) continue;

            GameObject seenObject = observation.collider.gameObject;
            Tag objTag = seenObject.tag.ToMyTags();
            int hitObjectIndex = -1;
            Target target = seenObject.GetComponent<Target>();
            int targetId = -1; //Default value for walls and final targets
            // One-hot encoding:
            // 0 = wall
            // 1 = intermediate target, valid
            // 2 = intermediate target, invalid
            // 3 = final target, valid
            // 4 = final target, invalid
            switch (objTag)
            {
                case Tag.Wall:
                    hitObjectIndex = 0;
                    break;
                case Tag.Target:
                    if (target != null)
                    {
                        if (target.targetType == TargetType.Final)
                        {
                            // Target finale: verde se task completato, rosso altrimenti
                            bool taskCompleted = rlAgent.IsTaskCompleted();
                            hitObjectIndex = taskCompleted ? 3 : 4;
                        }
                        else
                        {
                            float[] directionObjectives = rlAgent.DetermineVisualizationDirection(seenObject);
                            bool isDirectionValid = rlAgent.CheckForValidDirection(directionObjectives);
                            hitObjectIndex = isDirectionValid ? 1 : 2; // Bianco se direzione valida, giallo se no
                            targetId = target.id; // Set the target ID for intermediate targets
                        }
                    }
                    break;
                default:
                    Debug.LogError($"Error in {nameof(ComputeWallsAndTargetsObservations)} shouldn't see the tag: " + seenObject.tag);
                    break;
            }

            wallsAndTargetsGizmos.Add((objTag.ToMyGizmosTag(hitObjectIndex), observation.point));
            float normalizedDistance = Mathf.Clamp(observation.distance / constants.MAXIMUM_VIEW_DISTANCE, 0f, 1f);
            wallsAndTargetsObservations.Add(normalizedDistance);
            AddOneHotObservation(wallsAndTargetsObservations, hitObjectIndex, 5);
            //Debug.Log("Crossings" + rlAgent.GetCrossings(targetId));
            wallsAndTargetsObservations.Add(rlAgent.GetCrossings(targetId));
        }
        return wallsAndTargetsObservations;
    }

    /// <summary>
    /// Computes observations for walls and agents, including normalized distance, direction, and speed.
    /// </summary>
    /// <param name="results">Array of raycast hits.</param>
    private void ComputeWallsAndAgentsObservations(RaycastHit[] results)
    {
        wallsAndAgentsObservations.Clear();
        wallsAndAgentsGizmos.Clear();
        foreach (RaycastHit observation in results)
        {
            if (observation.collider == null) continue; // Controllo null dalla versione old
            
            GameObject seenObject = observation.collider.gameObject;
            Tag objTag = seenObject.tag.ToMyTags();

            int tagIndex = 0; // 0 = wall, 1 = agent
            float normalizedDirection = 0f;
            float normalizedSpeed = 0f;
            float normalizedDistance = -1f;
            

            switch (objTag)
            {
                case Tag.Wall:
                    normalizedDistance = observation.distance / constants.MAXIMUM_VIEW_DISTANCE;
                    break;
                case Tag.Agent:
                    tagIndex = 1;
                    float diffAng = Clamp0360(Clamp0360(seenObject.transform.eulerAngles.y) - Clamp0360(transform.eulerAngles.y));
                    normalizedDirection = Mathf.Clamp((diffAng / 180f) - 1, -1, 1);
                    normalizedSpeed = observation.rigidbody.velocity.magnitude / 1.7f;
                    normalizedDistance = observation.distance / constants.MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE;
                    break;
                default:
                    Debug.LogError($"Error in {nameof(ComputeWallsAndAgentsObservations)} should't see the tag: " + seenObject.tag);
                    break;
            }
            wallsAndAgentsGizmos.Add((MyGizmosTagExtensionsPlanning.ToMyGizmosTag(objTag, tagIndex), observation.point));
            normalizedDistance = Mathf.Clamp(normalizedDistance, 0f, 1f);
            wallsAndAgentsObservations.Add(normalizedDistance);
            wallsAndAgentsObservations.Add(tagIndex);
            wallsAndAgentsObservations.Add(normalizedDirection);
            wallsAndAgentsObservations.Add(normalizedSpeed);
        }
    }

    /// <summary>
    /// Computes observations for walls and objectives, including normalized distance and one-hot encoding.
    /// </summary>
    /// <param name="results">Array of raycast hits.</param>
    private void ComputeWallsAndObjectivesObservations(RaycastHit[] results)
    {
        wallsAndObjectivesObservations.Clear();
        wallsAndObjectivesGizmos.Clear();
        var objectiveHandler = GetComponent<ObjectiveInteractionHandler>();
        foreach (RaycastHit observation in results)
        {
            if (observation.collider == null) continue;
            GameObject seenObject = observation.collider.gameObject;
            Tag objTag = seenObject.tag.ToMyTags();

            int tagIndex = -1;
            float normalizedDistance = -1f;

            switch (objTag)
            {
                case Tag.Wall:
                    tagIndex = 0;
                    normalizedDistance = observation.distance / constants.MAXIMUM_VIEW_DISTANCE;
                    break;
                case Tag.Objective:
                    bool isAvailable = objectiveHandler != null && objectiveHandler.IsObjectiveCurrentlyAvailable(seenObject);
                    tagIndex = isAvailable ? 1 : 2; // 1 = valid now, 2 = already taken or not available (order)
                    normalizedDistance = observation.distance / constants.MAXIMUM_VIEW_DISTANCE;
                    break;
                default:
                    Debug.LogError($"Error in {nameof(ComputeWallsAndObjectivesObservations)}: unexpected tag {seenObject.tag}");
                    break;
            }

            wallsAndObjectivesGizmos.Add((objTag.ToMyGizmosTag(tagIndex), observation.point));
            normalizedDistance = Mathf.Clamp(normalizedDistance, 0f, 1f);
            wallsAndObjectivesObservations.Add(normalizedDistance);
            AddOneHotObservation(wallsAndObjectivesObservations, tagIndex, 3);
        }
    }

    public bool IsAgentObjective(List<GameObject> objectivesList, GameObject seenObject)
    {
        return objectivesList.Contains(seenObject);
    }

    /// <summary>
    /// Adds a one-hot encoded observation to the list.
    /// </summary>
    /// <param name="x">List to add to.</param>
    /// <param name="observation">Index to set as 1.</param>
    /// <param name="range">Length of one-hot vector.</param>
    private void AddOneHotObservation(List<float> x, int observation, int range)
    {
        for (int i = 0; i < range; i++)
        {
            x.Add(i == observation ? 1.0f : 0.0f);
        }
    }

    /// <summary>
    /// Clamps an angle to the [0, 360) range.
    /// </summary>
    /// <param name="eulerAngles">Angle in degrees.</param>
    /// <returns>Clamped angle.</returns>
    public static float Clamp0360(float eulerAngles)
    {
        float result = eulerAngles % 360;
        if (result < 0) { result += 360f; }
        return result;
    }
}