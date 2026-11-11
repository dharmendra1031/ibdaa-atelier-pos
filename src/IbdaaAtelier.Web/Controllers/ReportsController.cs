using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ReportsController(AtelierDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string period = "today",
        DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        DateOnly start;
        DateOnly end;
        switch (period)
        {
            case "yesterday":
                start = end = today.AddDays(-1);
                break;
            case "week":
                start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                end = today;
                break;
            case "month":
                start = new DateOnly(today.Year, today.Month, 1);
                end = today;
                break;
            case "custom" when from.HasValue && to.HasValue &&
                from.Value <= to.Value && to.Value <= today &&
                to.Value.DayNumber - from.Value.DayNumber <= 366:
                start = from.Value;
                end = to.Value;
                break;
            default:
                period = "today";
                start = end = today;
                break;
        }

        var startUtc = start.ToDateTime(TimeOnly.MinValue).AddHours(-3);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-3);
        var sales = db.Orders.AsNoTracking()
            .Where(o => o.CreatedAtUtc >= startUtc &&
                o.CreatedAtUtc < endUtc && o.Status != "Cancelled");
        var receipts = db.OrderPayments.AsNoTracking()
            .Where(p => p.CreatedAtUtc >= startUtc && p.CreatedAtUtc < endUtc);
        var refunds = db.OrderRefunds.AsNoTracking()
            .Where(r => r.CreatedAtUtc >= startUtc && r.CreatedAtUtc < endUtc);

        var receivedByMethod = await receipts.GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync(ct);
        var refundedByMethod = await refunds.GroupBy(r => r.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(r => r.Amount) }).ToListAsync(ct);
        var received = receivedByMethod.ToDictionary(x => x.Method, x => x.Total);
        var returned = refundedByMethod.ToDictionary(x => x.Method, x => x.Total);

        var activeOrders = db.Orders.AsNoTracking().Where(o => o.Status != "Cancelled");
        var receivable = await activeOrders.SumAsync(o => (decimal?)o.Total, ct) ?? 0m;
        var lifetimeReceived = await db.OrderPayments.AsNoTracking()
            .Where(p => activeOrders.Any(o => o.OrderId == p.OrderId))
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var lifetimeRefunded = await db.OrderRefunds.AsNoTracking()
            .Where(r => activeOrders.Any(o => o.OrderId == r.OrderId))
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        var itemTotals = await db.OrderItems.AsNoTracking()
            .Where(i => sales.Any(o => o.OrderId == i.OrderId))
            .GroupBy(i => new { i.ItemType, i.ProductId, i.ServiceId, i.Name, i.NameAr })
            .Select(g => new
            {
                Type = g.Key.ItemType,
                g.Key.Name,
                g.Key.NameAr,
                Quantity = g.Sum(i => i.Quantity),
                Total = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(g => g.Total).Take(20).ToListAsync(ct);
        var items = itemTotals.Select(i =>
            new ItemReportRow(i.Type, i.Name, i.NameAr, i.Quantity, i.Total)).ToList();

        var methods = received.Keys.Union(returned.Keys).OrderBy(x => x)
            .Select(x => new MethodReportRow(x, received.GetValueOrDefault(x), returned.GetValueOrDefault(x)))
            .ToList();
        return View(new SalesReportPage
        {
            From = start,
            To = end,
            Period = period,
            Orders = await sales.CountAsync(ct),
            Sales = await sales.SumAsync(o => (decimal?)o.Total, ct) ?? 0m,
            Collected = received.Values.Sum(),
            Refunded = returned.Values.Sum(),
            Outstanding = Math.Max(0m, receivable - lifetimeReceived + lifetimeRefunded),
            Methods = methods,
            Items = items
        });
    }
}
