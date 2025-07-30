using UnityEngine;
using System.Collections;

public interface IAgentRL
{
    void SetWalking(bool value);
    Rigidbody GetRigidBody();
    Coroutine StartCoroutine(IEnumerator routine);
}