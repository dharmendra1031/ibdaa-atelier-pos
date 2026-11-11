using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Payments;

[Table("RefundRequests", Schema = "dbo")]
public sealed class RefundRequest
{
    [Key] public long RequestId { get; set; }
    public long OrderId { get; set; }
    public int UserId { get; set; }
    [MaxLength(64)] public string? Fingerprint { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("OrderRefunds", Schema = "dbo")]
public sealed class OrderRefund
{
    [Key] public long RefundId { get; set; }
    public long RequestId { get; set; }
    public long OrderId { get; set; }
    [Required, MaxLength(20)] public string Method { get; set; } = "";
    [Column(TypeName = "decimal(18,3)")] public decimal Amount { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
    public int ApprovedByUserId { get; set; }
    public long? RegisterSessionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
