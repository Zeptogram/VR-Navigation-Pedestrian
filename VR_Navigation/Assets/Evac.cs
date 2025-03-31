using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
//using UnityEngine.InputSystem;

public class Evac : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject EvacCamera;
    [SerializeField] private Transform EvacPoint;

    [SerializeField] private Transform standingPoint;
    private GameObject fade;

    bool triggered = false;

    private void Start()
    {
        fade = GameObject.Find("fade");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.tag);
        if(other.CompareTag("Player"))
        {
            Debug.Log("Enter trigger Player");
            if (triggered == false)
            {
                fade.SetActive(true);
                Transform avatar = other.transform;
                

                // teleport to standing point
                //avatar.position = standingPoint.position;
                //avatar.rotation = standingPoint.rotation;

                // disable player input
                //avatar.GetComponent<PlayerInput>().enabled = false;

                

                triggered = true;
                StartCoroutine(RecoverWithDelay(5f));
                // StartCoroutine(FadeToBlack(5f));
            }
        }
    }

    private void Recover()
    {
        mainCamera.SetActive(true);
        EvacCamera.SetActive(false);
        foreach(RLAgent agent in GameObject.FindObjectsOfType<RLAgent>())
        {
            agent.flee();
        }
        foreach (NavMeshAgent agent in GameObject.FindObjectsOfType<NavMeshAgent>())
        {
            agent.gameObject.GetComponent<AIControlAgents>().Flee(EvacPoint);  
        }
    }

    // Fade the screen to black and end the application
    IEnumerator FadeToBlack(float wait)
    {
        fade.transform.position = mainCamera.transform.forward;
        for (float timer = wait; timer >= 0; timer -= Time.deltaTime)
        {
            Color color = fade.GetComponent<MeshRenderer>().material.color;
            print(color);
            color.a += Time.deltaTime / wait;
            fade.GetComponent<MeshRenderer>().material.color = color;
            fade.transform.position = mainCamera.transform.position + mainCamera.transform.forward;
            yield return null;
        }
        UnityEditor.EditorApplication.isPlaying = false;
    }

    // Changes temporarely the point of view of the user to the EvacCamera to observe  the scene from another perspective
    IEnumerator RecoverWithDelay(float delay)
    {
        mainCamera.SetActive(false);
        EvacCamera.SetActive(true);
        yield return new WaitForSeconds(delay);
        Recover();
    }

}
