using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ShopSettingsController(AtelierDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await db.ShopSettings.AsNoTracking().SingleOrDefaultAsync(ct);
        return View(new ShopSettingsForm
        {
            BusinessName = settings?.BusinessName ?? "",
            BusinessNameAr = settings?.BusinessNameAr ?? "",
            Mobile = settings?.Mobile,
            Email = settings?.Email,
            Address = settings?.Address,
            AddressAr = settings?.AddressAr,
            LogoPath = settings?.LogoPath
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ShopSettingsForm form, CancellationToken ct)
    {
        form.BusinessName = form.BusinessName?.Trim() ?? "";
        form.BusinessNameAr = form.BusinessNameAr?.Trim() ?? "";
        form.Mobile = form.Mobile?.Trim();
        form.Email = form.Email?.Trim();
        form.Address = form.Address?.Trim();
        form.AddressAr = form.AddressAr?.Trim();

        if (form.BusinessName.Length == 0)
            ModelState.AddModelError(nameof(form.BusinessName), "English business name is required.");
        if (form.BusinessNameAr.Length == 0)
            ModelState.AddModelError(nameof(form.BusinessNameAr), "Arabic business name is required.");

        if (!ModelState.IsValid)
        {
            form.LogoPath = await db.ShopSettings.AsNoTracking().Select(s => s.LogoPath).SingleOrDefaultAsync(ct);
            return View("Index", form);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var settings = await db.ShopSettings.FromSqlRaw(
            "SELECT * FROM dbo.ShopSettings WITH (UPDLOCK, HOLDLOCK) WHERE SettingsId = 1")
            .SingleOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new ShopSettings();
            db.ShopSettings.Add(settings);
        }

        settings.BusinessName = form.BusinessName;
        settings.BusinessNameAr = form.BusinessNameAr;
        settings.Mobile = string.IsNullOrWhiteSpace(form.Mobile) ? null : form.Mobile;
        settings.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email;
        settings.Address = string.IsNullOrWhiteSpace(form.Address) ? null : form.Address;
        settings.AddressAr = string.IsNullOrWhiteSpace(form.AddressAr) ? null : form.AddressAr;

        await db.SaveChangesAsync(ct);

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "ShopSettings.Update",
            EntityName = "ShopSettings",
            EntityId = settings.SettingsId
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData["Message"] = "Shop settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
