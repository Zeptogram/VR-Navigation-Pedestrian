using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class MonitorArtifact : Artifact, IArtifactConnectable
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI listPrepOrders;
    [SerializeField] private TextMeshProUGUI listReadyOrders;
    
    [Header("Visual Food Objects")]
    [SerializeField] private GameObject hamburgerObject;
    [SerializeField] private GameObject hotdogObject;
    
    [Header("Food Assignment Logic")]
    [SerializeField] private bool useOrderBasedFood = true; // Hamburger odd orders, Hotdog even orders
    
    // Track connected totems
    private List<TotemArtifact> connectedTotems = new List<TotemArtifact>();
    
    // Order tracking with food type
    private List<int> ordersInPreparation = new List<int>();
    private Dictionary<int, FoodType> readyOrdersWithFood = new Dictionary<int, FoodType>();
    
    public enum FoodType
    {
        Hamburger,
        Hotdog
    }

    public Dictionary<int, FoodType> ReadyOrdersWithFood => new Dictionary<int, FoodType>(readyOrdersWithFood);
    public List<int> OrdersInPreparation => ordersInPreparation.ToList();
    public List<int> ReadyOrderIds => readyOrdersWithFood.Keys.ToList();
    
    // For Artifact Interface

    protected override void Init()
    {
        Debug.Log($"[{ArtifactName}] Monitor initialized");
        UpdateUI();
        UpdateFoodVisuals();
    }
    

    public override void Use(int agentId, params object[] args)
    {
        base.Use(agentId, args);

        // Se viene passato un orderId come primo argomento, prova a ritirare quell'ordine
        if (args != null && args.Length > 0 && args[0] is int orderId)
        {
            PickUpOrder(agentId, orderId);
        }
        else
        {
            Debug.LogWarning($"[{ArtifactName}] Use chiamato senza orderId: specificare l'ordine da ritirare.");
        }
    }

    public override object Observe(string propertyName)
    {
        switch (propertyName)
        {
            case "ordersInPreparation":
                return ordersInPreparation.ToList();
            case "ordersReady":
                return readyOrdersWithFood.Keys.ToList();
            case "readyOrdersWithFood":
                return new Dictionary<int, FoodType>(readyOrdersWithFood);
            case "hasReadyOrders":
                return readyOrdersWithFood.Count > 0;
            case "prepOrdersCount":
                return ordersInPreparation.Count;
            case "readyOrdersCount":
                return readyOrdersWithFood.Count;
            default:
                return base.Observe(propertyName);
        }
    }

    
    public void ConnectTo(Artifact other)
    {
        if (other is TotemArtifact totem)
            ConnectToTotem(totem);
    }

    public void DisconnectFrom(Artifact other)
    {
        if (other is TotemArtifact totem)
            DisconnectFromTotem(totem);
    }

    public void ConnectToTotem(TotemArtifact totem)
    {
        if (!connectedTotems.Contains(totem))
        {
            totem.OnSignal += HandleSignal;
            connectedTotems.Add(totem);
            Debug.Log($"[{ArtifactName}] Connected to totem: {totem.ArtifactName}");
        }
    }
    
    public void DisconnectFromTotem(TotemArtifact totem)
    {
        if (connectedTotems.Contains(totem))
        {
            totem.OnSignal -= HandleSignal;
            connectedTotems.Remove(totem);
            Debug.Log($"[{ArtifactName}] Disconnected from totem: {totem.ArtifactName}");
        }
    }

    private void HandleSignal(string signal, object data)
    {
        switch (signal)
        {
            case "orderPlaced":
                OrderPlacedData orderData = data as OrderPlacedData;
                if (orderData != null)
                {
                    AddOrderToPreparation(orderData.orderId);
                    Debug.Log($"[{ArtifactName}] Ordine #{orderData.orderId} da agente {orderData.agentId} aggiunto alla preparazione");
                }
                break;
                
            case "orderReady":
                int readyOrderId = (int)data;
                MoveOrderToReady(readyOrderId);
                Debug.Log($"[{ArtifactName}] Ordine #{readyOrderId} è pronto!");
                break;
                
            case "orderPickedUp":
                int pickedUpOrderId = (int)data;
                RemoveOrderFromReady(pickedUpOrderId);
                Debug.Log($"[{ArtifactName}] Ordine #{pickedUpOrderId} ritirato");
                break;
        }
    }
    
    private void AddOrderToPreparation(int orderId)
    {
        if (!ordersInPreparation.Contains(orderId))
        {
            ordersInPreparation.Add(orderId);
            UpdateUI();
        }
    }
    
    private void MoveOrderToReady(int orderId)
    {
        if (ordersInPreparation.Contains(orderId))
        {
            ordersInPreparation.Remove(orderId);
            
            // Assign food type based on order ID
            FoodType foodType = useOrderBasedFood ? 
                (orderId % 2 == 1 ? FoodType.Hamburger : FoodType.Hotdog) : 
                GetNextFoodType();
                
            readyOrdersWithFood[orderId] = foodType;
            
            Debug.Log($"[{ArtifactName}] Ordine #{orderId} pronto come {foodType}");
            
            UpdateUI();
            UpdateFoodVisuals();
        }
    }
    
    private FoodType GetNextFoodType()
    {
        // Alternate between food types
        int readyCount = readyOrdersWithFood.Count;
        return readyCount % 2 == 0 ? FoodType.Hamburger : FoodType.Hotdog;
    }
    
    /// <summary>
    /// Updates the visibility of food objects based on ready orders
    /// </summary>
    private void UpdateFoodVisuals()
    {
        var foodCounts = readyOrdersWithFood.Values
            .GroupBy(food => food)
            .ToDictionary(g => g.Key, g => g.Count());
        
        bool showHamburger = foodCounts.ContainsKey(FoodType.Hamburger) && foodCounts[FoodType.Hamburger] > 0;
        bool showHotdog = foodCounts.ContainsKey(FoodType.Hotdog) && foodCounts[FoodType.Hotdog] > 0;
        
        if (hamburgerObject != null)
        {
            hamburgerObject.SetActive(showHamburger);
            Debug.Log($"[{ArtifactName}] Hamburger visibility: {showHamburger} (count: {foodCounts.GetValueOrDefault(FoodType.Hamburger, 0)})");
        }
        
        if (hotdogObject != null)
        {
            hotdogObject.SetActive(showHotdog);
            Debug.Log($"[{ArtifactName}] Hotdog visibility: {showHotdog} (count: {foodCounts.GetValueOrDefault(FoodType.Hotdog, 0)})");
        }
    }
    
    private void UpdateUI()
    {
        if (listPrepOrders != null)
        {
            string prepText = "";
            if (ordersInPreparation.Count > 0)
            {
                prepText += string.Join(", ", ordersInPreparation.Select(id => $"Order #{id}"));
            }
            else
            {
                prepText += "No Preparation Orders";
            }
            listPrepOrders.text = prepText;
        }
        
        if (listReadyOrders != null)
        {
            string readyText = "";
            if (readyOrdersWithFood.Count > 0)
            {
                var readyDescriptions = readyOrdersWithFood.Select(kvp => 
                    $"Order #{kvp.Key} ({kvp.Value})");
                readyText += string.Join(", ", readyDescriptions);
            }
            else
            {
                readyText += "No Ready Orders";
            }
            listReadyOrders.text = readyText;
        }
    }
    
    // GetOrderFoodType(int orderId): returns the food type for a specific order ID
    public FoodType? GetOrderFoodType(int orderId)
    {
        return readyOrdersWithFood.ContainsKey(orderId) ? readyOrdersWithFood[orderId] : (FoodType?)null;
    }
    
    // GetFoodTypeCounts(): returns a dictionary with counts of each food type in ready orders
    public Dictionary<FoodType, int> GetFoodTypeCounts()
    {
        return readyOrdersWithFood.Values
            .GroupBy(food => food)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
   
    
    // Method for agents to pick up ready orders
    public bool PickUpOrder(int agentId, int orderId)
    {
        if (readyOrdersWithFood.ContainsKey(orderId))
        {
            FoodType foodType = readyOrdersWithFood[orderId];
            Debug.Log($"[{ArtifactName}] Picking up {foodType} order #{orderId}");

            // Remove from ready orders
            readyOrdersWithFood.Remove(orderId);

            // Update UI and visuals
            UpdateUI();
            UpdateFoodVisuals();

            // Emit signal con dati strutturati (questo basta!)
            EmitSignal("orderPickedUp", new OrderPickedUpData(orderId, FindTotemNameForOrder(orderId)));

            Debug.Log($"[{ArtifactName}] {foodType} order #{orderId} ritirato da agente {agentId}");
            return true;
        }

        Debug.Log($"[{ArtifactName}] Ordine #{orderId} non trovato negli ordini pronti");
        return false;
    }
    
    // Get the first ready order (for agents who don't specify)
    public int? GetFirstReadyOrder()
    {
        return readyOrdersWithFood.Count > 0 ? readyOrdersWithFood.Keys.First() : (int?)null;
    }
    
    private void OnDestroy()
    {
        // Clean up connections when monitor is destroyed
        foreach (TotemArtifact totem in connectedTotems)
        {
            totem.OnSignal -= HandleSignal;
        }
        connectedTotems.Clear();
    }
    
    private void RemoveOrderFromReady(int orderId)
    {
        if (readyOrdersWithFood.ContainsKey(orderId))
        {
            FoodType foodType = readyOrdersWithFood[orderId];
            readyOrdersWithFood.Remove(orderId);
            Debug.Log($"[{ArtifactName}] {foodType} ordine #{orderId} rimosso dalla lista ready");
            UpdateUI();
            UpdateFoodVisuals();
        }
        else
        {
            Debug.LogWarning($"[{ArtifactName}] Tentativo di rimuovere ordine #{orderId} non presente nella lista ready");
        }
    }

    private string FindTotemNameForOrder(int orderId)
    {
        foreach (var totem in connectedTotems)
        {
            if (totem.HasOrder(orderId))
                return totem.ArtifactName;
        }
        return null;
    }
}
