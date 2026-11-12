using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Services;
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
        {
            var invoiceId = search.All(char.IsAsciiDigit) && long.TryParse(search, out var id)
                ? id : 0;
            query = query.Where(order => (invoiceId > 0 && order.OrderId == invoiceId) ||
                order.CustomerName.Contains(search) || order.CustomerNameAr.Contains(search) ||
                order.CustomerMobile.Contains(search));
        }

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + 24) / 25);
        page = Math.Clamp(page, 1, totalPages);

        var orders = await query.OrderByDescending(o => o.OrderId)
            .Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var orderIds = orders.Select(o => o.OrderId).ToArray();

        var received = await db.OrderPayments.AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId))
            .GroupBy(p => p.OrderId)
            .Select(g => new { OrderId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Amount, ct);
        var refunded = await db.OrderRefunds.AsNoTracking()
            .Where(r => orderIds.Contains(r.OrderId))
            .GroupBy(r => r.OrderId)
            .Select(g => new { OrderId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Amount, ct);

        var paid = orders.ToDictionary(o => o.OrderId, o =>
            received.GetValueOrDefault(o.OrderId) - refunded.GetValueOrDefault(o.OrderId));
        var balances = orders.ToDictionary(o => o.OrderId, o =>
            o.Status == "Cancelled" ? 0m : Math.Max(0m, o.Total - paid[o.OrderId]));

        return View(new OrderListPage
        {
            Search = search,
            Page = page,
            TotalPages = totalPages,
            Orders = orders,
            PaidByOrder = paid,
            BalanceByOrder = balances
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
        var statusHistory = await db.OrderStatusHistory.AsNoTracking()
            .Where(h => h.OrderId == id)
            .OrderBy(h => h.HistoryId).ToListAsync(ct);

        return View(new OrderDetailsPage
        {
            Order = order,
            Items = items,
            Measurements = measurements,
            Payments = receipts,
            Refunds = refunds,
            StatusHistory = statusHistory,
            NextStatuses = User.IsInRole("Admin") ? OrderStatusService.Next(order.Status) : [],
            GrossPaid = grossPaid,
            Refunded = refunded,
            Paid = paid,
            Balance = order.Status == "Cancelled" ? 0m : Math.Max(0m, order.Total - paid)
        });
    }
}
