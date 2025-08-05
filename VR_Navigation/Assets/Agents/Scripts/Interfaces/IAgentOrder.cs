public interface IAgentOrder
{
    int? MyOrderId { get; }
    bool IsMyOrderReady { get; }
}