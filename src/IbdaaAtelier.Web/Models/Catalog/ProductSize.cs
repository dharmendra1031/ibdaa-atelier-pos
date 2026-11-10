using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("ProductSizes", Schema = "dbo")]
public sealed class ProductSize
{
    [Key]
    public int SizeId { get; set; }
    public int ProductId { get; set; }
    [Required, MaxLength(30)]
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
