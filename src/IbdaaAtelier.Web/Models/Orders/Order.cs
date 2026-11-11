using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Orders;

[Table("Orders", Schema = "dbo")]
public sealed class Order
{
    [Key] public long OrderId { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [MaxLength(24)] public string OrderNumber { get; private set; } = "";
    public long RequestId { get; set; }
    public long CustomerId { get; set; }
    public int CreatedByUserId { get; set; }
    [MaxLength(120)] public string CustomerName { get; set; } = "";
    [MaxLength(120)] public string CustomerNameAr { get; set; } = "";
    [MaxLength(20)] public string CustomerMobile { get; set; } = "";
    public DateOnly DeliveryDate { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "New";
    [Column(TypeName = "decimal(18,3)")] public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("OrderRequests", Schema = "dbo")]
public sealed class OrderRequest
{
    [Key] public long RequestId { get; set; }
    public int UserId { get; set; }
    [MaxLength(64)] public string? Fingerprint { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
