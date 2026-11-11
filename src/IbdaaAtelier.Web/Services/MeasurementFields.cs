using IbdaaAtelier.Web.Models.Catalog;

namespace IbdaaAtelier.Web.Services;

public static class MeasurementFields
{
    public static List<MeasurementDefinition> Resolve(
        IEnumerable<MeasurementDefinition> definitions, int productId, int categoryId)
    {
        // A product-level definition replaces a same-named category field.
        return definitions
            .Where(field => field.IsActive &&
                (field.ProductId == productId || field.CategoryId == categoryId))
            .OrderByDescending(field => field.ProductId == productId)
            .ThenBy(field => field.DisplayOrder)
            .ThenBy(field => field.MeasurementId)
            .GroupBy(field => field.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.MeasurementId)
            .ToList();
    }
}
