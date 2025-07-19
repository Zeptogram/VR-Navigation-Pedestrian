public class OrderPickedUpData
{
    public int orderId;
    public string totemName; // o un altro identificatore univoco

    public OrderPickedUpData(int orderId, string totemName)
    {
        this.orderId = orderId;
        this.totemName = totemName;
    }
}