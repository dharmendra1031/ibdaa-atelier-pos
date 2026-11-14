using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Registers;

public sealed class CashExpenseForm
{
    [Range(1, long.MaxValue)] public long RequestId { get; set; }
    [Range(1, long.MaxValue)] public long SessionId { get; set; }
    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Amount { get; set; }
    [Required, StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = "";
}

public sealed record RegisterTotals(decimal Received, decimal Refunded, decimal Expenses)
{
    public decimal NetCash => Received - Refunded - Expenses;
}

public sealed record RegisterCloseResult(long SessionId, decimal Expected, decimal Difference, bool Replayed);

public sealed class RegisterPage
{
    public CashRegisterSession? OpenSession { get; init; }
    public List<CashRegisterSession> RecentSessions { get; init; } = [];
    public RegisterTotals Cash { get; init; } = new(0m, 0m, 0m);
    public decimal ReceivedOther { get; init; }
    public decimal RefundedOther { get; init; }
    public bool OpeningRequired { get; init; }
}
