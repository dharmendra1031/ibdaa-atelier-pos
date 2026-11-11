using System.Security.Claims;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class OrderStatusController(OrderStatusService statuses) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Change(OrderStatusForm form, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid status update.";
            return RedirectToAction("Details", "Orders", new { id = form.OrderId });
        }

        try
        {
            await statuses.ChangeAsync(form, userId, ct);
            TempData["Message"] = "Order status updated.";
        }
        catch (ArgumentException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Orders", new { id = form.OrderId });
    }
}
