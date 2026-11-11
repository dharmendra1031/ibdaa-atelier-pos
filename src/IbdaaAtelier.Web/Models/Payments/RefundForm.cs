using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Payments;

public sealed class RefundForm
{
    [Range(1, long.MaxValue)] public long RequestId { get; set; }
    [Range(1, long.MaxValue)] public long OrderId { get; set; }
    [Required] public string Method { get; set; } = "Cash";
    [Range(typeof(decimal), "0.001", "999999999999999.999")] public decimal Amount { get; set; }
    [Required, StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = "";
}

public sealed class RefundEntryPage
{
    public long OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public decimal TotalPaid { get; init; }
    public decimal TotalRefunded { get; init; }
    public Dictionary<string, decimal> AvailableByMethod { get; init; } = [];
    public RefundForm Form { get; init; } = new();
}

public sealed record RefundResult(long RefundId, decimal Refunded, bool Replayed);
