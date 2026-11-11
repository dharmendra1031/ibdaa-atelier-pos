using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class OrderLineBuilder(AtelierDbContext db)
{
    public sealed record PreparedLine(OrderItem Item, List<OrderItemMeasurement> Measurements);

    public async Task<List<PreparedLine>> BuildAsync(List<OrderLineInput> inputs, CancellationToken ct)
    {
        if (inputs.Count is < 1 or > 30 || inputs.Any(x => x is null || x.ItemId <= 0 || x.Quantity is < 1 or > 100 || x.Measurements is null))
            throw new ArgumentException("An order must contain 1–30 valid lines.");

        var productIds = inputs.Where(x => x.ItemType == "Product").Select(x => x.ItemId).Distinct().ToArray();
        var serviceIds = inputs.Where(x => x.ItemType == "Service").Select(x => x.ItemId).Distinct().ToArray();
        if (productIds.Length + serviceIds.Length == 0 ||
            inputs.Any(x => x.ItemType is not ("Product" or "Service")))
            throw new ArgumentException("Only Product and Service order lines are allowed.");

        var products = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId, ct);
        var services = await db.AlterationServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.ServiceId)).ToDictionaryAsync(s => s.ServiceId, ct);
        var categoryIds = products.Values.Select(p => p.CategoryId).Distinct().ToArray();
        var serviceCategoryIds = services.Values.Select(s => s.ServiceCategoryId).Distinct().ToArray();
        var categories = await db.ProductCategories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId, ct);
        var parentIds = categories.Values.Where(c => c.ParentCategoryId.HasValue)
            .Select(c => c.ParentCategoryId!.Value).Distinct().ToArray();
        var parents = await db.ProductCategories.AsNoTracking()
            .Where(c => parentIds.Contains(c.CategoryId)).ToListAsync(ct);
        foreach (var parent in parents)
            categories[parent.CategoryId] = parent;
        var activeServiceCategories = await db.ServiceCategories.AsNoTracking()
            .Where(c => serviceCategoryIds.Contains(c.ServiceCategoryId) && c.IsActive)
            .Select(c => c.ServiceCategoryId).ToListAsync(ct);
        var sizes = await db.ProductSizes.AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId) && s.IsActive).ToListAsync(ct);
        var definitions = await db.MeasurementDefinitions.AsNoTracking()
            .Where(m => m.IsActive && (productIds.Contains(m.ProductId ?? 0) ||
                categoryIds.Contains(m.CategoryId ?? 0))).ToListAsync(ct);

        var prepared = new List<PreparedLine>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input.Notes?.Length > 1000 || input.Measurements.Count > 50)
                throw new ArgumentException("Line notes or measurement count exceed the permitted limit.");

            if (input.ItemType == "Service")
            {
                if (!services.TryGetValue(input.ItemId, out var service) || !service.IsActive ||
                    !activeServiceCategories.Contains(service.ServiceCategoryId) ||
                    input.SizeId.HasValue || input.Measurements.Count > 0)
                    throw new ArgumentException("Invalid or inactive alteration service.");

                prepared.Add(new PreparedLine(new OrderItem
                {
                    ItemType = "Service",
                    ServiceId = service.ServiceId,
                    Name = service.Name,
                    NameAr = service.NameAr,
                    Notes = input.Notes?.Trim(),
                    Quantity = input.Quantity,
                    UnitPrice = service.Price,
                    LineTotal = checked(service.Price * input.Quantity)
                }, []));
                continue;
            }

            if (!products.TryGetValue(input.ItemId, out var product) || !product.IsActive ||
                !categories.TryGetValue(product.CategoryId, out var category) || !category.IsActive ||
                (category.ParentCategoryId is int parentId &&
                    (!categories.TryGetValue(parentId, out var parent) || !parent.IsActive)))
                throw new ArgumentException("Invalid or inactive product.");

            var validSizes = sizes.Where(s => s.ProductId == product.ProductId).ToList();
            var chosenSize = validSizes.SingleOrDefault(s => s.SizeId == input.SizeId);
            if ((validSizes.Count > 0 && chosenSize is null) ||
                (input.SizeId.HasValue && chosenSize is null))
                throw new ArgumentException("Select a size belonging to this product.");

            var applicable = definitions.Where(m => m.ProductId == product.ProductId ||
                m.CategoryId == product.CategoryId).ToDictionary(m => m.MeasurementId);
            var submitted = input.Measurements ?? [];
            if (submitted.Select(m => m.MeasurementId).Distinct().Count() != submitted.Count ||
                applicable.Values.Any(m => m.IsRequired && !submitted.Any(v => v.MeasurementId == m.MeasurementId)))
                throw new ArgumentException("Required or duplicate measurements are invalid.");

            var snapshots = new List<OrderItemMeasurement>(submitted.Count);
            foreach (var measure in submitted)
            {
                if (!applicable.TryGetValue(measure.MeasurementId, out var definition) ||
                    measure.Value < 0 || measure.Value > 9999999.99m ||
                    decimal.Round(measure.Value, 2) != measure.Value)
                    throw new ArgumentException("Measurement is not applicable or exceeds its permitted precision.");

                snapshots.Add(new OrderItemMeasurement
                {
                    MeasurementId = definition.MeasurementId,
                    Name = definition.Name,
                    NameAr = definition.NameAr ?? "",
                    Unit = definition.Unit,
                    Value = measure.Value
                });
            }

            var unitPrice = product.DiscountPrice ?? product.RegularPrice;
            prepared.Add(new PreparedLine(new OrderItem
            {
                ItemType = "Product",
                ProductId = product.ProductId,
                SizeId = chosenSize?.SizeId,
                ProductCode = product.ProductCode,
                Name = product.Name,
                NameAr = product.NameAr ?? "",
                SizeName = chosenSize?.Name,
                Notes = input.Notes?.Trim(),
                Quantity = input.Quantity,
                UnitPrice = unitPrice,
                LineTotal = checked(unitPrice * input.Quantity)
            }, snapshots));
        }

        return prepared;
    }
}
