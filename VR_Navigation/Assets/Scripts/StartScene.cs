using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScene : MonoBehaviour
{
    public bool hideTargets = true;
    private static Material invisibleTargetMaterial;

    // Makes all targets invisible at the start of the scene
    void Start()
    {
        if (hideTargets)
        {
            invisibleTargetMaterial = Resources.Load("Materials/invisible", typeof(Material)) as Material;
            GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");
            foreach(GameObject target in targets)
            {
                target.GetComponent<MeshRenderer>().material = invisibleTargetMaterial;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
