using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Masters;

[Table("Customers", Schema = "dbo")]
public sealed class Customer
{
    [Key]
    public long CustomerId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, MaxLength(120)]
    public string NameAr { get; set; } = "";

    [Required, MaxLength(20)]
    public string Mobile { get; set; } = "";

    [MaxLength(20)]
    public string? AlternateMobile { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}
