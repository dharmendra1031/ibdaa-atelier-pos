using System.Security.Claims;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Cashier")]
public sealed class PaymentsController(PaymentService payments) : Controller
{
    // Old bookmarked payment links continue into the POS payment popup.
    [HttpGet]
    public IActionResult Take(long id)
    {
        if (id <= 0)
            return NotFound();

        return RedirectToAction("Index", "Pos", new { invoiceId = id, receive = true });
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

}
