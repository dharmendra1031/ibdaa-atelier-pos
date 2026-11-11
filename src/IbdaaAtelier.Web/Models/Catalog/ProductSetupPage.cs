namespace IbdaaAtelier.Web.Models.Catalog;

public sealed class ProductSetupPage
{
    public Product Product { get; init; } = new();
    public List<ProductSize> Sizes { get; init; } = [];
    public List<MeasurementDefinition> Measurements { get; init; } = [];
    public List<SizeChartValue> ChartValues { get; init; } = [];
}

public sealed class CategoryMeasurementPage
{
    public ProductCategory Category { get; init; } = new();
    public List<MeasurementDefinition> Measurements { get; init; } = [];
}
