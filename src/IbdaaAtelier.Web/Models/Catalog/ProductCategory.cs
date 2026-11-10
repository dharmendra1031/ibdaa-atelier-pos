using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("ProductCategories", Schema = "dbo")]
public sealed class ProductCategory
{
    [Key]
    public int CategoryId { get; set; }
    public int? ParentCategoryId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    [MaxLength(100)]
    public string? NameAr { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    [MaxLength(1000)]
    public string? DescriptionAr { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
