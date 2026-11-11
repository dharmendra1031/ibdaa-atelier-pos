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
        "OrderNumber" => "IA-123",
        "BusinessName" => "TEST REPORT - IBDAA Atelier",
        "BusinessNameAr" => "إبداع أتيليه",
        "CustomerName" => "Arabic and English Customer",
        "CustomerNameAr" => "عميل عربي",
        "Total" => "80.000",
        "Paid" => "25.000",
        "Refunded" => "0.000",
        "Balance" => "55.000",
        "LogoMime" => "image/png",
        _ => "11-11-2026"
    };
}
// Small in-memory test logo; no client image or live filesystem is used.
header["Logo"] = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7+RwAAAABJRU5ErkJggg==");
data.Header.Rows.Add(header);
data.Lines.Rows.Add(1, "Long dress alteration with custom sleeves", "تعديل فستان بأكمام مخصصة",
    "Size: XL\nChest / الصدر: 100 cm\nWaist / الخصر: 85 cm", 2, "40.000", "80.000");
data.Payments.Rows.Add("KNET", "25.000", "11-11-2026 10:00");

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
    Console.WriteLine($"{name}: rendered {pdf.Length} PDF bytes.");
    var outputDirectory = Environment.GetEnvironmentVariable("INVOICE_SMOKE_OUTPUT");
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(name) + "-TEST.pdf"), pdf);
    }
}
