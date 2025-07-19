// OrderPlacedData.cs
// Data structure for order placement in the fast food system
public class OrderPlacedData
{
    public int orderId;
    public int agentId;

    public OrderPlacedData(int orderId, int agentId)
    {
        this.orderId = orderId;
        this.agentId = agentId;
    }
}