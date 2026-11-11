namespace IbdaaAtelier.Web.Models.Catalog;

public sealed class ProductGalleryPage
{
    public Product Product { get; init; } = new();
    public List<ProductImage> Images { get; init; } = [];
}
