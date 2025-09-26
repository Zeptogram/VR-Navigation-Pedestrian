using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour
{
    [Tooltip("Range dei magneti")]
    public float range = 0.4f;
    [Range(0.1f, 10)]
    [Tooltip("Distanza tra magneti")]
    public float distanzaMin = 1.5f;
    [HideInInspector]
    public List<Vector3> magneti = new List<Vector3>();
    [Tooltip("Colore dei magneti")]
    public Color colore;

    private void Start()
    {
        CalcolaPunti();
    }



    void CalcolaPunti()
    {
        magneti = new List<Vector3>();
        float w = transform.localScale.x;
        float h = transform.localScale.z;

        float x = w / 2 - range;
        float z = h / 2 - range;

        ///Salvo 4 angoli
        magneti.Add(new Vector3(-x + transform.position.x, 1, +z + transform.position.z));
        magneti.Add(new Vector3(+x + transform.position.x, 1, +z + transform.position.z));
        magneti.Add(new Vector3(-x + transform.position.x, 1, -z + transform.position.z));
        magneti.Add(new Vector3(+x + transform.position.x, 1, -z + transform.position.z));

        int nw = Mathf.FloorToInt((w - (2 * range)) / distanzaMin);
        int nh = Mathf.FloorToInt((h - (2 * range)) / distanzaMin);

        float distX = 2 * x / nw;
        float distZ = 2 * z / nh;

        float iteraX = -x;
        float iteraZ = -z;
        for (int i = 0; i < nw - 1; i++)
        {
            iteraX += distX;
            magneti.Add(new Vector3(iteraX + transform.position.x, 1, +z + transform.position.z));
            magneti.Add(new Vector3(iteraX + transform.position.x, 1, -z + transform.position.z));
        }
        for (int i = 0; i < nh - 1; i++)
        {
            iteraZ += distZ;
            magneti.Add(new Vector3(+x + transform.position.x, 1, +iteraZ + transform.position.z));
            magneti.Add(new Vector3(-x + transform.position.x, 1, +iteraZ + transform.position.z));
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            CalcolaPunti();
        }
        Gizmos.color = colore;
        foreach (var item in magneti)
        {
            Gizmos.DrawSphere(item, range);
        }

    }

}
