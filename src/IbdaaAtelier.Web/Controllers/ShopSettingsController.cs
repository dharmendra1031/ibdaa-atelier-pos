using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ShopSettingsController(AtelierDbContext db, ProductImageFiles images, IWebHostEnvironment environment) : Controller
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
            LogoPath = settings?.LogoPath,
            ReceiptFooter = settings?.ReceiptFooter,
            RequireRegisterOpen = settings?.RequireRegisterOpen ?? false,
            EnablePosDiscount = settings?.EnablePosDiscount ?? false,
            OpenInvoicePdfInNewTab = settings?.OpenInvoicePdfInNewTab ?? true
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(ProductImageFiles.MaxFileBytes + 100_000)]
    public async Task<IActionResult> UploadLogo(IFormFile? logo, CancellationToken ct)
    {
        if (logo is null)
        {
            TempData["Error"] = "Choose a PNG or JPEG logo for invoices.";
            return RedirectToAction(nameof(Index));
        }

        var settings = await db.ShopSettings.SingleOrDefaultAsync(ct);
        if (settings is null)
        {
            TempData["Error"] = "Save your shop details before uploading a logo.";
            return RedirectToAction(nameof(Index));
        }

        byte[] bytes;
        string extension;
        try
        {
            (bytes, extension) = await images.ReadAsync(logo, ct);
        }
        catch (ArgumentException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }

        if (extension is not (".png" or ".jpg"))
        {
            TempData["Error"] = "Upload a PNG or JPEG logo to print it on invoices.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(environment.WebRootPath))
            throw new InvalidOperationException("Shop image storage is not configured.");

        var folder = Path.Combine(environment.WebRootPath, "uploads", "shop");
        Directory.CreateDirectory(folder);
        var fileName = "logo-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + extension;
        var fullPath = Path.Combine(folder, fileName);
        var publicPath = "/uploads/shop/" + fileName;

        await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, useAsync: true))
            await output.WriteAsync(bytes, ct);

        try
        {
            settings.LogoPath = publicPath;
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId > 0 ? userId : null,
                Action = "ShopSettings.UpdateLogo",
                EntityName = "ShopSettings",
                EntityId = settings.SettingsId
            });
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            System.IO.File.Delete(fullPath);
            throw;
        }

        // Keep previous logo files: saved invoice snapshots may still refer to them.
        // The uploads/shop folder must be included in deployment backups.

        TempData["Message"] = "Shop logo updated.";
        return RedirectToAction(nameof(Index));
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
        form.ReceiptFooter = form.ReceiptFooter?.Trim();

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
            "SELECT * FROM dbo.ShopSettings WITH (UPDLOCK, HOLDLOCK)")
            .SingleOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new ShopSettings();
            db.ShopSettings.Add(settings);
        }

        settings.RequireRegisterOpen = form.RequireRegisterOpen;
        settings.EnablePosDiscount = form.EnablePosDiscount;
        settings.OpenInvoicePdfInNewTab = form.OpenInvoicePdfInNewTab;
        settings.BusinessName = form.BusinessName;
        settings.BusinessNameAr = form.BusinessNameAr;
        settings.Mobile = string.IsNullOrWhiteSpace(form.Mobile) ? null : form.Mobile;
        settings.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email;
        settings.Address = string.IsNullOrWhiteSpace(form.Address) ? null : form.Address;
        settings.AddressAr = string.IsNullOrWhiteSpace(form.AddressAr) ? null : form.AddressAr;
        settings.ReceiptFooter = string.IsNullOrWhiteSpace(form.ReceiptFooter) ? null : form.ReceiptFooter;

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
