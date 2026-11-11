using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Masters;

[Table("AlterationServices", Schema = "dbo")]
public sealed class AlterationService
{
    [Key]
    public int ServiceId { get; set; }
    public int ServiceCategoryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, MaxLength(120)]
    public string NameAr { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? DescriptionAr { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Price { get; set; }

    public int? EstimatedMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
