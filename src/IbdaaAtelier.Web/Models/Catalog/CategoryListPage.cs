namespace IbdaaAtelier.Web.Models.Catalog;

public sealed record CategoryListRow(
    int CategoryId,
    string Name,
    string? NameAr,
    string ParentName,
    bool IsActive,
    int DisplayOrder);

public sealed class CategoryListPage
{
    public List<CategoryListRow> Categories { get; init; } = [];
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string Search { get; init; } = "";
}
