using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;

public class ObjectivePositioner : MonoBehaviour
{
    [SerializeField] private GameObject objective;
    
    [Header("Configurazione posizionamento")]
    [SerializeField] private float rangeXMin = -8.5f;
    [SerializeField] private float rangeXMax = 8.5f;
    [SerializeField] private float rangeZMin = -8.5f;
    [SerializeField] private float rangeZMax = 8.5f;
    [SerializeField] private float minDistanceFromWalls = 0.5f;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private float minDistanceFromTarget = 2f; // Distanza minima dall'obiettivo finale
    
    // Lista di target aggiuntivi da considerare per la distanza minima
    [SerializeField] private List<Transform> additionalTargets = new List<Transform>();
    [SerializeField] private string targetTag = "Target"; // Tag per cercare automaticamente i target
    
    // Riferimento all'ambiente a cui appartiene questo posizionatore
    [SerializeField] private EnvironmentPlanning currentEnvironment;
    
    // Evento per notificare quando l'obiettivo è stato riposizionato
    public event Action<Vector3> OnObjectiveRepositioned;
    
    // Lista di agenti da monitorare
    private List<RLAgentPlanning> monitoredAgents = new List<RLAgentPlanning>();
    
    void Start()
    {
        // Se non è stato assegnato manualmente, cerca l'ambiente nel parent
        if (currentEnvironment == null)
        {
            currentEnvironment = GetComponentInParent<EnvironmentPlanning>();
            if (currentEnvironment == null)
            {
                Debug.LogWarning("Nessun ambiente associato all'ObjectivePositioner. " +
                                "Il riposizionamento potrebbe avvenire con eventi di altri ambienti.");
            }
        }
        
        // Trova tutti gli agenti nella scena
        var agents = FindObjectsOfType<RLAgentPlanning>();
        foreach (var agent in agents)
        {
            RegisterAgent(agent);
        }
        
        // Se non abbiamo agenti registrati, prova a monitorare future istanze
        if (monitoredAgents.Count == 0)
        {
            Debug.LogWarning("Nessun agente trovato da ObjectivePositioner. Cercherò di monitorare nuovi agenti.");
            
            // Monitora eventuali eventi di inizializzazione dell'ambiente che potrebbero creare agenti
            var envs = FindObjectsOfType<EnvironmentPlanning>();
            foreach (var env in envs)
            {
                env.allAgentsInitialized += OnAllAgentsInitialized;
            }
        }
        
        // Trova e registra i target nella scena
        FindAndRegisterTargets();
        
        // Se c'è un ObjectiveActivator, lascia che sia lui a gestire l'attivazione degli obiettivi
        // altrimenti, mantieni il comportamento originale
        if (GetComponent<ObjectiveActivator>() == null && objective != null)
        {
            RepositionObjective();
        }
    }
    
    // Cerca e registra tutti i target nell'ambiente
    private void FindAndRegisterTargets()
    {
        if (!string.IsNullOrEmpty(targetTag))
        {
            GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (var targetObj in targetObjects)
            {
                if (!additionalTargets.Contains(targetObj.transform))
                {
                    additionalTargets.Add(targetObj.transform);
                    Debug.Log($"ObjectivePositioner: registrato target {targetObj.name}");
                }
            }
        }
    }
    
    private void OnAllAgentsInitialized()
    {
        // Cerca nuovamente gli agenti dopo l'inizializzazione dell'ambiente
        var agents = FindObjectsOfType<RLAgentPlanning>();
        foreach (var agent in agents)
        {
            if (!monitoredAgents.Contains(agent))
            {
                RegisterAgent(agent);
            }
        }
    }
    
    private void RegisterAgent(RLAgentPlanning agent)
    {
        if (!monitoredAgents.Contains(agent))
        {
            agent.agentTerminated += OnAgentTerminated;
            monitoredAgents.Add(agent);
            Debug.Log($"ObjectivePositioner: registrato agente {agent.name}");
        }
    }
    
    private void OnAgentTerminated(float reward, EnvironmentPlanning env)
    {
        // Verifica che l'ambiente corrente sia valido
        if (currentEnvironment == null)
        {
            Debug.LogWarning("currentEnvironment è null in OnAgentTerminated");
            return;
        }

        // Verifica che l'ambiente passato non sia null
        if (env == null)
        {
            Debug.LogWarning("Parametro 'env' è null in OnAgentTerminated");
            return;
        }

        // Verifica che l'evento provenga dall'ambiente corrente
        if (env != currentEnvironment)
        {
            Debug.Log($"Ignorato evento di terminazione da ambiente diverso: {env.name}");
            return;
        }

        // Se c'è un ObjectiveActivator, lascia che sia lui a gestire il riposizionamento
        // altrimenti, mantieni il comportamento originale
        if (GetComponent<ObjectiveActivator>() == null && objective != null)
        {
            Debug.Log("Agente terminato nell'ambiente corrente, riposiziono l'obiettivo");
            RepositionObjective();
        }
    }
    
    public Vector3 RepositionObjective()
    {
        if (objective == null) 
        {
            Debug.LogError("Nessun obiettivo assegnato in ObjectivePositioner");
            return Vector3.zero;
        }
        
        Vector3 objectivePosition = GenerateSafePosition();
        objective.transform.localPosition = objectivePosition;
        objective.SetActive(true);
        
        // Notifica altri componenti della nuova posizione
        OnObjectiveRepositioned?.Invoke(objectivePosition);
        
        Debug.Log($"Obiettivo riposizionato a {objectivePosition} e riattivato");
        return objectivePosition;
    }

    public Vector3 GenerateSafePosition()
    {
        Vector3 position;
        int attempts = 0;

        do
        {
            float posX = Random.Range(rangeXMin, rangeXMax);
            float posZ = Random.Range(rangeZMin, rangeZMax);
            position = new Vector3(posX, 0f, posZ);
            attempts++;

            // Evita un loop infinito se non trova posizioni valide
            if (attempts > 100)
            {
                Debug.LogWarning("Raggiunto numero massimo di tentativi per posizionare l'obiettivo");
                break;
            }

        } while (
            Physics.CheckSphere(position, minDistanceFromWalls, wallLayerMask) ||
            TooCloseToAnyTarget(position)
        );

        return position;
    }

    // Verifica se la posizione è troppo vicina a qualsiasi agente o target
    private bool TooCloseToAnyTarget(Vector3 position)
    {
        // Verifica distanza dagli agenti
        foreach (var agent in monitoredAgents)
        {
            if (agent != null && agent.gameObject.activeSelf)
            {
                float distance = Vector3.Distance(position, agent.transform.position);
                if (distance < minDistanceFromTarget)
                {
                    return true; // Troppo vicino a un agente
                }
            }
        }
        
        // Verifica distanza dai target aggiuntivi
        foreach (var target in additionalTargets)
        {
            if (target != null && target.gameObject.activeSelf)
            {
                float distance = Vector3.Distance(position, target.position);
                if (distance < minDistanceFromTarget)
                {
                    return true; // Troppo vicino a un target
                }
            }
        }

        return false;
    }
    
    // Metodo pubblico per registrare manualmente un agente
    public void RegisterExternalAgent(RLAgentPlanning agent)
    {
        RegisterAgent(agent);
    }
    
    // Metodo pubblico per registrare manualmente un target
    public void RegisterTarget(Transform target)
    {
        if (!additionalTargets.Contains(target))
        {
            additionalTargets.Add(target);
            Debug.Log($"ObjectivePositioner: registrato target {target.name}");
        }
    }
    
    // Metodo pubblico per impostare manualmente l'ambiente
    public void SetEnvironment(EnvironmentPlanning env)
    {
        currentEnvironment = env;
        Debug.Log($"Impostato ambiente {env.name} per ObjectivePositioner");
    }
    
    // Metodo per accedere all'obiettivo (usato da ObjectiveActivator)
    public GameObject GetObjective()
    {
        return objective;
    }
    
    private void OnDisable()
    {
        // Disiscriviti da tutti gli agenti monitorati
        foreach (var agent in monitoredAgents)
        {
            if (agent != null)
            {
                agent.agentTerminated -= OnAgentTerminated;
            }
        }
        
        // Disiscriviti dagli ambienti
        var envs = FindObjectsOfType<EnvironmentPlanning>();
        foreach (var env in envs)
        {
            env.allAgentsInitialized -= OnAllAgentsInitialized;
        }
        
        monitoredAgents.Clear();
    }
}