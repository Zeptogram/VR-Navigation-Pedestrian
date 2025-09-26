using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/**
 * \class DirectionsObjectives
 * \brief Stores and provides direction arrays for objectives, depending on the agent's approach side.
 * 
 * This component holds two arrays representing the directions for front and rear views.
 * It provides a method to retrieve the appropriate direction array based on the side from which the agent approaches.
 */
public class DirectionsObjectives : MonoBehaviour
{
    [Header("Configuration Mode")]
    [Tooltip("Use Objective (GameObject) mode")]
    [SerializeField] private bool useGameObjectReferences = true;


    [Header("Objective References")]
    [SerializeField] private List<GameObject> frontViewObjectives = new List<GameObject>();

    [SerializeField] private List<GameObject> rearViewObjectives = new List<GameObject>();

    [Header("Final Target Configuration")]
    [SerializeField] private GameObject frontViewFinalTarget;
    [SerializeField] private GameObject rearViewFinalTarget;

    [Header("Array Configuration (Legacy)")]

    /// <summary>
    /// Array of directions for the front view (legacy).
    /// </summary>
    [SerializeField] private float[] frontViewDirections = new float[10];

    /// <summary>
    /// Array of directions for the rear view (legacy).
    /// </summary>
    [SerializeField] private float[] rearViewDirections = new float[10];

    /// <summary>
    /// Returns the direction array based on the specified side with automatic padding.
    /// </summary>
    public float[] getDirections(int side)
    {
        if (useGameObjectReferences)
        {
            return GenerateDirectionsFromGameObjectsWithPadding(side);
        }
        else
        {
            // Legacy behavior - pad to global size
            return PadArrayToGlobalSize(side == 0 ? rearViewDirections : frontViewDirections);
        }
    }

    /**
     * \brief Generates direction array from GameObject references with automatic padding to global size.
     */
    private float[] GenerateDirectionsFromGameObjectsWithPadding(int side)
    {
        // Get global size from ObjectiveObserver
        int globalSize = ObjectiveObserver.GetGlobalArraySize();
        
        float[] directions = new float[globalSize];
        
        // Initialize all to 0
        for (int i = 0; i < globalSize; i++)
        {
            directions[i] = 0f;
        }

        List<GameObject> relevantObjectives = null;
        GameObject finalTarget = null;
        
        if (side == 0) // Rear view
        {
            relevantObjectives = rearViewObjectives;
            finalTarget = rearViewFinalTarget;
        }
        else if (side == 1) // Front view
        {
            relevantObjectives = frontViewObjectives;
            finalTarget = frontViewFinalTarget;
        }
        else
        {
            Debug.LogError("Side parameter invalid - use 0 for rear, 1 for front");
            return directions;
        }

        // Set directions based on visible objectives
        foreach (GameObject objective in relevantObjectives)
        {
            if (objective != null && objective.activeInHierarchy)
            {
                int objectiveIndex = GetObjectiveIndexFromName(objective.name);
                
                if (objectiveIndex >= 0 && objectiveIndex < globalSize - 1) // Leave last slot for final target
                {
                    directions[objectiveIndex] = 1.0f;
                }
            }
        }

        // Handle final target (always goes in last position)
        if (finalTarget != null && finalTarget.activeInHierarchy)
        {
            directions[globalSize - 1] = 1.0f;
        }

        Debug.Log($"[DirectionsObjectives] {gameObject.name} generated array of size {globalSize} for side {side}");
        return directions;
    }

    /**
     * \brief Pads a legacy array to match the global size.
     */
    private float[] PadArrayToGlobalSize(float[] originalArray)
    {
        int globalSize = ObjectiveObserver.GetGlobalArraySize();
        
        if (originalArray.Length >= globalSize)
        {
            // If original is already big enough, just return the first globalSize elements
            float[] result = new float[globalSize];
            for (int i = 0; i < globalSize; i++)
            {
                result[i] = originalArray[i];
            }
            return result;
        }
        
        // Pad with zeros
        float[] paddedArray = new float[globalSize];
        for (int i = 0; i < globalSize; i++)
        {
            if (i < originalArray.Length)
                paddedArray[i] = originalArray[i];
            else
                paddedArray[i] = 0f;
        }
        
        Debug.Log($"[DirectionsObjectives] {gameObject.name} padded legacy array from {originalArray.Length} to {globalSize}");
        return paddedArray;
    }

    /**
     * \brief Extracts objective index from GameObject name.
     */
    private int GetObjectiveIndexFromName(string objectiveName)
    {
        // Get global size for final target mapping
        int globalSize = ObjectiveObserver.GetGlobalArraySize();
        
        // Special case for final target names
        if (objectiveName.ToLower().Contains("final") || objectiveName.ToLower().Contains("fina"))
        {
            return globalSize - 1; // Always goes to last position
        }

        // Try to parse from name (e.g., "Objective (2)")
        if (objectiveName.Contains("(") && objectiveName.Contains(")"))
        {
            string indexStr = objectiveName.Split('(', ')')[1];
            if (int.TryParse(indexStr, out int index))
            {
                // Handle legacy -1 index for final target
                if (index == -1)
                {
                    return globalSize - 1; // Map to last position
                }
                return index;
            }
        }

        Debug.LogWarning($"Could not determine index for objective: {objectiveName}");
        return -1;
    }
}