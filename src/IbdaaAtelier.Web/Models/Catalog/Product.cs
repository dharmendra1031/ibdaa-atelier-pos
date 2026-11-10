using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("Products", Schema = "dbo")]
public sealed class Product
{
    [Key]
    public int ProductId { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [MaxLength(16)]
    public string ProductCode { get; private set; } = "";
    public int CategoryId { get; set; }
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";
    [MaxLength(150)]
    public string? NameAr { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    [MaxLength(2000)]
    public string? DescriptionAr { get; set; }
    [Required, MaxLength(60)]
    public string ProductType { get; set; } = "Ready-made";
    [Column(TypeName = "decimal(18,3)")]
    public decimal RegularPrice { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal? DiscountPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
