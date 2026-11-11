using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ProductsController(AtelierDbContext db) : Controller
{
    private const int PageSize = 24;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var query = db.Products.AsNoTracking();
        if (search.Length > 0)
            query = query.Where(p => p.Name.Contains(search) ||
                (p.NameAr != null && p.NameAr.Contains(search)) ||
                p.ProductCode.Contains(search));

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, totalPages);

        var products = await query
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.ProductId)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .ToListAsync(ct);

        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToArray();
        var categories = await db.ProductCategories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId, c => c.Name, ct);

        return View(new ProductListPage
        {
            Search = search,
            Page = page,
            TotalPages = totalPages,
            Products = products.Select(p => new ProductListRow(
                p.ProductId, p.ProductCode, p.Name, p.NameAr,
                categories.GetValueOrDefault(p.CategoryId, "—"),
                p.RegularPrice, p.DiscountPrice, p.IsActive)).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadCategoriesAsync(ct);
        return View("Editor", new ProductForm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductForm form, CancellationToken ct)
    {
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        var product = new Product();
        CopyValues(form, product);
        db.Products.Add(product);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit("Product.Create", product.ProductId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not save the product. Review its details.");
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        TempData["Message"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProductId == id, ct);
        if (product is null)
            return NotFound();

        await LoadCategoriesAsync(ct);
        return View("Editor", new ProductForm
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            Name = product.Name,
            NameAr = product.NameAr ?? "",
            Description = product.Description,
            DescriptionAr = product.DescriptionAr,
            ProductType = product.ProductType,
            RegularPrice = product.RegularPrice,
            DiscountPrice = product.DiscountPrice,
            DisplayOrder = product.DisplayOrder
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductForm form, CancellationToken ct)
    {
        if (form.ProductId <= 0)
            return BadRequest();

        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == form.ProductId, ct);
        if (product is null)
            return NotFound();

        await ValidateAsync(form, ct);

        if (form.CategoryId != product.CategoryId)
        {
            var previousCategoryId = product.CategoryId;
            var oldCategoryValues = await db.SizeChartValues.AnyAsync(value =>
                db.ProductSizes.Any(size =>
                    size.SizeId == value.SizeId && size.ProductId == product.ProductId) &&
                db.MeasurementDefinitions.Any(measurement =>
                    measurement.MeasurementId == value.MeasurementId &&
                    measurement.CategoryId == previousCategoryId), ct);

            if (oldCategoryValues)
                ModelState.AddModelError(nameof(form.CategoryId),
                    "This product has size-chart values from its current category. Remove or migrate those values before changing category.");

            var overlappingFields = await db.MeasurementDefinitions.AnyAsync(measurement =>
                measurement.CategoryId == form.CategoryId && measurement.IsActive &&
                db.MeasurementDefinitions.Any(productMeasurement =>
                    productMeasurement.ProductId == product.ProductId &&
                    productMeasurement.IsActive && productMeasurement.Name == measurement.Name), ct);
            if (overlappingFields)
                ModelState.AddModelError(nameof(form.CategoryId),
                    "The new category contains measurement names already defined on this product.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        CopyValues(form, product);
        AddAudit("Product.Edit", product.ProductId);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not update the product. Review its details.");
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        TempData["Message"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id, ct);
        if (product is null)
            return NotFound();

        if (!product.IsActive &&
            !await db.ProductCategories.AnyAsync(c => c.CategoryId == product.CategoryId && c.IsActive, ct))
        {
            TempData["Error"] = "Activate the product category first.";
            return RedirectToAction(nameof(Index));
        }

        product.IsActive = !product.IsActive;
        AddAudit(product.IsActive ? "Product.Activate" : "Product.Deactivate", product.ProductId);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = product.IsActive ? "Product activated." : "Product deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(ProductForm form, CancellationToken ct)
    {
        form.Name = form.Name?.Trim() ?? "";
        form.NameAr = form.NameAr?.Trim() ?? "";
        form.ProductType = form.ProductType?.Trim() ?? "";
        form.Description = form.Description?.Trim();
        form.DescriptionAr = form.DescriptionAr?.Trim();

        if (form.Name.Length == 0)
            ModelState.AddModelError(nameof(form.Name), "English name is required.");
        if (form.NameAr.Length == 0)
            ModelState.AddModelError(nameof(form.NameAr), "Arabic name is required.");
        if (form.ProductType.Length == 0)
            ModelState.AddModelError(nameof(form.ProductType), "Product type is required.");

        var validCategory = await db.ProductCategories.AsNoTracking()
            .AnyAsync(c => c.CategoryId == form.CategoryId && c.IsActive &&
                (c.ParentCategoryId == null ||
                 db.ProductCategories.Any(parent => parent.CategoryId == c.ParentCategoryId && parent.IsActive)), ct);
        if (!validCategory)
            ModelState.AddModelError(nameof(form.CategoryId), "Choose an active category.");

        if (form.RegularPrice < 0 || form.RegularPrice != decimal.Round(form.RegularPrice, 3))
            ModelState.AddModelError(nameof(form.RegularPrice), "Use a non-negative KWD price with up to three decimals.");

        if (form.DiscountPrice is decimal discount &&
            (discount < 0 || discount > form.RegularPrice || discount != decimal.Round(discount, 3)))
            ModelState.AddModelError(nameof(form.DiscountPrice), "Discount price must be within the regular price and have up to three decimals.");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        ViewBag.Categories = await db.ProductCategories.AsNoTracking()
            .Where(c => c.IsActive && (c.ParentCategoryId == null ||
                db.ProductCategories.Any(parent => parent.CategoryId == c.ParentCategoryId && parent.IsActive)))
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }

    private static void CopyValues(ProductForm form, Product product)
    {
        product.CategoryId = form.CategoryId;
        product.Name = form.Name;
        product.NameAr = form.NameAr;
        product.Description = form.Description;
        product.DescriptionAr = form.DescriptionAr;
        product.ProductType = form.ProductType;
        product.RegularPrice = form.RegularPrice;
        product.DiscountPrice = form.DiscountPrice;
        product.DisplayOrder = form.DisplayOrder;
    }

    private void AddAudit(string action, int productId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = "Product",
            EntityId = productId
        });
    }
}
