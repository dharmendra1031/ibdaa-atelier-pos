using IbdaaAtelier.Web.Models.Payments;

namespace IbdaaAtelier.Web.Models.Orders;

public sealed class OrderDetailsPage
{
    public Order Order { get; init; } = new();
    public List<OrderItem> Items { get; init; } = [];
    public List<OrderItemMeasurement> Measurements { get; init; } = [];
    public List<OrderPayment> Payments { get; init; } = [];
    public List<OrderStatusChange> StatusHistory { get; init; } = [];
    public string[] NextStatuses { get; init; } = [];
    public List<OrderRefund> Refunds { get; init; } = [];
    public decimal GrossPaid { get; init; }
    public decimal Refunded { get; init; }
    public decimal Paid { get; init; }
    public decimal Balance { get; init; }
}

public sealed class OrderListPage
{
    public List<Order> Orders { get; init; } = [];
    public string Search { get; init; } = "";
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
}
