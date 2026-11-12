using System.Security.Claims;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Cashier")]
public sealed class PaymentsController(PaymentService payments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Take(long id, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        try
        {
            var page = await payments.PrepareAsync(id, userId, ct);
            return View(page);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Details", "Orders", new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Prepare([FromBody] long orderId, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (orderId <= 0)
            return BadRequest(new { error = "Invalid invoice." });

        try
        {
            var page = await payments.PrepareAsync(orderId, userId, ct);
            return Ok(new { page.Form.RequestId, page.OrderId, page.Balance });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Record([FromBody] PaymentForm form, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (form is null || !ModelState.IsValid)
            return BadRequest(new { error = "Invalid payment details." });

        try
        {
            var receipt = await payments.SaveAsync(form, userId, ct);
            return Ok(receipt);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(PaymentForm form, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid payment details.";
            return RedirectToAction(nameof(Take), new { id = form.OrderId });
        }

        try
        {
            var receipt = await payments.SaveAsync(form, userId, ct);
            TempData["Message"] = receipt.Replayed
                ? "Payment already saved. Duplicate entry was prevented."
                : "Payment recorded successfully. Updated invoice is ready to print.";
            return RedirectToAction("Index", "Invoices", new { id = form.OrderId, paper = "80mm" });
        }
        catch (ArgumentException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Take), new { id = form.OrderId });
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Details", "Orders", new { id = form.OrderId });
        }
    }
}
