namespace IbdaaAtelier.Web.Models.Catalog;

public sealed record ProductListRow(
    int ProductId,
    string ProductCode,
    string Name,
    string? NameAr,
    string CategoryName,
    decimal RegularPrice,
    decimal? DiscountPrice,
    bool IsActive);

public sealed class ProductListPage
{
    public List<ProductListRow> Products { get; init; } = [];
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string Search { get; init; } = "";
}
