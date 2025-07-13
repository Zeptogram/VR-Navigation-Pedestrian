using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;


public class objectiveArea : MonoBehaviour
{
    public TextMeshProUGUI instruction;
    public string newTask;
    private bool triggered = false;
    public int areaIndex; // Index used to define the order of objective areas

    private static List<bool> objectivesCompleted = new List<bool>(new bool[10]); // Initialise the objectives (change size as needed)

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CanTrigger(areaIndex) && !triggered)
            {
                instruction.text = newTask;
                CompleteObjective(areaIndex);
                triggered = true;
            }
        }
    }

    // Checks if the previous objective has been completed
    private bool CanTrigger(int index)
    {
        return index == 0 || objectivesCompleted[index - 1];
    }

    private void CompleteObjective(int index)
    {
        if (index < objectivesCompleted.Count)
        {
            objectivesCompleted[index] = true;
        }
    }
}

