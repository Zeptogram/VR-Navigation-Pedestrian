using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class Startup
{
    private static Material defaultTargetMaterial;
    private static Material highlightTargetMaterial;

    static Startup()
    {
        defaultTargetMaterial = Resources.Load("Materials/targets", typeof(Material)) as Material;
        highlightTargetMaterial = Resources.Load("Materials/highlitedTarget", typeof(Material)) as Material;
        GameObject[] allTargets = GameObject.FindGameObjectsWithTag("Target");
        foreach (GameObject target in allTargets)
        {
            if (!target.name.Contains("Flee"))
            {
                target.GetComponent<MeshRenderer>().material = defaultTargetMaterial;
            }
        }
        Selection.selectionChanged += HighlightPath;
    }

    // Change the color of the target in the path of the selected agent to highlight them when selecting the agent in the editor
    static void  HighlightPath()
    {
        /*GameObject selected = Selection.activeGameObject;
        if(selected != null && selected.GetComponent<RLAgent>() != null)
        {
            GameObject[] allTargets = GameObject.FindGameObjectsWithTag("Target");
            foreach(GameObject target in allTargets)
            {
                if (!target.name.Contains("Flee"))
                {
                    target.GetComponent<MeshRenderer>().material = defaultTargetMaterial;
                }
            }
            RLAgent.action[] lista = selected.GetComponent<RLAgent>().goalAction;
            foreach (RLAgent.action step in lista)
            {
                step.goalLocation.GetComponent<MeshRenderer>().material = highlightTargetMaterial;
            }
        }
        if (selected != null && selected.GetComponent<AIControlAgents>() != null)
        {
            GameObject[] allTargets = GameObject.FindGameObjectsWithTag("Target");
            foreach (GameObject target in allTargets)
            {
                if (!target.name.Contains("Flee"))
                {
                    target.GetComponent<MeshRenderer>().material = defaultTargetMaterial;
                }
            }
            AIControlAgents.action[] lista = selected.GetComponent<AIControlAgents>().goalAction;
            foreach (AIControlAgents.action step in lista)
            {
                if (step.goalLocation.GetComponent<MeshRenderer>() != null)
                {
                    step.goalLocation.GetComponent<MeshRenderer>().material = highlightTargetMaterial;
                }
            }
        }
        else
        {
            GameObject[] allTargets = GameObject.FindGameObjectsWithTag("Target");
            foreach (GameObject target in allTargets)
            {
                if (!target.name.Contains("Flee"))
                {
                    target.GetComponent<MeshRenderer>().material = defaultTargetMaterial;
                }
            }
        }*/
    }
}