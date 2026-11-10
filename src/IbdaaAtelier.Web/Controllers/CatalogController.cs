using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

public sealed class CatalogController(AtelierDbContext db, IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, string? search, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("AtelierDb")))
            return View("NotConfigured");

        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var categories = await db.ProductCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(ct);

        var products = db.Products.AsNoTracking()
            .Where(product => product.IsActive &&
                db.ProductCategories.Any(category =>
                    category.CategoryId == product.CategoryId && category.IsActive));

        if (categoryId is > 0)
            products = products.Where(product => product.CategoryId == categoryId);

        if (search.Length > 0)
            products = products.Where(product =>
                product.Name.Contains(search) ||
                (product.NameAr != null && product.NameAr.Contains(search)) ||
                product.ProductCode.Contains(search));

        var items = await products
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Take(100)
            .Select(product => new CatalogProduct(
                product.ProductId,
                product.ProductCode,
                product.Name,
                product.NameAr,
                product.DiscountPrice ?? product.RegularPrice,
                product.DiscountPrice.HasValue ? product.RegularPrice : null,
                db.ProductImages
                    .Where(image => image.ProductId == product.ProductId)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return View(new CatalogPage
        {
            Categories = categories,
            Products = items,
            SelectedCategoryId = categoryId,
            Search = search
        });
    }
}
