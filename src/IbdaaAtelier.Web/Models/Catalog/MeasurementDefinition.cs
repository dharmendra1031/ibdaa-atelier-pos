using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("MeasurementDefinitions", Schema = "dbo")]
public sealed class MeasurementDefinition
{
    [Key]
    public int MeasurementId { get; set; }
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    [MaxLength(100)]
    public string? NameAr { get; set; }
    [Required, MaxLength(20)]
    public string Unit { get; set; } = "cm";
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
