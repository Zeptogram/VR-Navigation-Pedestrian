// Class to hold data when an order is placed
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