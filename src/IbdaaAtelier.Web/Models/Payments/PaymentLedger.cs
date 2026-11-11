using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Payments;

[Table("PaymentRequests", Schema = "dbo")]
public sealed class PaymentRequest
{
    [Key] public long RequestId { get; set; }
    public long OrderId { get; set; }
    public int UserId { get; set; }
    [MaxLength(64)] public string? Fingerprint { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("PaymentBatches", Schema = "dbo")]
public sealed class PaymentBatch
{
    [Key] public long PaymentBatchId { get; set; }
    public long RequestId { get; set; }
    public long OrderId { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("OrderPayments", Schema = "dbo")]
public sealed class OrderPayment
{
    [Key] public long PaymentId { get; set; }
    public long PaymentBatchId { get; set; }
    public long OrderId { get; set; }
    [Required, MaxLength(20)] public string Method { get; set; } = "";
    [Column(TypeName = "decimal(18,3)")] public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal Tendered { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal Change { get; set; }
    public int ReceivedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
