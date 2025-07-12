using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//[ExecuteInEditMode]
public class Sensor : MonoBehaviour
{
    [Tooltip("Sensor Name")]
    public string myName;
    [Header("True to see Gizmos")]
    public bool showGizmos = true;
    [Header("Layer Map")]
    [Tooltip("Select the layers that sensors can see")]
    public LayerMask rayLayeredMask;
    private int maxAngle;
    private readonly int rayLength = 100;
    [NonSerialized] public readonly float delta = 1.5f;
    private RLAgentScript agent;

    public struct ObjectHit{
        public GameObject gameObject;
        public float distance;
        public ObjectHit(GameObject gameObject, float distance){
            this.gameObject = gameObject;
            this.distance = distance;
        }
    }

    private void OnDrawGizmos(){
        if (agent == null) agent = GetComponent<RLAgentScript>();
        maxAngle = agent.viewingAngle;
        if (showGizmos){
            DrawRays();
        }
    }

    private void Start(){
        agent = GetComponent<RLAgentScript>();
        maxAngle = agent.viewingAngle;
    }

    private void DrawRays(){
        Vector3 startingPos = new Vector3(transform.position.x, 1f, transform.position.z);

        float currentAngle = 0;
        //angles starting from center and going to the external 
        for (float i = delta; currentAngle <= maxAngle; i += delta){
            //for used to have both left and right angles
            foreach (int val in new int[] { 1, -1 }){
                float angle = transform.eulerAngles.y + currentAngle * val;
                Vector3 rayDirection = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), 0, Mathf.Cos(Mathf.Deg2Rad * angle));
                Vector3 endPos = startingPos + rayDirection * rayLength;

                //cast the ray from agent position to end position
                Physics.Linecast(startingPos, endPos, out RaycastHit info, rayLayeredMask);

                Vector3 hitPos = new Vector3(info.point.x, 1f, info.point.z);

                //color the rays
                if (info.collider.gameObject.CompareTag("Agente")){ Gizmos.color = Color.yellow;
                }else if (info.collider.gameObject.CompareTag("Muro")) Gizmos.color = new Color(1, 1, 1, 0.05f);
                else if (info.collider.gameObject.CompareTag("Target")){
                    if (info.collider.gameObject.name == agent.finalTarget) Gizmos.color = Color.green;
                    else if (agent.reachedTargets.Any(myT => myT.target == info.collider.gameObject)) Gizmos.color = Color.red;
                    else if (!agent.reachedTargets.Any(myT => myT.target == info.collider.gameObject)) Gizmos.color = Color.cyan;
                }else Debug.LogWarning("error during hit detection tag: " + info.collider.gameObject.tag);
                Gizmos.DrawLine(startingPos, info.point);
            }
            //block the angle to the max angle possible
            if (currentAngle != maxAngle && currentAngle + i > maxAngle) currentAngle = maxAngle;
            else currentAngle += i;
        }
    }

    //returns the info taken by the rays hits
    public List<ObjectHit> GetRaysInfo(){
        List<ObjectHit> hitObjects = new List<ObjectHit>();

        Vector3 startingPos = new Vector3(transform.position.x, 1f, transform.position.z);

        //TODO Refactoring? maybe unite this and drawrays functions (or create a function for the distribution)?
        //same distribution as before
        float currentAngle = 0;
        for (float i = delta; currentAngle <= maxAngle; i += delta){
            foreach (int val in new int[] { 1, -1 }){
                float angle = transform.eulerAngles.y + currentAngle * val;
                Vector3 rayDirection = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), 0, Mathf.Cos(Mathf.Deg2Rad * angle));
                Vector3 endPos = startingPos + rayDirection * rayLength;

                //get rays info                     
                Physics.Linecast(startingPos, endPos, out RaycastHit info, rayLayeredMask);

                if (info.collider == null){
                    Debug.LogError("Error, or maybe ray too short");
                    hitObjects.Add(new ObjectHit(null, 100));
                }else{
                    Vector3 hitPos = new Vector3(info.point.x, 1f, info.point.z);
                    float distance = Vector3.Distance(startingPos, hitPos);

                    if (distance > 100 || distance < 0) Debug.LogError("Error, distance: " + distance + " with: " + info.collider.gameObject.name);

                    hitObjects.Add(new ObjectHit(info.collider.gameObject, distance));
                }
            }
            //block the angle to the max angle possible
            if (currentAngle != maxAngle && currentAngle + i > maxAngle) currentAngle = maxAngle;
            else currentAngle += i;
        }

        hitObjects.RemoveAt(0); //remove the dopple ray at 0 
        return hitObjects;
    }
}
