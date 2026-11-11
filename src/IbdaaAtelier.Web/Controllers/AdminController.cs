using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController(AtelierDbContext db) : Controller
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

        // Kuwait uses UTC+03:00 year-round; financial dates are grouped in shop-local time.
        var startUtc = start.ToDateTime(TimeOnly.MinValue).AddHours(-3);
        var endExclusiveUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-3);
        var datedOrders = db.Orders.AsNoTracking()
            .Where(o => o.CreatedAtUtc >= startUtc && o.CreatedAtUtc < endExclusiveUtc &&
                        o.Status != "Cancelled");

        var payments = db.OrderPayments.AsNoTracking()
            .Where(p => p.CreatedAtUtc >= startUtc && p.CreatedAtUtc < endExclusiveUtc);
        var refunds = db.OrderRefunds.AsNoTracking()
            .Where(r => r.CreatedAtUtc >= startUtc && r.CreatedAtUtc < endExclusiveUtc);

        var received = await payments
            .GroupBy(p => p.Method)
            .Select(group => new { Method = group.Key, Total = group.Sum(p => p.Amount) })
            .ToListAsync(ct);
        var returned = await refunds
            .GroupBy(r => r.Method)
            .Select(group => new { Method = group.Key, Total = group.Sum(r => r.Amount) })
            .ToListAsync(ct);
        var refundedByMethod = returned.ToDictionary(x => x.Method, x => x.Total);

        var activeOrders = db.Orders.AsNoTracking().Where(o => o.Status != "Cancelled");
        var totalReceivable = await activeOrders.SumAsync(o => (decimal?)o.Total, ct) ?? 0m;
        var totalReceived = await db.OrderPayments.AsNoTracking()
            .Where(p => activeOrders.Any(o => o.OrderId == p.OrderId))
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var totalRefunded = await db.OrderRefunds.AsNoTracking()
            .Where(r => activeOrders.Any(o => o.OrderId == r.OrderId))
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        return View(new AdminDashboardPage
        {
            From = start,
            To = end,
            Period = period,
            OrderCount = await datedOrders.CountAsync(ct),
            CustomerCount = await db.Customers.CountAsync(ct),
            PendingDeliveries = await activeOrders.CountAsync(o =>
                o.DeliveryDate < today && o.Status != "Delivered", ct),
            ReadyForDelivery = await activeOrders.CountAsync(o =>
                o.Status == "Ready for Delivery", ct),
            Sales = await datedOrders.SumAsync(o => (decimal?)o.Total, ct) ?? 0m,
            Collected = received.Sum(x => x.Total),
            Refunded = returned.Sum(x => x.Total),
            Outstanding = Math.Max(0m, totalReceivable - totalReceived + totalRefunded),
            PaymentMethods = received.Select(x => new PaymentMethodTotal(
                x.Method, x.Total, refundedByMethod.GetValueOrDefault(x.Method)))
                .Concat(returned.Where(x => received.All(p => p.Method != x.Method))
                    .Select(x => new PaymentMethodTotal(x.Method, 0m, x.Total)))
                .OrderBy(x => x.Method).ToList()
        });
    }
}
