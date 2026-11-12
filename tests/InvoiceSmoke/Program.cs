using IbdaaAtelier.Web.Reports.DataSets;
using Microsoft.Reporting.NETCore;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "..", "..", "..", "..", "..", "src", "IbdaaAtelier.Web", "Reports"));
if (!Directory.Exists(root))
    throw new InvalidOperationException("Report definitions not found: " + root);

var data = new InvoiceDataSet(Path.Combine(root, "DataSets", "InvoiceDataSet.xsd"));
var header = data.Header.NewRow();
foreach (System.Data.DataColumn column in data.Header.Columns)
{
    if (column.ColumnName == "Logo") header[column] = DBNull.Value;
    else header[column] = column.ColumnName switch
    {
        "OrderNumber" => "123",
        "BusinessName" => "TEST REPORT - IBDAA Atelier",
        "BusinessNameAr" => "إبداع أتيليه",
        "CustomerName" => "Arabic and English Customer",
        "CustomerNameAr" => "عميل عربي",
        "ShopAddress" => "Test address, Kuwait",
        "ShopAddressAr" => "عنوان اختبار - الكويت",
        "ShopMobile" => "00000000",
        "ShopEmail" => "invoice-test@example.invalid",
        "CustomerMobile" => "00000000",
        "OrderDate" => "11-11-2026 10:00",
        "DeliveryDate" => "15-11-2026",
        "Total" => "80.000",
        "Paid" => "25.000",
        "Refunded" => "0.000",
        "Balance" => "55.000",
        "LogoMime" => "image/png",
        "PaymentStatus" => "PARTIALLY PAID / مدفوع جزئياً",
        "ReceiptFooter" => "Thank you for visiting IBDAA Atelier / شكراً لزيارتكم",
        _ => "11-11-2026"
    };
}
// Small in-memory test logo; no client image or live filesystem is used.
header["Logo"] = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABHUlEQVR42u3bvQnCQBjG8cuR0oAgYmXhAqYVXMHaEdI5gaXYOoBzuIAjmM5OsBYERbGKAxhzuWjg3rv/W4Z8vb978kG4REVRqJBLq8ALAAACr7jJRlmSOnvn3N4Okc36kc1TwOXGm0JoH5u3OV9jAqQ1bpsGngK+j76pD+1786Z+uARCGP2qvkgAAAAAAAAAAAAAAAAAABBkxW0fYJ3vVH80/Hk/r8dTLQYTEgAAAMLuAcvxrHR5p9dVm9P+Y/lqOlfn/EgCAAAAAAAAAAAAAAAAAAAAAABA8PeAb3W/XFWWpCQAAAAAcA/Adsq5lCrriwTYaPk2+sYE+ILAdPmKqv3LjMT5w3USrP+5M2nNWyVAShpa/WuMN0EAAPCu3ksZVtkJgHPYAAAAAElFTkSuQmCC");
data.Header.Rows.Add(header);
data.Lines.Rows.Add(1, "Long dress alteration with custom sleeves", "تعديل فستان بأكمام مخصصة",
    "Size: XL\nChest / الصدر: 100 cm\nWaist / الخصر: 85 cm", 2, "40.000", "80.000");
data.Payments.Rows.Add(1L, "Cash", "10.000", "11-11-2026 10:00", "15.000", "5.000");
data.Payments.Rows.Add(2L, "WAMD", "5.000", "11-11-2026 10:01", "", "");
data.Payments.Rows.Add(3L, "KNET", "10.000", "11-11-2026 10:02", "", "");

foreach (var name in new[] { "InvoiceA4.rdlc", "Invoice80mm.rdlc" })
{
    using var report = new LocalReport();
    using var definition = File.OpenRead(Path.Combine(root, name));
    report.LoadReportDefinition(definition);
    report.DataSources.Add(new ReportDataSource("InvoiceHeader", data.Header));
    report.DataSources.Add(new ReportDataSource("InvoiceLines", data.Lines));
    report.DataSources.Add(new ReportDataSource("InvoicePayments", data.Payments));
    var pdf = report.Render("PDF");
    if (pdf.Length < 500 || System.Text.Encoding.ASCII.GetString(pdf.AsSpan(0, 4)) != "%PDF")
        throw new Exception(name + " did not render a PDF.");
    var pages = System.Text.RegularExpressions.Regex.Matches(
        System.Text.Encoding.Latin1.GetString(pdf), @"/Type\s*/Page\b").Count;
    if (pages != 1)
        throw new Exception(name + $" rendered {pages} pages instead of one.");
    Console.WriteLine($"{name}: rendered {pdf.Length} PDF bytes on one page.");
    var outputDirectory = Environment.GetEnvironmentVariable("INVOICE_SMOKE_OUTPUT");
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(name) + "-TEST.pdf"), pdf);
    }
}
