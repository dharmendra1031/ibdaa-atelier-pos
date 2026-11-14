using System.Globalization;
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
public sealed class PosController(AtelierDbContext db, PosOrderService orders, PaymentService payments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1,
        string? serviceSearch = null, int? serviceCategoryId = null, int servicePage = 1,
        bool services = false, long? invoiceId = null, bool receive = false,
        CancellationToken ct = default)
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
            OpenInvoiceId = invoiceId is > 0 ? invoiceId : null,
            OpenInvoicePayment = invoiceId is > 0 && receive,
            CategoryId = categoryId,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            NextInvoiceNumber = await GetNextInvoiceNumberAsync(ct),
            EnablePosDiscount = await db.ShopSettings.AsNoTracking()
                .Select(s => s.EnablePosDiscount).SingleOrDefaultAsync(ct),
            OpenInvoicePdfInNewTab = await db.ShopSettings.AsNoTracking()
                .Select(s => (bool?)s.OpenInvoicePdfInNewTab).SingleOrDefaultAsync(ct) ?? true
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

    [HttpGet]
    public async Task<IActionResult> FindInvoice(string? number, CancellationToken ct)
    {
        var input = number?.Trim() ?? "";
        if (input.Length is < 1 or > 20 || !input.All(char.IsAsciiDigit) ||
            !long.TryParse(input, out var invoiceId) || invoiceId <= 0)
            return BadRequest(new { error = "Enter a numeric invoice number." });

        var order = await db.Orders.AsNoTracking()
            .Where(o => o.OrderId == invoiceId)
            .Select(o => new
            {
                o.OrderId, o.CustomerName, o.CustomerNameAr,
                o.CustomerMobile, o.DeliveryDate, o.Status,
                o.Subtotal, o.DiscountAmount, o.Total
            })
            .SingleOrDefaultAsync(ct);
        if (order is null)
            return NotFound(new { error = "Invoice not found." });

        var items = await db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == order.OrderId)
            .OrderBy(i => i.OrderItemId)
            .Select(i => new { i.Name, i.NameAr, i.SizeName, i.Quantity, i.UnitPrice, i.LineTotal })
            .ToListAsync(ct);
        var paid = await payments.GetPaidAsync(order.OrderId, ct);
        var previousPayments = await db.OrderPayments.AsNoTracking()
            .Where(payment => payment.OrderId == order.OrderId)
            .OrderBy(payment => payment.PaymentId)
            .Select(payment => new
            {
                payment.Method, payment.Amount, payment.Change, payment.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Json(new
        {
            order.OrderId, order.CustomerName, order.CustomerNameAr,
            order.CustomerMobile, order.DeliveryDate, order.Status,
            order.Subtotal, order.DiscountAmount, order.Total,
            Paid = paid,
            Balance = order.Status == "Cancelled" ? 0m : Math.Max(0m, order.Total - paid),
            Items = items,
            Payments = previousPayments.Select(payment => new
            {
                payment.Method, payment.Amount, payment.Change,
                Date = payment.CreatedAtUtc.AddHours(3)
                    .ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var requestId = await orders.ReserveAsync(userId, ct);
        return Json(new { requestId, nextInvoiceNumber = await GetNextInvoiceNumberAsync(ct) });
    }

    private async Task<long> GetNextInvoiceNumberAsync(CancellationToken ct)
    {
        var lastInvoiceId = await db.Orders.AsNoTracking()
            .MaxAsync(order => (long?)order.OrderId, ct) ?? 0;
        return checked(lastInvoiceId + 1);
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
