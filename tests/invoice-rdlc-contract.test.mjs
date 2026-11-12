import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const a4 = read("src/IbdaaAtelier.Web/Reports/InvoiceA4.rdlc");
const receipt = read("src/IbdaaAtelier.Web/Reports/Invoice80mm.rdlc");
const xsd = read("src/IbdaaAtelier.Web/Reports/DataSets/InvoiceDataSet.xsd");
const mapper = read("src/IbdaaAtelier.Web/Services/InvoiceReportService.cs");
const endpoint = read("src/IbdaaAtelier.Web/Controllers/InvoicesController.cs");
const ci = read(".github/workflows/ci.yml");

test("separate A4 and 80mm RDLC layouts use the same XSD-defined data sources", () => {
    for (const report of [a4, receipt]) {
        assert.match(report, /InvoiceHeader/);
        assert.match(report, /InvoiceLines/);
        assert.match(report, /InvoicePayments/);
        assert.match(report, /GroupExpression/);
        assert.match(report, /Fields!Details\.Value/);
    }
    assert.match(a4, /<PageWidth>21cm<\/PageWidth>/);
    assert.match(receipt, /<PageWidth>8cm<\/PageWidth>/);
    assert.match(xsd, /msdata:IsDataSet="true"/);
    assert.match(xsd, /name="InvoiceLines"/);
    assert.match(xsd, /name="InvoicePayments"/);
});

test("invoice is mapped from saved item measurements, ledger transactions and shop image path", () => {
    assert.match(mapper, /db\.OrderItems\.AsNoTracking/);
    assert.match(mapper, /db\.OrderItemMeasurements\.AsNoTracking/);
    assert.match(mapper, /db\.OrderPayments\.AsNoTracking/);
    assert.match(mapper, /db\.OrderRefunds\.AsNoTracking/);
    assert.match(mapper, /StartsWith\("\/uploads\/shop\/"/);
    assert.match(mapper, /DataSources\.Add\(new ReportDataSource/);
    assert.match(mapper, /report\.Render\("PDF"\)/);
    assert.match(endpoint, /Authorize\(Roles = "Admin,Cashier"\)/);
});

test("Windows CI attempts real RDLC PDF rendering, not just source checks", () => {
    assert.match(ci, /invoice-render:/);
    assert.match(ci, /runs-on: windows-latest/);
    assert.match(ci, /invoice-rdlc-test-pdfs/);
    assert.match(ci, /INVOICE_SMOKE_OUTPUT/);
    assert.match(ci, /dotnet run --project tests\/InvoiceSmoke\/InvoiceSmoke.csproj/);
    assert.match(read("tests/InvoiceSmoke/Program.cs"), /%PDF/);
});

test("invoice uses one number, settings branding and typed payment/change fields", () => {
    const settings = read("src/IbdaaAtelier.Web/Models/Masters/ShopSettings.cs");
    const snapshot = read("src/IbdaaAtelier.Web/Models/Orders/OrderInvoiceSnapshot.cs");
    const controller = read("src/IbdaaAtelier.Web/Controllers/ShopSettingsController.cs");
    const preview = read("src/IbdaaAtelier.Web/Views/Invoices/Index.cshtml");
    const invoiceSmoke = read("tests/InvoiceSmoke/Program.cs");
    assert.match(settings, /ReceiptFooter/);
    assert.match(snapshot, /ReceiptFooter/);
    assert.match(controller, /settings\.ReceiptFooter =/);
    assert.match(mapper, /savedBrand\?\.ReceiptFooter \?\? shop\?\.ReceiptFooter/);
    assert.match(mapper, /payment\.Change > 0m/);
    assert.match(mapper, /payment\.Method == "Card" \? "WAMD"/);
    assert.match(mapper, /payment\.PaymentId/);
    assert.match(xsd, /name="PaymentStatus"/);
    assert.match(xsd, /name="ReceiptFooter"/);
    assert.match(xsd, /name="PaymentId" type="xs:long"/);
    for (const field of ["Tendered", "Change"]) {
        assert.match(xsd, new RegExp('name="' + field + '"'));
    }
    for (const report of [a4, receipt]) {
        assert.match(report, /Invoice No \/ رقم الفاتورة/);
        assert.match(report, /Fields!PaymentStatus\.Value/);
        assert.match(report, /Fields!ReceiptFooter\.Value/);
        assert.match(report, /Fields!Tendered\.Value/);
        assert.match(report, /Fields!Change\.Value/);
        assert.match(report, /GroupExpression>=Fields!PaymentId\.Value/);
        assert.doesNotMatch(report, /Order No \/ رقم الطلب/);
    }
    assert.match(receipt, /<Image Name="ShopLogo">/);
    assert.match(preview, /asp-route-paper="80mm"/);
    assert.match(preview, /asp-route-paper="a4"/);
    assert.match(invoiceSmoke, /"Cash", "10\.000"/);
    assert.match(invoiceSmoke, /"WAMD", "5\.000"/);
    assert.match(invoiceSmoke, /"KNET", "10\.000"/);
});
