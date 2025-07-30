using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IAgentRL
{
    void SetWalking(bool value);
    Rigidbody GetRigidBody();
    Coroutine StartCoroutine(IEnumerator routine);

    // Artifacts
    List<Artifact> assignedArtifacts { get; }

    
}