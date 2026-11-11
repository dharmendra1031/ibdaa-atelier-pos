namespace IbdaaAtelier.Web.Models.Masters;

public sealed class AdminDashboardPage
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Period { get; init; } = "today";
    public int OrderCount { get; init; }
    public int CustomerCount { get; init; }
    public int PendingDeliveries { get; init; }
    public int ReadyForDelivery { get; init; }
    public decimal Sales { get; init; }
    public decimal Collected { get; init; }
    public decimal Refunded { get; init; }
    public decimal Outstanding { get; init; }
    public List<PaymentMethodTotal> PaymentMethods { get; init; } = [];
    public List<AdminRecentOrder> RecentOrders { get; init; } = [];
}

public sealed record PaymentMethodTotal(string Method, decimal Received, decimal Refunded)
{
    public decimal Net => Received - Refunded;
}

public sealed record AdminRecentOrder(long OrderId, string OrderNumber, string CustomerName,
    decimal Total, string Status, DateOnly DeliveryDate);
