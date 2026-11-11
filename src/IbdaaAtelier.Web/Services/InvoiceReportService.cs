using System.Globalization;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Reports.DataSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;

namespace IbdaaAtelier.Web.Services;

public sealed class InvoiceReportService(AtelierDbContext db, IWebHostEnvironment environment)
{
    private static readonly CultureInfo MoneyCulture = CultureInfo.InvariantCulture;

    public async Task<InvoiceDataSet?> PrepareAsync(long orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order is null)
            return null;

        var items = await db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .OrderBy(i => i.OrderItemId)
            .ToListAsync(ct);
        var itemIds = items.Select(i => i.OrderItemId).ToArray();
        var measurements = await db.OrderItemMeasurements.AsNoTracking()
            .Where(m => itemIds.Contains(m.OrderItemId))
            .OrderBy(m => m.OrderMeasurementId)
            .ToListAsync(ct);
        var payments = await db.OrderPayments.AsNoTracking()
            .Where(p => p.OrderId == orderId).OrderBy(p => p.PaymentId).ToListAsync(ct);
        var refunds = await db.OrderRefunds.AsNoTracking()
            .Where(r => r.OrderId == orderId).OrderBy(r => r.RefundId).ToListAsync(ct);
        var savedBrand = await db.OrderInvoiceSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(s => s.OrderId == orderId, ct);
        // Older orders made before the snapshot migration retain the original
        // item details but may not have a historical shop-branding snapshot.
        var shop = savedBrand is null
            ? await db.ShopSettings.AsNoTracking().SingleOrDefaultAsync(ct)
            : null;

        var schema = Path.Combine(environment.ContentRootPath,
            "Reports", "DataSets", "InvoiceDataSet.xsd");
        var data = new InvoiceDataSet(schema);
        var grossPaid = payments.Sum(p => p.Amount);
        var refunded = refunds.Sum(r => r.Amount);
        var netPaid = grossPaid - refunded;
        var balance = order.Status == "Cancelled" ? 0m : Math.Max(0m, order.Total - netPaid);

        byte[]? logo = null;
        string logoMime = "";
        if ((savedBrand?.LogoPath ?? shop?.LogoPath) is { } path &&
            path.StartsWith("/uploads/shop/", StringComparison.Ordinal))
        {
            var fileName = path["/uploads/shop/".Length..];
            if (Path.GetFileName(fileName) == fileName && !fileName.Contains('\\'))
            {
                var fullPath = Path.Combine(environment.WebRootPath, "uploads", "shop", fileName);
                if (File.Exists(fullPath))
                {
                    var ext = Path.GetExtension(fullPath).ToLowerInvariant();
                    logoMime = ext switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        _ => ""
                    };
                    if (logoMime.Length > 0)
                        logo = await File.ReadAllBytesAsync(fullPath, ct);
                }
            }
        }

        var header = data.Header.NewRow();
        header["OrderNumber"] = order.OrderNumber;
        header["OrderDate"] = order.CreatedAtUtc.AddHours(3)
            .ToString("dd-MM-yyyy HH:mm", MoneyCulture);
        header["DeliveryDate"] = order.DeliveryDate.ToString("dd-MM-yyyy", MoneyCulture);
        header["BusinessName"] = savedBrand?.BusinessName ?? shop?.BusinessName ?? "IBDAA Atelier";
        header["BusinessNameAr"] = savedBrand?.BusinessNameAr ?? shop?.BusinessNameAr ?? "";
        header["ShopAddress"] = savedBrand?.Address ?? shop?.Address ?? "";
        header["ShopAddressAr"] = savedBrand?.AddressAr ?? shop?.AddressAr ?? "";
        header["ShopMobile"] = savedBrand?.Mobile ?? shop?.Mobile ?? "";
        header["ShopEmail"] = savedBrand?.Email ?? shop?.Email ?? "";
        header["CustomerName"] = order.CustomerName;
        header["CustomerNameAr"] = order.CustomerNameAr;
        header["CustomerMobile"] = order.CustomerMobile;
        header["Total"] = Money(order.Total);
        header["Paid"] = Money(netPaid);
        header["Refunded"] = Money(refunded);
        header["Balance"] = Money(balance);
        header["LogoMime"] = logoMime;
        header["Logo"] = logo is null ? DBNull.Value : logo;
        data.Header.Rows.Add(header);

        var lineNumber = 0;
        foreach (var item in items)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.ProductCode))
                details.Add("SKU: " + item.ProductCode);
            if (!string.IsNullOrWhiteSpace(item.SizeName))
                details.Add("Size / المقاس: " + item.SizeName);
            details.AddRange(measurements
                .Where(m => m.OrderItemId == item.OrderItemId)
                .Select(m => $"{m.Name} / {m.NameAr}: {m.Value:0.##} {m.Unit}"));
            if (!string.IsNullOrWhiteSpace(item.Notes))
                details.Add(item.Notes);

            data.Lines.Rows.Add(++lineNumber, item.Name, item.NameAr,
                string.Join(Environment.NewLine, details), item.Quantity,
                Money(item.UnitPrice), Money(item.LineTotal));
        }

        foreach (var payment in payments)
            data.Payments.Rows.Add(payment.Method, Money(payment.Amount),
                payment.CreatedAtUtc.AddHours(3).ToString("dd-MM-yyyy HH:mm", MoneyCulture));
        foreach (var refund in refunds)
            data.Payments.Rows.Add("REFUND " + refund.Method, "-" + Money(refund.Amount),
                refund.CreatedAtUtc.AddHours(3).ToString("dd-MM-yyyy HH:mm", MoneyCulture));

        return data;
    }

    public byte[] RenderPdf(InvoiceDataSet data, string paper)
    {
        var reportName = paper switch
        {
            "a4" => "InvoiceA4.rdlc",
            "80mm" => "Invoice80mm.rdlc",
            _ => throw new ArgumentException("Unknown paper size.", nameof(paper))
        };

        var file = Path.Combine(environment.ContentRootPath, "Reports", reportName);
        using var definition = File.OpenRead(file);
        using var report = new LocalReport();
        report.LoadReportDefinition(definition);
        report.DataSources.Add(new ReportDataSource("InvoiceHeader", data.Header));
        report.DataSources.Add(new ReportDataSource("InvoiceLines", data.Lines));
        report.DataSources.Add(new ReportDataSource("InvoicePayments", data.Payments));
        return report.Render("PDF");
    }

    private static string Money(decimal amount) => amount.ToString("0.000", MoneyCulture);
}
