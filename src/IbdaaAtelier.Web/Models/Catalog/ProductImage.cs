using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("ProductImages", Schema = "dbo")]
public sealed class ProductImage
{
    [Key]
    public int ProductImageId { get; set; }
    public int ProductId { get; set; }
    [Required, MaxLength(260)]
    public string ImagePath { get; set; } = "";
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
