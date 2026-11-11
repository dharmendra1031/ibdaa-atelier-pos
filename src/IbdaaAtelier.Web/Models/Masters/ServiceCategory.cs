using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Masters;

[Table("ServiceCategories", Schema = "dbo")]
public sealed class ServiceCategory
{
    [Key]
    public int ServiceCategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
