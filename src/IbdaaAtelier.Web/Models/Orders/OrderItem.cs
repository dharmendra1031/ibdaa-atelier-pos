using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Orders;

[Table("OrderItems", Schema = "dbo")]
public sealed class OrderItem
{
    [Key] public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    [Required, MaxLength(10)] public string ItemType { get; set; } = "";
    public int? ProductId { get; set; }
    public int? ServiceId { get; set; }
    public int? SizeId { get; set; }
    [MaxLength(16)] public string ProductCode { get; set; } = "";
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(150)] public string NameAr { get; set; } = "";
    [MaxLength(30)] public string? SizeName { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal LineTotal { get; set; }
}

[Table("OrderItemMeasurements", Schema = "dbo")]
public sealed class OrderItemMeasurement
{
    [Key] public long OrderMeasurementId { get; set; }
    public long OrderItemId { get; set; }
    public int MeasurementId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(100)] public string NameAr { get; set; } = "";
    [Required, MaxLength(20)] public string Unit { get; set; } = "cm";
    [Column(TypeName = "decimal(9,2)")] public decimal Value { get; set; }
}
