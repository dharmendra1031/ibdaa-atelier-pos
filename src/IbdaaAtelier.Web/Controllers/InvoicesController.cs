using IbdaaAtelier.Web.Services;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class InvoicesController(InvoiceReportService invoices, DirectInvoicePrinter printer, AtelierDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(long id, string paper = "80mm", CancellationToken ct = default)
    {
        if (id <= 0 || paper is not ("a4" or "80mm"))
            return BadRequest();

        if (!await db.Orders.AnyAsync(order => order.OrderId == id, ct))
            return NotFound();

        return View(new InvoicePreviewPage(id, paper));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Cashier")]
    public async Task<IActionResult> PrintDirect(long id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest();

        if (HttpContext.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip))
            return Conflict(new { error = "Direct printing is available only on the cashier PC running this app." });

        var openPdf = await db.ShopSettings.AsNoTracking()
            .Select(settings => settings.OpenInvoicePdfInNewTab).SingleOrDefaultAsync(ct);
        if (openPdf)
            return Conflict(new { error = "Shop settings currently open the PDF in a new tab." });

        var data = await invoices.PrepareAsync(id, ct);
        if (data is null)
            return NotFound();

        try
        {
            await printer.PrintAsync(invoices.RenderPdf(data, "80mm"), ct);
            return Ok(new { queued = true });
        }
        catch (InvalidOperationException error)
        {
            return StatusCode(503, new { error = error.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Print(long id, string paper = "a4", CancellationToken ct = default)
    {
        if (id <= 0 || paper is not ("a4" or "80mm"))
            return BadRequest();

        var data = await invoices.PrepareAsync(id, ct);
        if (data is null)
            return NotFound();

        var pdf = invoices.RenderPdf(data, paper);
        Response.Headers.CacheControl = "no-store";
        return File(pdf, "application/pdf");
    }
}
