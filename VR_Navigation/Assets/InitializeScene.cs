using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SetLayerRecursively(gameObject, 8);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Set all the objects inside this gameobject with the tag Muro (wall) so that the ML-agents can avoid them
    private void SetLayerRecursively(GameObject obj, int  layer)
    {
        obj.layer = layer;
        obj.tag = "Muro";

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

}
