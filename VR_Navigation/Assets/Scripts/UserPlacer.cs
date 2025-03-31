using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

public class UserPlacer : MonoBehaviour
{
    XRRayInteractor rayInteractor;
    private GameObject map;
    private void Start()
    {
        map = GameObject.Find("Map");
    }
    public void Drag(SelectEnterEventArgs args)
    {
        
    }

    private void LateUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, map.transform.position.z);
    }
}
