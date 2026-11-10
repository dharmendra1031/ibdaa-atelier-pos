namespace IbdaaAtelier.Web.Models.Catalog;

public sealed record CatalogProduct(
    int ProductId,
    string ProductCode,
    string Name,
    string? NameAr,
    decimal Price,
    decimal? RegularPrice,
    string? ImagePath);

public sealed class CatalogPage
{
    public List<ProductCategory> Categories { get; init; } = [];
    public List<CatalogProduct> Products { get; init; } = [];
    public int? SelectedCategoryId { get; init; }
    public string Search { get; init; } = "";
}
