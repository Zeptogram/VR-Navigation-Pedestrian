using UnityEngine;

public class ObjectiveTrigger : MonoBehaviour
{
    [Tooltip("Artifact to trigger")]
    public Artifact linkedArtifact;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente")) 
        {
            int agentId = other.gameObject.GetInstanceID();
            linkedArtifact?.Use(agentId);
        }
    }
}
