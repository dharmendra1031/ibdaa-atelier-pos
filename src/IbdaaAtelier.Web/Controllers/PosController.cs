using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Cashier")]
public sealed class PosController(AtelierDbContext db, PosOrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1,
        string? serviceSearch = null, int? serviceCategoryId = null, int servicePage = 1,
        bool services = false, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100) search = search[..100];
        serviceSearch = serviceSearch?.Trim() ?? "";
        if (serviceSearch.Length > 100) serviceSearch = serviceSearch[..100];

        var query = db.Products.AsNoTracking().Where(p => p.IsActive &&
            db.ProductCategories.Any(c => c.CategoryId == p.CategoryId && c.IsActive &&
                (c.ParentCategoryId == null || db.ProductCategories.Any(parent =>
                    parent.CategoryId == c.ParentCategoryId && parent.IsActive))));
        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);
        if (search.Length > 0)
            query = query.Where(p => p.Name.Contains(search) ||
                (p.NameAr != null && p.NameAr.Contains(search)) || p.ProductCode.Contains(search));

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + 35) / 36);
        page = Math.Clamp(page, 1, totalPages);

        var serviceQuery = db.AlterationServices.AsNoTracking()
            .Where(s => s.IsActive && db.ServiceCategories.Any(c =>
                c.ServiceCategoryId == s.ServiceCategoryId && c.IsActive));
        if (serviceCategoryId is > 0)
            serviceQuery = serviceQuery.Where(s => s.ServiceCategoryId == serviceCategoryId);
        if (serviceSearch.Length > 0)
            serviceQuery = serviceQuery.Where(s => s.Name.Contains(serviceSearch) ||
                s.NameAr.Contains(serviceSearch));

        var serviceCount = await serviceQuery.CountAsync(ct);
        var serviceTotalPages = Math.Max(1, (serviceCount + 23) / 24);
        servicePage = Math.Clamp(servicePage, 1, serviceTotalPages);

        var products = await query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.ProductId)
            .Skip((page - 1) * 36).Take(36)
            .Select(p => new CatalogProduct(p.ProductId, p.ProductCode, p.Name, p.NameAr,
                p.DiscountPrice ?? p.RegularPrice, p.DiscountPrice.HasValue ? p.RegularPrice : null,
                db.ProductImages.Where(i => i.ProductId == p.ProductId)
                    .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath).FirstOrDefault()))
            .ToListAsync(ct);

        return View(new PosPage
        {
            Products = products,
            Categories = await db.ProductCategories.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct),
            Services = await serviceQuery.OrderBy(s => s.Name).ThenBy(s => s.ServiceId)
                .Skip((servicePage - 1) * 24).Take(24).ToListAsync(ct),
            ServiceCategories = await db.ServiceCategories.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.ServiceCategoryId).ToListAsync(ct),
            ServiceSearch = serviceSearch,
            ServiceCategoryId = serviceCategoryId,
            ServicePage = servicePage,
            ServiceTotalPages = serviceTotalPages,
            ShowServices = services,
            CategoryId = categoryId,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            NextOrderNumber = "IA-" + ((await db.Orders.CountAsync(ct)) + 1)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Product(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProductId == id && p.IsActive, ct);
        if (product is null || !await db.ProductCategories.AnyAsync(c =>
            c.CategoryId == product.CategoryId && c.IsActive &&
            (c.ParentCategoryId == null || db.ProductCategories.Any(parent =>
                parent.CategoryId == c.ParentCategoryId && parent.IsActive)), ct))
            return NotFound();

        var sizes = await db.ProductSizes.AsNoTracking()
            .Where(s => s.ProductId == id && s.IsActive)
            .OrderBy(s => s.DisplayOrder).Select(s => new { s.SizeId, s.Name }).ToListAsync(ct);
        var definitions = await db.MeasurementDefinitions.AsNoTracking()
            .Where(m => m.IsActive && (m.ProductId == id || m.CategoryId == product.CategoryId))
            .ToListAsync(ct);
        var measurements = MeasurementFields.Resolve(definitions, id, product.CategoryId)
            .Select(m => new { m.MeasurementId, m.Name, m.NameAr, m.Unit, m.IsRequired })
            .ToList();

        var images = await db.ProductImages.AsNoTracking()
            .Where(image => image.ProductId == id)
            .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.DisplayOrder)
            .Select(image => image.ImagePath).Take(12).ToListAsync(ct);

        return Json(new
        {
            product.ProductId, product.Name, product.NameAr,
            Price = product.DiscountPrice ?? product.RegularPrice,
            Images = images, Sizes = sizes, Measurements = measurements
        });
    }

    [HttpGet]
    public async Task<IActionResult> FindCustomers(string? search, CancellationToken ct)
    {
        search = search?.Trim() ?? "";
        if (search.Length < 2 || search.Length > 60)
            return Json(Array.Empty<object>());

        var mobile = PhoneNumbers.Normalize(search);
        var customers = await db.Customers.AsNoTracking()
            .Where(c => c.Name.Contains(search) || c.NameAr.Contains(search) ||
                (mobile != null && c.Mobile.Contains(mobile)))
            .OrderByDescending(c => c.CustomerId).Take(10)
            .Select(c => new { c.CustomerId, c.Name, c.NameAr, c.Mobile })
            .ToListAsync(ct);
        return Json(customers);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var requestId = await orders.ReserveAsync(userId, ct);
        return Json(new { requestId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] OrderSaveForm form, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (form is null || !ModelState.IsValid)
            return BadRequest(new { error = "Invalid order data." });

        try
        {
            var saved = await orders.SaveAsync(form, userId, ct);
            return Ok(saved);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
