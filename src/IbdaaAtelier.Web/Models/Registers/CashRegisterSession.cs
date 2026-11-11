using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Registers;

[Table("CashRegisterSessions", Schema = "dbo")]
public sealed class CashRegisterSession
{
    [Key] public long SessionId { get; set; }
    public int OpenedByUserId { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal OpeningCash { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public int? ClosedByUserId { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? CountedCash { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? ExpectedCash { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? Difference { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}

[Table("CashExpenseRequests", Schema = "dbo")]
public sealed class CashExpenseRequest
{
    [Key] public long RequestId { get; set; }
    public long SessionId { get; set; }
    public int UserId { get; set; }
    [MaxLength(64)] public string? Fingerprint { get; set; }
}

[Table("CashExpenses", Schema = "dbo")]
public sealed class CashExpense
{
    [Key] public long ExpenseId { get; set; }
    public long RequestId { get; set; }
    public long SessionId { get; set; }
    public int RecordedByUserId { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal Amount { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
