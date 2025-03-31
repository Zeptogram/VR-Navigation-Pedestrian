using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIOrder : MonoBehaviour
{
    private Animator anim;
    public Transform standingPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("customer"))
        {
            //Debug.Log("forse quaslcosa funziona");
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            agent.SetDestination(standingPoint.position);
          
        }
    }

}
