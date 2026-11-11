using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ProductSetupController(AtelierDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int productId, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product is null)
            return NotFound();

        var sizes = await db.ProductSizes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.SizeId).ToListAsync(ct);
        var measurements = await db.MeasurementDefinitions.AsNoTracking()
            .Where(x => x.ProductId == productId || x.CategoryId == product.CategoryId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.MeasurementId).ToListAsync(ct);

        var sizeIds = sizes.Select(x => x.SizeId).ToList();
        var chart = await db.SizeChartValues.AsNoTracking()
            .Where(x => sizeIds.Contains(x.SizeId))
            .ToListAsync(ct);

        return View(new ProductSetupPage
        {
            Product = product,
            Sizes = sizes,
            Measurements = measurements,
            ChartValues = chart
        });
    }

    [HttpGet]
    public async Task<IActionResult> Category(int categoryId, CancellationToken ct)
    {
        var category = await db.ProductCategories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null)
            return NotFound();

        var measurements = await db.MeasurementDefinitions.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.MeasurementId).ToListAsync(ct);

        return View(new CategoryMeasurementPage { Category = category, Measurements = measurements });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSize(int productId, string? name, int displayOrder, CancellationToken ct)
    {
        var exists = await db.Products.AnyAsync(p => p.ProductId == productId, ct);
        if (!exists)
            return NotFound();

        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 30 || displayOrder < 0 ||
            await db.ProductSizes.AnyAsync(x => x.ProductId == productId && x.Name == name, ct))
        {
            TempData["Error"] = "Enter a unique size name (up to 30 characters) and valid order.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        db.ProductSizes.Add(new ProductSize { ProductId = productId, Name = name, DisplayOrder = displayOrder });
        AddAudit("ProductSize.Create", "Product", productId);
        try
        {
            await db.SaveChangesAsync(ct);
            TempData["Message"] = "Size added.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Size could not be saved. Check for duplicates.";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSize(int productId, int sizeId, CancellationToken ct)
    {
        var size = await db.ProductSizes.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.SizeId == sizeId, ct);
        if (size is null)
            return NotFound();

        size.IsActive = !size.IsActive;
        AddAudit(size.IsActive ? "ProductSize.Activate" : "ProductSize.Deactivate", "Product", productId);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Size status updated.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMeasurement(
        int? productId, int? categoryId, string? name, string? nameAr,
        string? unit, bool isRequired, int displayOrder, CancellationToken ct)
    {
        var toProduct = productId is > 0 && categoryId is null;
        var toCategory = categoryId is > 0 && productId is null;
        if (!toProduct && !toCategory)
            return BadRequest();

        var targetId = toProduct ? productId!.Value : categoryId!.Value;
        var redirect = toProduct ? nameof(Index) : nameof(Category);
        object route = toProduct ? new { productId = targetId } : new { categoryId = targetId };
        var targetExists = toProduct ?
            await db.Products.AnyAsync(p => p.ProductId == targetId, ct) :
            await db.ProductCategories.AnyAsync(c => c.CategoryId == targetId, ct);
        if (!targetExists)
            return NotFound();

        name = name?.Trim();
        nameAr = nameAr?.Trim();
        unit = unit?.Trim().ToLowerInvariant();

        var productCategoryId = toProduct
            ? await db.Products.Where(p => p.ProductId == productId)
                .Select(p => (int?)p.CategoryId).SingleAsync(ct)
            : categoryId;

        var duplicate = await db.MeasurementDefinitions.AnyAsync(x =>
            x.Name == name &&
            ((x.ProductId == productId && x.CategoryId == categoryId) ||
             (x.IsActive && (
                (toProduct && x.CategoryId == productCategoryId) ||
                (toCategory && x.ProductId.HasValue &&
                    db.Products.Any(p => p.ProductId == x.ProductId &&
                        p.CategoryId == categoryId))))), ct);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
            string.IsNullOrWhiteSpace(nameAr) || nameAr.Length > 100 ||
            unit is not ("cm" or "in") || displayOrder < 0 || duplicate)
        {
            TempData["Error"] = "Provide unique bilingual labels, cm/in unit and valid display order.";
            return RedirectToAction(redirect, route);
        }

        db.MeasurementDefinitions.Add(new MeasurementDefinition
        {
            ProductId = productId,
            CategoryId = categoryId,
            Name = name,
            NameAr = nameAr,
            Unit = unit,
            IsRequired = isRequired,
            DisplayOrder = displayOrder
        });
        AddAudit("Measurement.Create", toProduct ? "Product" : "ProductCategory", targetId);

        try
        {
            await db.SaveChangesAsync(ct);
            TempData["Message"] = "Measurement definition added.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Measurement definition could not be saved.";
        }
        return RedirectToAction(redirect, route);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMeasurement(
        int measurementId, int? productId, int? categoryId, CancellationToken ct)
    {
        var measurement = await db.MeasurementDefinitions.SingleOrDefaultAsync(
            x => x.MeasurementId == measurementId, ct);
        if (measurement is null)
            return NotFound();

        var correctProduct = productId is > 0 && categoryId is null && measurement.ProductId == productId;
        var correctCategory = categoryId is > 0 && productId is null && measurement.CategoryId == categoryId;
        if (!correctProduct && !correctCategory)
            return BadRequest();

        measurement.IsActive = !measurement.IsActive;
        AddAudit(measurement.IsActive ? "Measurement.Activate" : "Measurement.Deactivate",
            correctProduct ? "Product" : "ProductCategory", (correctProduct ? productId : categoryId)!.Value);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Measurement status updated.";
        return RedirectToAction(correctProduct ? nameof(Index) : nameof(Category),
            correctProduct ? new { productId } : new { categoryId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveChart(
        int productId, int sizeId, int measurementId, decimal value, CancellationToken ct)
    {
        var sizeValid = await db.ProductSizes.AnyAsync(s =>
            s.SizeId == sizeId && s.ProductId == productId && s.IsActive, ct);
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product is null)
            return NotFound();

        var measurementValid = await db.MeasurementDefinitions.AnyAsync(m =>
            m.MeasurementId == measurementId && m.IsActive &&
            (m.ProductId == productId || m.CategoryId == product.CategoryId), ct);
        if (!sizeValid || !measurementValid || value < 0 || value > 9999999.99m ||
            value != decimal.Round(value, 2))
        {
            TempData["Error"] = "Select an active size and applicable measurement with a valid two-decimal value.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var entry = await db.SizeChartValues.SingleOrDefaultAsync(
            x => x.SizeId == sizeId && x.MeasurementId == measurementId, ct);
        if (entry is null)
            db.SizeChartValues.Add(new SizeChartValue
            {
                SizeId = sizeId,
                MeasurementId = measurementId,
                Value = value
            });
        else
            entry.Value = value;

        AddAudit("SizeChart.Save", "Product", productId);
        try
        {
            await db.SaveChangesAsync(ct);
            TempData["Message"] = "Size chart saved.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Size chart could not be saved. Please retry.";
        }
        return RedirectToAction(nameof(Index), new { productId });
    }

    private void AddAudit(string action, string entityName, int entityId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = entityName,
            EntityId = entityId
        });
    }
}
