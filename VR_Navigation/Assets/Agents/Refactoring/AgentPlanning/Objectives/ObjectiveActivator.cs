using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ObjectiveActivator : MonoBehaviour
{
    [Header("Obiettivi da gestire")]
    [SerializeField] private List<GameObject> objectives = new List<GameObject>();
    
    private int numToActivate;
    [Header("Configurazione attivazione")]
    [SerializeField] private int numToAssign = 3; // Numero di obiettivi da assegnare ad ogni gruppo di agenti
    
    [Header("Collegamenti")]
    [SerializeField] private ObjectivePositioner objectivePositioner;
    [SerializeField] private EnvironmentPlanning currentEnvironment;
    [SerializeField] private bool usePositioner = true;
    
    // Lista degli obiettivi attualmente attivi
    private List<GameObject> activeObjectives = new List<GameObject>();
    
    private void Start()
    {
        // Se non è stato assegnato manualmente, cerca l'ambiente nel parent
        if (currentEnvironment == null)
        {
            currentEnvironment = GetComponentInParent<EnvironmentPlanning>();
            if (currentEnvironment == null)
            {
                Debug.LogWarning("Nessun ambiente trovato. La ricerca degli obiettivi non funzionerà correttamente.");
                return;
            }
        }
        
        // Registrazione agli eventi dell'ambiente
        if (currentEnvironment != null)
        {
            currentEnvironment.allAgentsInitialized += OnAllAgentsInitialized;
        }
        
        // Se usePositioner è true ma objectivePositioner non è assegnato, cerca di trovarlo
        if (usePositioner && objectivePositioner == null)
        {
            objectivePositioner = GetComponentInParent<ObjectivePositioner>();
            if (objectivePositioner == null)
            {
                objectivePositioner = FindObjectOfType<ObjectivePositioner>();
            }
            
            // Se ancora non lo trova, disabilita l'uso del posizionatore
            if (objectivePositioner == null)
            {
                usePositioner = false;
                Debug.Log("Nessun ObjectivePositioner trovato. ObjectiveActivator funzionerà in modalità standalone.");
            }
        }
        
        // Cerca gli agenti solo nell'ambiente corrente
        var agents = currentEnvironment.GetComponentsInChildren<RLAgentPlanning>();
        foreach (var agent in agents)
        {
            agent.agentTerminated += OnAgentTerminated;
        }
        
        // Se gli obiettivi non sono stati assegnati manualmente, cerca solo nell'ambiente corrente
        if (objectives.Count == 0)
        {
            // Cerca oggetti con tag "Obiettivo" solo nell'ambiente corrente
            Transform[] allTransformsInEnvironment = currentEnvironment.GetComponentsInChildren<Transform>();
            foreach (Transform t in allTransformsInEnvironment)
            {
                if (t.CompareTag("Obiettivo"))
                {
                    objectives.Add(t.gameObject);
                }
            }
            
            if (objectives.Count == 0)
            {
                Debug.LogError("Nessun obiettivo con tag 'Obiettivo' trovato nell'ambiente corrente.");
            }
            else
            {
                Debug.Log($"Trovati {objectives.Count} obiettivi con tag 'Obiettivo' nell'ambiente corrente.");
            }
        }
        
        // Disattiva tutti gli obiettivi all'inizio
        DisableAllObjectives();
        
    }
    
    // Nuovo metodo per gestire l'evento di inizializzazione degli agenti
    private void OnAllAgentsInitialized()
    {
        // Dopo che tutti gli agenti sono stati inizializzati, notifica loro degli obiettivi attivi
        ActivateRandomObjectives();
        Debug.Log("[ObjectiveActivator] Ricevuto evento allAgentsInitialized, distribuisco obiettivi.");
    }
    
    private void OnAgentTerminated(float reward, EnvironmentPlanning env)
    {
        // Verifica che l'evento provenga dall'ambiente corrente
        if (env != currentEnvironment && currentEnvironment != null)
        {
            return;
        }
    }
    
    public void ActivateRandomObjectives()
    {
        if (objectives.Count == 0)
        {
            Debug.LogWarning("Nessun obiettivo disponibile per l'attivazione");
            return;
        }
        
        // Prima disattiva tutti gli obiettivi
        DisableAllObjectives();
        activeObjectives.Clear();
        
        
        // Crea una copia della lista e la mescola
        List<GameObject> shuffledObjectives = new List<GameObject>(objectives);
        for (int i = 0; i < shuffledObjectives.Count; i++) 
        {
            int randomIndex = Random.Range(i, shuffledObjectives.Count);
            GameObject temp = shuffledObjectives[i];
            shuffledObjectives[i] = shuffledObjectives[randomIndex];
            shuffledObjectives[randomIndex] = temp;
        }
        numToActivate = objectives.Count;
        // Attiva solo il numero richiesto di obiettivi
        for (int i = 0; i < numToActivate; i++)
        {
            GameObject objective = shuffledObjectives[i];
            
            // Se stiamo usando il posizionatore, riposiziona gli obiettivi
            if (usePositioner && objectivePositioner != null)
            {
                Vector3 position = objectivePositioner.GenerateSafePosition();
                objective.transform.position = position;
            }
            // Altrimenti, mantieni la posizione originale
            
            objective.SetActive(true);
            activeObjectives.Add(objective);
        }
        
        // Notifica gli agenti del cambiamento degli obiettivi
        NotifyAgentsOfObjectiveChange();
        
        Debug.Log($"Attivati {numToActivate} obiettivi su {objectives.Count} disponibili");
    }
    
    public void NotifyAgentsOfObjectiveChange()
    {
        if (currentEnvironment == null)
        {
            Debug.LogWarning("[ObjectiveActivator] currentEnvironment è null!");
            return;
        }

        var agents = currentEnvironment.GetComponentsInChildren<RLAgentPlanning>();
        var gruppiPresenti = agents.Select(a => a.group).Distinct().ToList();

        // Ottieni o aggiungi ObjectiveColorManager
        ObjectiveColorManager colorManager = currentEnvironment.GetComponent<ObjectiveColorManager>();
        if (colorManager == null)
        {
            colorManager = currentEnvironment.gameObject.AddComponent<ObjectiveColorManager>();
        }

        // Pulisci le assegnazioni precedenti
        colorManager.ClearAllAssignments();

        Dictionary<Group, List<GameObject>> obiettiviPerGruppo = new Dictionary<Group, List<GameObject>>();

        foreach (var gruppo in gruppiPresenti)
        {
            List<GameObject> shuffled = new List<GameObject>(activeObjectives);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, shuffled.Count);
                var temp = shuffled[i];
                shuffled[i] = shuffled[rnd];
                shuffled[rnd] = temp;
            }

            int num = Mathf.Min(numToAssign, shuffled.Count);
            var lista = shuffled.Take(num).ToList();

            obiettiviPerGruppo[gruppo] = lista;
        }

        foreach (var agent in agents)
        {
            var objectiveHandler = agent.GetComponent<ObjectiveInteractionHandler>();
            if (objectiveHandler != null)
            {
                if (obiettiviPerGruppo.TryGetValue(agent.group, out var lista))
                {
                    objectiveHandler.SetObjectives(lista);
                    colorManager.RegisterAgentObjectives(agent, lista);
                }
                else
                {
                    objectiveHandler.SetObjectives(new List<GameObject>());
                }
            }
        }
    }
    
    public void DisableAllObjectives()
    {
        foreach (var objective in objectives)
        {
            if (objective != null)
            {
                objective.SetActive(false);
            }
        }
    }
    
    public void DeactivateObjective(GameObject objective)
    {
        if (objective != null && activeObjectives.Contains(objective))
        {
            activeObjectives.Remove(objective);
            objective.SetActive(false);
            Debug.Log($"Obiettivo {objective.name} disattivato da ObjectiveActivator");
        }
    }
    
    public List<GameObject> GetActiveObjectives()
    {
        return activeObjectives;
    }
    
    // Proprietà pubblica per abilitare/disabilitare l'uso del posizionatore a runtime
    public bool UsePositioner
    {
        get { return usePositioner; }
        set { usePositioner = value; }
    }
    
    private void OnDisable()
    {
        // Disiscriviti dagli eventi
        if (currentEnvironment != null)
        {
            currentEnvironment.allAgentsInitialized -= OnAllAgentsInitialized;
            
            var agents = currentEnvironment.GetComponentsInChildren<RLAgentPlanning>();
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    agent.agentTerminated -= OnAgentTerminated;
                }
            }
        }
    }
}