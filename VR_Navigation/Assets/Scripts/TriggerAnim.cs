using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TriggerAnim : MonoBehaviour
{
    private Animator anim;
    public Quaternion rotation;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("customer"))
        {
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            agent.transform.rotation = rotation;
            anim = other.GetComponent<Animator>();
            anim.SetTrigger("isIdle");
            agent.speed = 0;

        }
    }
}
