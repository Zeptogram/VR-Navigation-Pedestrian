using UnityEngine;

public class ColliderBlocker : MonoBehaviour
{
    [Tooltip("Collider dell'agente")]
    public CapsuleCollider agenteCollider;

    [Tooltip("Collider dell'agente da bloccare")]
    public CapsuleCollider colliderBlockerCollider;

    void Start()
    {
        ///Metodo per evitare le collisioni
        Physics.IgnoreCollision(agenteCollider, colliderBlockerCollider, true);
    }
}
