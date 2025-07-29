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

    private List<OrderPlacedData> placedOrders = new List<OrderPlacedData>();

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
        DefineObsProperty("ordersInPreparation", ordersInPreparation.ToList());
        DefineObsProperty("ordersReady", readyOrdersWithFood.Keys.ToList());
        DefineObsProperty("placedOrders", new List<OrderPlacedData>(placedOrders));
        UpdateUI();
        UpdateFoodVisuals();
    }


    public override void Use(int agentId, params object[] args)
    {
        base.Use(agentId, args);

        // If args contains an orderId, pick up that order
        if (args != null && args.Length > 0 && args[0] is int orderId)
            PickUpOrder(agentId, orderId);
        else
            Debug.LogWarning($"[{ArtifactName}] Use not supported without orderId argument");
        

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

    // Specific methods for MonitorArtifact

    // HandleSignal(string signal, object data): Processes signals from connected totems
    private void HandleSignal(string signal, object data)
    {
        switch (signal)
        {
            case "orderPlaced":
                OrderPlacedData orderData = data as OrderPlacedData;
                if (orderData != null)
                {
                    AddOrderToPreparation(orderData.orderId, orderData.agentId);
                    Debug.Log($"[{ArtifactName}] Order #{orderData.orderId} from Agent {orderData.agentId} in preparation");
                }
                break;

            case "orderReady":
                int readyOrderId = (int)data;
                MoveOrderToReady(readyOrderId);
                Debug.Log($"[{ArtifactName}] Order #{readyOrderId} ready");
                break;
        }
    }

    // AddOrderToPreparation(int orderId): Adds an order to the preparation list
    private void AddOrderToPreparation(int orderId, int agentId)
    {
        if (!ordersInPreparation.Contains(orderId))
        {
            ordersInPreparation.Add(orderId);
            placedOrders.Add(new OrderPlacedData(orderId, agentId));
            UpdateObsProperty("placedOrders", new List<OrderPlacedData>(placedOrders));
            UpdateObsProperty("ordersInPreparation", ordersInPreparation.ToList());
            UpdateUI();
        }
    }

    // MoveOrderToReady(int orderId): Moves an order from preparation to ready state
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
            UpdateObsProperty("ordersReady", readyOrdersWithFood.Keys.ToList());
            UpdateObsProperty("ordersInPreparation", ordersInPreparation.ToList());
            UpdateUI();
            UpdateFoodVisuals();
        }
    }

    // GetNextFoodType(): Determines the next food type based on current ready orders
    private FoodType GetNextFoodType()
    {
        // Alternate between food types
        int readyCount = readyOrdersWithFood.Count;
        return readyCount % 2 == 0 ? FoodType.Hamburger : FoodType.Hotdog;
    }

    // UpdateFoodVisuals(): Updates the visibility of food objects based on ready orders
    private void UpdateFoodVisuals()
    {
        var foodCounts = readyOrdersWithFood.Values
            .GroupBy(food => food)
            .ToDictionary(g => g.Key, g => g.Count());

        bool showHamburger = foodCounts.ContainsKey(FoodType.Hamburger) && foodCounts[FoodType.Hamburger] > 0;
        bool showHotdog = foodCounts.ContainsKey(FoodType.Hotdog) && foodCounts[FoodType.Hotdog] > 0;

        if (hamburgerObject != null)
            hamburgerObject.SetActive(showHamburger);

        if (hotdogObject != null)
            hotdogObject.SetActive(showHotdog);
    }

    // UpdateUI(): Updates the UI text elements for preparation and ready orders
    private void UpdateUI()
    {
        if (listPrepOrders != null)
        {
            string prepText = "";
            if (ordersInPreparation.Count > 0)
                prepText += string.Join(", ", ordersInPreparation.Select(id => $"Order #{id}"));
            else
                prepText += "No Preparation Orders";
            listPrepOrders.text = prepText;
        }

        if (listReadyOrders != null)
        {
            string readyText = "";
            if (readyOrdersWithFood.Count > 0)
            {
                var readyDescriptions = readyOrdersWithFood.Select(kvp =>
                    $"Order #{kvp.Key}");
                readyText += string.Join(", ", readyDescriptions);
            }
            else
                readyText += "No Ready Orders";
            listReadyOrders.text = readyText;
        }
    }

    // PickUpOrder(int agentId, int orderId): Called by Use method to pick up an order
    public bool PickUpOrder(int agentId, int orderId)
    {
        if (readyOrdersWithFood.ContainsKey(orderId))
        {
            FoodType foodType = readyOrdersWithFood[orderId];

            RemoveOrderFromReady(orderId);

            EmitSignal("orderPickedUp", new OrderPickedUpData(orderId, FindTotemNameForOrder(orderId)));

            Debug.Log($"[{ArtifactName}] {foodType} order #{orderId} retired by Agent {agentId}");
            return true;
        }

        Debug.Log($"[{ArtifactName}] Order #{orderId} not found in ready orders");
        return false;
    }


    // RemoveOrderFromReady(int orderId): Removes an order from the ready orders
    private void RemoveOrderFromReady(int orderId)
    {
        if (readyOrdersWithFood.ContainsKey(orderId))
        {
            readyOrdersWithFood.Remove(orderId);
            UpdateObsProperty("ordersReady", readyOrdersWithFood.Keys.ToList());
            UpdateUI();
            UpdateFoodVisuals();
        }
    }

    // FindTotemNameForOrder(int orderId): Finds the totem name associated with an order
    private string FindTotemNameForOrder(int orderId)
    {
        foreach (var totem in connectedTotems)
        {
            if (totem.HasOrder(orderId))
                return totem.ArtifactName;
        }
        return null;
    }
    
    // OnDestroy(): Clean up connections when the monitor is destroyed
    private void OnDestroy()
    {
        // Clean up connections when monitor is destroyed
        foreach (TotemArtifact totem in connectedTotems)
        {
            totem.OnSignal -= HandleSignal;
        }
        connectedTotems.Clear();
    }



}
