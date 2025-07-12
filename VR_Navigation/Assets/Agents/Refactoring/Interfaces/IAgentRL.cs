using UnityEngine;
using System.Collections;

public interface IAgentRL
{
    void SetRun(bool value);
    Rigidbody GetRigidBody();
    Coroutine StartCoroutine(IEnumerator routine);
}