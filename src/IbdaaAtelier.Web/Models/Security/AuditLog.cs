using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Security;

[Table("AuditLogs", Schema = "dbo")]
public sealed class AuditLog
{
    [Key]
    public long AuditId { get; set; }
    public int? UserId { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = "";

    [Required, MaxLength(60)]
    public string EntityName { get; set; } = "";

    public long? EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
