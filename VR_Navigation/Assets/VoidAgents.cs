using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidAgents : MonoBehaviour
{
    // Destroys the agents who enters the trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Agente" && other.gameObject.name != "Complete XR Origin Set Up") 
        {
            GameObject.Destroy(other.gameObject);
        } 
    }
}
