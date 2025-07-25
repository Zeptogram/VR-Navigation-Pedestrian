using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactTrigger : MonoBehaviour
{
    bool debugging = false;
    [SerializeField] private Artifact targetArtifact;

    void Start()
    {
        // If no artifact is assigned, try to find it in parent or same GameObject
        if (targetArtifact == null)
        {
            targetArtifact = GetComponentInParent<Artifact>();
            if (targetArtifact == null)
                targetArtifact = GetComponent<Artifact>();
        }

        if (targetArtifact == null)
            Debug.LogWarning($"[ArtifactTrigger] No Artifact component found for {gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugging)
        {
            if (other.CompareTag("Agente"))
            {
                if (targetArtifact != null)
                {
                    Debug.Log($"Artifact '{targetArtifact.ArtifactName}' triggered by Agent {other.gameObject.name}");
                }
                else
                {
                    Debug.Log("Artifact Triggered by Agent (no artifact reference)");
                }
            }
        }
    }
}
    
