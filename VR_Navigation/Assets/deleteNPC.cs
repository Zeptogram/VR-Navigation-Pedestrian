using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class deleteNPC : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            Destroy(other.gameObject);
        }
    }
}
