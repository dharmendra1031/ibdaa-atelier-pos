using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Orders;

public sealed class OrderSaveForm
{
    [Range(1, long.MaxValue)] public long RequestId { get; set; }
    [Range(1, long.MaxValue)] public long CustomerId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required, MinLength(1), MaxLength(30)]
    public List<OrderLineInput> Items { get; set; } = [];
}

public sealed class OrderLineInput
{
    [Required] public string ItemType { get; set; } = "";
    [Range(1, int.MaxValue)] public int ItemId { get; set; }
    public int? SizeId { get; set; }
    [Range(1, 100)] public int Quantity { get; set; } = 1;
    [MaxLength(1000)] public string? Notes { get; set; }
    public List<OrderMeasurementInput> Measurements { get; set; } = [];
}

public sealed class OrderMeasurementInput
{
    [Range(1, int.MaxValue)] public int MeasurementId { get; set; }
    public decimal Value { get; set; }
}

public sealed record SavedOrder(long OrderId, string OrderNumber, decimal Total, bool Replayed);
