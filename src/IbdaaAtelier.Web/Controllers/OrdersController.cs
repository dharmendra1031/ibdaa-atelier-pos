using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class OrdersController(AtelierDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var query = db.Orders.AsNoTracking();
        if (search.Length > 0)
            query = query.Where(order => order.OrderNumber.Contains(search) ||
                order.CustomerName.Contains(search) || order.CustomerNameAr.Contains(search) ||
                order.CustomerMobile.Contains(search));

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + 24) / 25);
        page = Math.Clamp(page, 1, totalPages);

        return View(new OrderListPage
        {
            Search = search,
            Page = page,
            TotalPages = totalPages,
            Orders = await query.OrderByDescending(o => o.OrderId)
                .Skip((page - 1) * 25).Take(25).ToListAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == id, ct);
        if (order is null)
            return NotFound();

        var items = await db.OrderItems.AsNoTracking()
            .Where(item => item.OrderId == id).OrderBy(item => item.OrderItemId).ToListAsync(ct);
        var itemIds = items.Select(item => item.OrderItemId).ToArray();
        var measurements = await db.OrderItemMeasurements.AsNoTracking()
            .Where(m => itemIds.Contains(m.OrderItemId))
            .OrderBy(m => m.OrderMeasurementId).ToListAsync(ct);
        var receipts = await db.OrderPayments.AsNoTracking()
            .Where(p => p.OrderId == id).OrderBy(p => p.PaymentId).ToListAsync(ct);
        var refunds = await db.OrderRefunds.AsNoTracking()
            .Where(r => r.OrderId == id).OrderBy(r => r.RefundId).ToListAsync(ct);
        var grossPaid = receipts.Sum(p => p.Amount);
        var refunded = refunds.Sum(r => r.Amount);
        var paid = grossPaid - refunded;

        return View(new OrderDetailsPage
        {
            Order = order,
            Items = items,
            Measurements = measurements,
            Payments = receipts,
            Refunds = refunds,
            GrossPaid = grossPaid,
            Refunded = refunded,
            Paid = paid,
            Balance = order.Status == "Cancelled" ? 0m : Math.Max(0m, order.Total - paid)
        });
    }
}
