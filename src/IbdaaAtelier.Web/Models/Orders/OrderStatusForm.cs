using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Orders;

public sealed class OrderStatusForm
{
    [Range(1, long.MaxValue)] public long OrderId { get; set; }
    [Required] public string Status { get; set; } = "";
    [StringLength(500)] public string? Remarks { get; set; }
}
