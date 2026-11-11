using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Orders;

[Table("OrderStatusHistory", Schema = "dbo")]
public sealed class OrderStatusChange
{
    [Key] public long HistoryId { get; set; }
    public long OrderId { get; set; }
    public int UserId { get; set; }
    [MaxLength(30)] public string? OldStatus { get; set; }
    [Required, MaxLength(30)] public string NewStatus { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
