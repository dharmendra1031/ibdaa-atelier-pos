using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Payments;

public sealed class PaymentForm
{
    [Range(1, long.MaxValue)] public long RequestId { get; set; }
    [Range(1, long.MaxValue)] public long OrderId { get; set; }
    [Required, MinLength(1), MaxLength(5)]
    public List<PaymentLine> Lines { get; set; } = [];
}

public sealed class PaymentLine
{
    [Required] public string Method { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal? Tendered { get; set; }
}

public sealed record PaymentResult(long BatchId, decimal Received, decimal Balance, bool Replayed);

public sealed class PaymentEntryPage
{
    public long OrderId { get; init; }
    public decimal Total { get; init; }
    public decimal Paid { get; init; }
    public decimal Balance { get; init; }
    public decimal Refunded { get; init; }
    public List<OrderPayment> PreviousPayments { get; init; } = [];
    public PaymentForm Form { get; init; } = new();
}
