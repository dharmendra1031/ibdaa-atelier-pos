using System.Globalization;
using System.Text;
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
        var (selectedPeriod, start, end) = SelectRange(period, from, to);
        period = selectedPeriod;

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

    // Both the screen and export use the same Kuwait-local reporting window.
    private static (string Period, DateOnly Start, DateOnly End) SelectRange(
        string period, DateOnly? from, DateOnly? to)
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


        return (period, start, end);
    }

    [HttpGet]
    public async Task ExportSales(string period = "today",
        DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var (_, start, end) = SelectRange(period, from, to);
        var startUtc = start.ToDateTime(TimeOnly.MinValue).AddHours(-3);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-3);

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.CacheControl = "no-store";
        Response.Headers.ContentDisposition = "attachment; filename=\"IBDAA-sales-" +
            start.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" +
            end.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".csv\"";

        await using var writer = new StreamWriter(Response.Body,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 8192, leaveOpen: true);
        await writer.WriteLineAsync("Invoice Number,Order Date (Kuwait),Customer,Mobile,Status,Total KWD,Net Paid KWD,Refunded KWD,Balance KWD");

        var sales = db.Orders.AsNoTracking().Where(order =>
            order.CreatedAtUtc >= startUtc && order.CreatedAtUtc < endUtc &&
            order.Status != "Cancelled");

        long lastOrderId = 0;
        while (true)
        {
            var page = await sales.Where(order => order.OrderId > lastOrderId)
                .OrderBy(order => order.OrderId)
                .Take(250)
                .Select(order => new
                {
                    order.OrderId,
                    order.CreatedAtUtc,
                    order.CustomerName,
                    order.CustomerMobile,
                    order.Status,
                    order.Total,
                    Received = db.OrderPayments.Where(p => p.OrderId == order.OrderId)
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    Returned = db.OrderRefunds.Where(p => p.OrderId == order.OrderId)
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync(ct);

            if (page.Count == 0)
                break;

            foreach (var order in page)
            {
                var paid = order.Received - order.Returned;
                var fields = new[]
                {
                    order.OrderId.ToString(CultureInfo.InvariantCulture),
                    order.CreatedAtUtc.AddHours(3).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    order.CustomerName,
                    order.CustomerMobile,
                    order.Status,
                    Money(order.Total),
                    Money(paid),
                    Money(order.Returned),
                    Money(Math.Max(0m, order.Total - paid))
                };

                await writer.WriteLineAsync(string.Join(",", fields.Select(CsvCell)).AsMemory(), ct);
            }

            lastOrderId = page[^1].OrderId;
            await writer.FlushAsync(ct);
        }
    }

    private static string Money(decimal value) => value.ToString("0.000", CultureInfo.InvariantCulture);

    private static string CsvCell(string? value)
    {
        value ??= "";
        // Spreadsheet applications may evaluate user-supplied names as formulae.
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') ||
            value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

}
