using IbdaaAtelier.Web.Services;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class InvoicesController(InvoiceReportService invoices, AtelierDbContext db) : Controller
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
