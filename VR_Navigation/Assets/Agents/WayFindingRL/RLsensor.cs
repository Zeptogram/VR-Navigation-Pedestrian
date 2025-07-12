using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//[ExecuteInEditMode]
public class RLsensor : MonoBehaviour
{
    [NonSerialized] public readonly float delta = 1.5f;
    public Color gizmoColor = new Color(0f, 0f, 0f, 0.1f);
    public int numberOfRays = 1;
    public float rayLength = 30;
    Group group;
    //RLAgent agent;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 forward = transform.forward;
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = i * (90f / numberOfRays);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward;
            Gizmos.DrawRay(transform.position, direction * rayLength);
            RaycastHit[] hits;

            hits = Physics.RaycastAll(transform.position, direction, rayLength);
            var previusPosition = transform.position;
            for (int j = 0; j < hits.Length; j++)
            {

                RaycastHit hit = hits[j];
                GameObject hitGameObj = hit.collider.gameObject;

                String hitTag = hitGameObj.tag;
                Target target = hitGameObj.GetComponent<Target>();
                if (hitTag == "Target")
                {
                    if (target.group == group || target.group == Group.Generic)
                    {
                        Debug.DrawRay(previusPosition, direction * hit.distance, Color.green);
                    }
                    else
                    {
                        Debug.DrawRay(previusPosition, direction * hit.distance, Color.red);
                    }
                }
                else if (hitTag == "Agente")
                {
                    Debug.DrawRay(previusPosition, direction * hit.distance, Color.yellow);
                    //break;

                }
                else 
                {
                    Debug.DrawRay(previusPosition, direction * hit.distance, gizmoColor);
                    //break;
                }

                previusPosition = hit.point;

            }
        }
    }

    private void Start()
    {
        //agent = transform.GetComponent<RLAgent>();
        //group = agent.group;
    }

    private void DrawRays()
    {

    }

}