// OrderPickedUpData.cs
public class OrderPickedUpData
{
    public int orderId;
    public string totemName;

    public OrderPickedUpData(int orderId, string totemName)
    {
        this.orderId = orderId;
        this.totemName = totemName;
    }
}