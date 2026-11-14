using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Printing;

[Table("PrintTerminals", Schema = "dbo")]
public sealed class PrintTerminal
{
    [Key] public int TerminalId { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    [Required, MaxLength(64)] public string TokenHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("PrintJobs", Schema = "dbo")]
public sealed class PrintJob
{
    [Key] public long PrintJobId { get; set; }
    public int TerminalId { get; set; }
    public long OrderId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    [MaxLength(300)] public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}
