using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Catalog;

public sealed class ProductForm
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = "";

    [Required, StringLength(150)]
    public string NameAr { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? DescriptionAr { get; set; }

    [Required, StringLength(60)]
    public string ProductType { get; set; } = "Ready-made";

    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal RegularPrice { get; set; }

    public decimal? DiscountPrice { get; set; }

    [Range(0, 100000)]
    public int DisplayOrder { get; set; }
}
