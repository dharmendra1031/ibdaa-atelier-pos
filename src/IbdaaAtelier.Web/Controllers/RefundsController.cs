using System.Security.Claims;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class RefundsController(RefundService refunds) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        try
        {
            var page = await refunds.PrepareAsync(id, userId, ct);
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
    public async Task<IActionResult> Issue(RefundForm form, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid refund details or missing reason.";
            return RedirectToAction(nameof(Issue), new { id = form.OrderId });
        }

        try
        {
            var result = await refunds.SaveAsync(form, userId, ct);
            TempData["Message"] = result.Replayed
                ? "Refund already recorded; duplicate request prevented."
                : "Refund recorded in the financial ledger. Complete external refund separately if required.";
            return RedirectToAction("Details", "Orders", new { id = form.OrderId });
        }
        catch (ArgumentException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Issue), new { id = form.OrderId });
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Details", "Orders", new { id = form.OrderId });
        }
    }
}
