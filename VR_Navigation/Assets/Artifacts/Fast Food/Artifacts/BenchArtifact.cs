using UnityEngine;
using System.Collections;
using TMPro;
using System.Linq;
using UnityEngine.AI;

public class BenchArtifact : Artifact
{
    [Header("Bench Settings")]
    [SerializeField] private Transform sittingPosition;
    [Header("Visual Food Object")]
    [SerializeField] private GameObject hamburgerObject;

    protected override void Init()
    {
        if (sittingPosition == null)
            sittingPosition = transform;
        if (hamburgerObject != null)
            hamburgerObject.SetActive(false);

    }

    public override void Use(int agentId, params object[] args)
    {
        // Args safety
        if (args == null || args.Length == 0 || args[0] is not GameObject agent)
        {
            Debug.LogWarning($"[{ArtifactName}] Use() expected args[0] as GameObject");
            return;
        }

        Debug.Log($"[{ArtifactName}] Agent {agentId} sitting at bench");

        var nav = agent.GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            nav.enabled = false;
            agent.transform.position = sittingPosition.position;
            nav.updateRotation = false;
            agent.transform.rotation = sittingPosition.rotation;
            if (hamburgerObject != null)
                hamburgerObject.SetActive(true);
        }
      
    }
}