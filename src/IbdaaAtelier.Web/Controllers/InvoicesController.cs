using IbdaaAtelier.Web.Services;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class InvoicesController(InvoiceReportService invoices, PrintQueueService printJobs, AtelierDbContext db) : Controller
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
    public async Task<IActionResult> PrintDirect(long id, int terminalId, CancellationToken ct = default)
    {
        if (id <= 0 || terminalId <= 0)
            return BadRequest(new { error = "Pair a cashier printer before using direct print." });

        var pdfMode = await db.ShopSettings.AsNoTracking()
            .Select(settings => (bool?)settings.OpenInvoicePdfInNewTab).SingleOrDefaultAsync(ct) ?? true;
        if (pdfMode)
            return Conflict(new { error = "Shop settings currently open the PDF in a new tab." });

        try
        {
            var job = await printJobs.QueueAsync(id, terminalId, ct);
            return Ok(new { job.PrintJobId, job.Status });
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpGet, Authorize(Roles = "Cashier")]
    public async Task<IActionResult> PrintStatus(long jobId, CancellationToken ct = default)
    {
        if (jobId <= 0) return BadRequest();
        var job = await printJobs.GetStatusAsync(jobId, ct);
        if (job is null) return NotFound();
        return Ok(new
        {
            status = PrintQueueService.DisplayStatus(job),
            error = job.ErrorMessage
        });
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
