using System.Security.Claims;
using IbdaaAtelier.Web.Models.Registers;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class RegisterController(CashRegisterService register) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();

        var page = await register.GetPageAsync(ct);
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(decimal openingCash, CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();
        if (!ModelState.IsValid)
            return Failed("Enter a valid opening cash amount.");

        try
        {
            await register.OpenAsync(openingCash, userId, ct);
            TempData["Message"] = "Cash register is open.";
        }
        catch (ArgumentException exception) { return Failed(exception.Message); }
        catch (InvalidOperationException exception) { return Failed(exception.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(long sessionId, decimal countedCash, CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();
        if (!ModelState.IsValid)
            return Failed("Enter a valid actual closing cash count.");

        try
        {
            var result = await register.CloseAsync(sessionId, countedCash, userId, ct);
            TempData["Message"] = result.Replayed
                ? "This cash register session was already closed."
                : $"Register closed. Cash difference: {result.Difference:0.000} KWD.";
        }
        catch (ArgumentException exception) { return Failed(exception.Message); }
        catch (InvalidOperationException exception) { return Failed(exception.Message); }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ReserveExpense(long sessionId, CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();

        try
        {
            var requestId = await register.ReserveExpenseAsync(sessionId, userId, ct);
            return Ok(new { requestId });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Expense(CashExpenseForm form, CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();
        if (!ModelState.IsValid)
            return Failed("Expense amount and reason are required.");

        try
        {
            var replayed = await register.AddExpenseAsync(form, userId, ct);
            TempData["Message"] = replayed
                ? "Cash expense already recorded. Duplicate prevented."
                : "Cash expense recorded.";
        }
        catch (ArgumentException exception) { return Failed(exception.Message); }
        catch (InvalidOperationException exception) { return Failed(exception.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long sessionId, CancellationToken ct)
    {
        if (!CurrentUser(out var userId))
            return Unauthorized();

        try
        {
            await register.ApproveAsync(sessionId, userId, ct);
            TempData["Message"] = "Register closing approved.";
        }
        catch (InvalidOperationException exception) { return Failed(exception.Message); }

        return RedirectToAction(nameof(Index));
    }

    private bool CurrentUser(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private IActionResult Failed(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
