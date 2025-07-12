using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AgentScriptNavMesh : MonoBehaviour
{
    private NavMeshAgent agent;
    //private ControllerScriptSecondo controller;
    [Tooltip("Quanto dev'essere distante dal target l'agente per prenderlo")]
    public float distanzaDaTarget = 2f;
    [Tooltip("Numero di destinazioni possibili per l'agente")]
    public int nDestinazioni = 4;
    [Tooltip("Variabile per ricalcolare il percorso")]
    public bool isRicalcola;

    //public bool passatoPrimo = false;

    private float distanza;
    private Vector3 myTarget;

    private Vector3 oldPosition;
    [Header("Target finale dell'agente")]
    [Tooltip("Settare il target finale dell'agente")]
    public GameObject targetFinaleNMA;

    [HideInInspector]
    public TargetScript targetScript;

    private float tempoUscita;

    Vector3 startingPos;
    Quaternion startingRot;
    ///Nome del target finale
    //private string targetFinale; ///Da inizializzare da ambiente

    private Animator animator;

    void Start()
    {

        startingPos = transform.position;
        startingRot = transform.rotation;
        animator = GetComponent<Animator>();

        if (transform.parent.name == "Sotto")
        {
            //targetFinale = "TargetFine1";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.red;
        }
        else if (transform.parent.name == "Sopra")
        {
            //targetFinale = "TargetFine2";
            GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.cyan;
        }
        targetScript = targetFinaleNMA.GetComponent<TargetScript>();

        agent = GetComponent<NavMeshAgent>();
        if (nDestinazioni == -1)
            nDestinazioni = targetScript.magneti.Count;

        agent.SetDestination(GestisciDestinazione());
        if (isRicalcola)
            StartCoroutine(RicalcoloPercorso());
    }

    IEnumerator RicalcoloPercorso()
    {
        while (true)
        {
            if (distanza >= 8)
                agent.SetDestination(GestisciDestinazione());
            yield return new WaitForSeconds(2);
        }
    }


    private Vector3 GestisciDestinazione()
    {
        List<(Vector3, float)> magnDist = new List<(Vector3, float)>();
        List<Vector3> myMagneti = targetScript.magneti;

        foreach (var mag in myMagneti)
        {
            float dist = Vector3.Distance(transform.position, mag);
            magnDist.Add((mag, dist));
        }
        magnDist.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        ///Tengo solo i primi nDestinazioni magneti più vicini es. 20, 30, 40
        magnDist = magnDist.GetRange(0, nDestinazioni);
        ///Calcolo la somma => 90
        float sum = magnDist.Sum(x => x.Item2);
        ///Divido la somma per i valori per ottenere distanze minori = prob maggiori => 4.5, 3, 2.25
        for (int i = 0; i < nDestinazioni; i++)
        {
            magnDist[i] = (magnDist[i].Item1, sum / magnDist[i].Item2);
        }

        ///Sommo per ottenere percentuali => 9.75
        sum = magnDist.Sum(x => x.Item2);
        ///Prendo percentuale casuale
        float rng = Random.Range(0, 1f);

        for (int i = 0; i < nDestinazioni; i++)
        {
            ///Calcolo percentuali => 0.46, 0.3, 0.24
            float fract = magnDist[i].Item2 / sum;
            ///Sottraggo dal numero la percentuale, quando ho < 0 esco
            rng -= fract;
            if (rng <= 0) return magnDist[i].Item1;
        }
#if UNITY_EDITOR


        Debug.LogError("Non dovresti essere qui");
        UnityEditor.EditorApplication.isPaused = true;
#endif

        return Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        string nomeCollider = other.gameObject.name;
        if (nomeCollider == targetScript.name)
        {
            Fine();
        }
    }

    private void FixedUpdate()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon && agent.velocity.normalized != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);

        distanza = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(agent.destination.x, 0, agent.destination.z));
        if (distanza <= distanzaDaTarget)
            Fine();
    }

    private void Fine()
    {
        GetComponent<PythonAgent>().WriteStats(4f, "Nav");
        gameObject.SetActive(false);
    }
}
