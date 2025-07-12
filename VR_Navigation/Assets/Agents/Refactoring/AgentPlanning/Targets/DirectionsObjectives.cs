using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * \class DirectionsObjectives
 * \brief Stores and provides direction arrays for objectives, depending on the agent's approach side.
 * 
 * This component holds two arrays representing the directions for front and rear views.
 * It provides a method to retrieve the appropriate direction array based on the side from which the agent approaches.
 */
public class DirectionsObjectives : MonoBehaviour
{
    /// <summary>
    /// Array of directions for the front view (length must be 10).
    /// </summary>
    [SerializeField] private float[] frontViewDirections = new float[10];

    /// <summary>
    /// Array of directions for the rear view (length must be 10).
    /// </summary>
    [SerializeField] private float[] rearViewDirections = new float[10];

    /**
     * \brief Checks at startup that both direction arrays have the correct length.
     */
    private void Start() {
        if(rearViewDirections.Length != 10 || frontViewDirections.Length != 10)
            Debug.LogError("Length must be 10");
    }

    /**
     * \brief Returns the direction array based on the specified side.
     * \param side 0 for rear view, 1 for front view.
     * \return The corresponding direction array, or a new array of length 10 if the side is invalid.
     */
    public float[] getDirections(int side){
        if (side == 0) return rearViewDirections;
        else if (side == 1) return frontViewDirections;
        else{
            Debug.LogError("Side string invalid");
            return new float[10];
        } 
    }
}