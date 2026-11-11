using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Masters;

namespace IbdaaAtelier.Web.Models.Orders;

public sealed class PosPage
{
    public List<ProductCategory> Categories { get; init; } = [];
    public List<CatalogProduct> Products { get; init; } = [];
    public List<AlterationService> Services { get; init; } = [];
    public int? CategoryId { get; init; }
    public string Search { get; init; } = "";
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string ServiceSearch { get; init; } = "";
    public int ServicePage { get; init; } = 1;
    public int ServiceTotalPages { get; init; } = 1;
    public bool ShowServices { get; init; }
    public string NextOrderNumber { get; init; } = "";
}
