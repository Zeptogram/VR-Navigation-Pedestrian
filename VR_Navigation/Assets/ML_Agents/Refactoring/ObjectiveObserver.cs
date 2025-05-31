using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * \class ObjectiveObserver
 * \brief Observes and manages the state of objectives for an RL agent.
 * 
 * This component tracks which objectives are active or completed and provides an observation array
 * representing the current state of objectives for the agent.
 */
public class ObjectiveObserver : MonoBehaviour
{
    /// <summary>
    /// Reference to the RLAgent component.
    /// </summary>
    private RLAgent agent;
    /// <summary>
    /// Array representing the observation state of objectives (1 = active, 0 = inactive).
    /// The last element is used as a flag for all objectives completed.
    /// </summary>
    [SerializeField] private float[] objectivesObservation = new float[10]; // default to 0

    /**
     * \brief Initializes the observer and sets the completed flag if no objectives are present.
     */
    private void Awake()
    {
        agent = GetComponent<RLAgent>();
        // RIMUOVI: Non impostare l'ultimo elemento a 1 qui!
        // L'inizializzazione corretta avverrà in InitializeObjectives()
        Debug.Log($"ObjectiveObserver awakened for {agent.gameObject.name}");
    }

    /**
     * \brief Initializes the objectives observation array based on the provided objectives list.
     * Sets the agent's taskCompleted flag accordingly.
     * \param objectives List of objective GameObjects.
     */
    public void InitializeObjectives(List<GameObject> objectives)
    {
        // Reset all elements to 0
        for (int i = 0; i < objectivesObservation.Length; i++)
            objectivesObservation[i] = 0;

        // Handle the case where there are no objectives
        if (objectives == null || objectives.Count == 0)
        {
            if (agent != null)
            {
                agent.taskCompleted = true;
            }
            objectivesObservation[objectivesObservation.Length - 1] = 1; // Set last element to 1 (completed)
            Debug.Log("No objectives found - task marked as completed");
            return;
        }

        // Set indicators for active objectives
        int activeObjectivesCount = 0;
        foreach (GameObject objective in objectives)
        {
            string objectiveName = objective.name;
            int startIndex = objectiveName.IndexOf('(');
            int endIndex = objectiveName.IndexOf(')');
            
            if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
            {
                string indexStr = objectiveName.Substring(startIndex + 1, endIndex - startIndex - 1);
                
                if (int.TryParse(indexStr, out int index))
                {
                    if (index < objectivesObservation.Length - 1)
                    {
                        objectivesObservation[index] = 1; // 1 = obiettivo attivo
                        activeObjectivesCount++;
                        Debug.Log($"Objective {objective.name} initialized as ACTIVE with index {index}");
                    }
                }
            }
        }

        // Se ci sono obiettivi attivi, l'agente NON ha completato il task
        if (activeObjectivesCount > 0)
        {
            if (agent != null)
            {
                agent.taskCompleted = false;
            }
            objectivesObservation[objectivesObservation.Length - 1] = 0; // 0 = task incompleto
            Debug.Log($"Task initialized as INCOMPLETE - {activeObjectivesCount} objectives to complete");
        }
        else
        {
            // Nessun obiettivo valido trovato
            if (agent != null)
            {
                agent.taskCompleted = true;
            }
            objectivesObservation[objectivesObservation.Length - 1] = 1; // 1 = task completo
            Debug.Log("No valid objectives found - task marked as completed");
        }
    }

    /**
     * \brief Returns the current objectives observation array.
     * \return Array of floats representing objectives state.
     */
    public float[] GetObjectivesObservation()
    {
        return objectivesObservation;
    }

    /**
     * \brief Marks a specific objective as completed in the observation array.
     * \param objectiveGameObject The objective GameObject that was completed.
     */
    public void MarkObjectiveAsCompleted(GameObject objectiveGameObject)
    {
        // Estrai l'indice dal nome dell'obiettivo (es. "Objective(0)")
        string objectiveName = objectiveGameObject.name;
        
        // Trova le parentesi e estrai l'indice
        int startIndex = objectiveName.IndexOf('(');
        int endIndex = objectiveName.IndexOf(')');
        
        if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
        {
            string indexStr = objectiveName.Substring(startIndex + 1, endIndex - startIndex - 1);
            
            if (int.TryParse(indexStr, out int index))
            {
                if (index < objectivesObservation.Length - 1)
                {
                    objectivesObservation[index] = 0; // 0 = obiettivo completato
                    Debug.Log($"Objective {objectiveGameObject.name} marked as COMPLETED at index {index}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not parse index from objective name: {objectiveName}");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid objective name format: {objectiveName}. Expected format: 'Objective(index)'");
        }
        
        // Aggiorna il flag di completamento
        UpdateCompletionFlag();
    }

    /**
     * \brief Updates the completion flag based on current objectives state.
     */
    private void UpdateCompletionFlag()
    {
        // Conta quanti obiettivi sono ancora attivi (= 1) e quanti sono completati (= 0)
        int activeObjectives = 0;
        int completedObjectives = 0;
        
        for (int i = 0; i < objectivesObservation.Length - 1; i++) // Escludi l'ultimo elemento (flag)
        {
            if (objectivesObservation[i] == 1)
            {
                activeObjectives++;
            }
            else if (objectivesObservation[i] == 0)
            {
                // Questo potrebbe essere un obiettivo completato O un slot vuoto
                // Per distinguere, dovremmo sapere quanti obiettivi erano inizialmente attivi
                completedObjectives++;
            }
        }
        
        Debug.Log($"UpdateCompletionFlag: activeObjectives={activeObjectives}, completedObjectives={completedObjectives}");
        
        // Il task è completato solo se:
        // 1. Non ci sono più obiettivi attivi (activeObjectives == 0)
        // 2. E c'erano degli obiettivi inizialmente (questo lo verifichiamo dal fatto che agent.taskCompleted era false)
        if (activeObjectives == 0 && agent != null && !agent.taskCompleted)
        {
            SetTaskCompleted();
        }
    }

    /**
     * \brief Sets the task as completed and updates the completion flag.
     */
    public void SetTaskCompleted()
    {
        if (agent != null)
        {
            agent.taskCompleted = true;
        }
        
        // Imposta il flag di completamento nell'ultima posizione dell'array
        if (objectivesObservation.Length > 0)
        {
            objectivesObservation[objectivesObservation.Length - 1] = 1f;
        }
        
        Debug.Log("ALL OBJECTIVES COMPLETED - task marked as DONE!");
    }
}