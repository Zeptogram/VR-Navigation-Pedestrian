using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIOrdering : MonoBehaviour
{
    GameObject[] goalLocations;
    NavMeshAgent agent;
    private int currentGoalIndex = 0;
    Animator anim;
    private bool isWalking = false;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        goalLocations = GameObject.FindGameObjectsWithTag("order");
        anim = this.GetComponent<Animator>();
        Invoke("StartWalking", 5f);

    }

    // Update is called once per frame
    void Update()
    {
        if (isWalking == true)
        {
            // Calcola la posizione di destinazione
            agent.SetDestination(goalLocations[currentGoalIndex].transform.position);
            

            // Se l'avatar è vicino alla posizione obiettivo, cambia animazione e ferma il movimento
            if (agent.remainingDistance < 0.5f)
            {
                isWalking = false;

                // Cambia animazione a "Idle"
                anim.SetTrigger("isIdle");
                //agent.speed = 0f;

                // Incrementa l'indice dell'obiettivo attuale
                currentGoalIndex++;

                // Verifica se ci sono ancora obiettivi, altrimenti ricomincia da capo
                if (currentGoalIndex < goalLocations.Length)
                {
                    // Inizia il conteggio per la prossima camminata
                    Invoke("StartWalking", 8f);
                }
                else
                {
                    // Puoi aggiungere la logica desiderata qui per gestire la fine degli obiettivi
                    anim.SetTrigger("isIdle");
                    // Ad esempio, per ricominciare da capo immediatamente, puoi chiamare StartWalking();
                }
            }
        }
    }

    void StartWalking()
    {
        anim.SetTrigger("isWalking");
        isWalking = true;
    }
}
