using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Catalog;

public sealed class CategoryForm
{
    public int CategoryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, StringLength(100)]
    public string NameAr { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(1000)]
    public string? DescriptionAr { get; set; }

    public int? ParentCategoryId { get; set; }

    [Range(0, 100000)]
    public int DisplayOrder { get; set; }
}
