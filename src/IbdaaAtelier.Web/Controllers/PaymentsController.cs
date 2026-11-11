using System.Security.Claims;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
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
                : "Payment recorded successfully.";
            return RedirectToAction("Details", "Orders", new { id = form.OrderId });
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
