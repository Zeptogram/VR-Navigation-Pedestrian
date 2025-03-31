using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserWriter : MonoBehaviour
{
    Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("writeUserStats", 0f, 0.3f);
        mainCamera = Camera.main;
    }

    // Calls a method that writes the position of the user and the rotation of it's head inside a file used by PedPy
    void writeUserStats()
    {
        if (mainCamera.isActiveAndEnabled)
        {
            StatsWriter.WriteUserStats(
                Camera.main.transform.position.x,
                Camera.main.transform.position.z,
                Camera.main.transform.rotation);
        }
    }
}
