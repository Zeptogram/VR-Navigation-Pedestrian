using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;

public interface IAgentRL
{


    // Targets
    public List<GameObject> targetsTaken { get; }

    // Artifacts
    List<Artifact> assignedArtifacts { get; }
    public void EnableNavMeshMode();
    public void DisableNavMeshMode();
    public System.Collections.IEnumerator EnableNavMeshObstacleWithDelay(float delay);

    // Agent Constants
    IAgentConstants constants { get; }

    // Episode management
    void ComputeSteps();
    void Finished();

    // Agent actions
    void SpeedChange(float deltaSpeed);
    void AngleChange(float deltaAngle);

    // Utilities
    void SetWalking(bool value);
    Rigidbody GetRigidBody();
    Coroutine StartCoroutine(IEnumerator routine);
}

public interface IAgentRLPlanning : IAgentRL
{
    public void rewardsWallsAndTargetsObservations(List<(GizmosTagPlanning, Vector3)> wallsAndTargets);
    public void rewardsWallsAndAgentsObservations(List<(GizmosTagPlanning, Vector3)> wallsAndAgents);
}

public interface IAgentRLBase : IAgentRL
{
    public void rewardsWallsAndTargetsObservations(List<(GizmosTag, Vector3)> wallsAndTargets);
    public void rewardsWallsAndAgentsObservations(List<(GizmosTag, Vector3)> wallsAndAgents);
}

