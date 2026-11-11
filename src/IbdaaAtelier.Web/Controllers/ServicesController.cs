using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ServicesController(AtelierDbContext db) : Controller
{
    private const int PageSize = 25;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var query = db.AlterationServices.AsNoTracking();
        if (search.Length > 0)
            query = query.Where(s => s.Name.Contains(search) || s.NameAr.Contains(search));

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, totalPages);

        return View(new ServiceListPage
        {
            Categories = await db.ServiceCategories.AsNoTracking()
                .OrderBy(c => c.Name).ToListAsync(ct),
            Services = await query.OrderBy(s => s.ServiceCategoryId).ThenBy(s => s.Name)
                .ThenBy(s => s.ServiceId).Skip((page - 1) * PageSize)
                .Take(PageSize).ToListAsync(ct),
            Search = search,
            Page = page,
            TotalPages = totalPages
        });
    }

    [HttpGet]
    public IActionResult CreateCategory() => View("CategoryEditor", new ServiceCategoryForm());

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, CancellationToken ct)
    {
        var category = await db.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.ServiceCategoryId == id, ct);
        if (category is null)
            return NotFound();

        return View("CategoryEditor", new ServiceCategoryForm
        {
            ServiceCategoryId = category.ServiceCategoryId,
            Name = category.Name,
            NameAr = category.NameAr
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(ServiceCategoryForm form, CancellationToken ct)
    {
        if (form.ServiceCategoryId < 0)
            return BadRequest();

        form.Name = form.Name?.Trim() ?? "";
        form.NameAr = form.NameAr?.Trim() ?? "";
        if (form.Name.Length == 0 || form.NameAr.Length == 0)
            ModelState.AddModelError("", "Enter the English and Arabic category names.");

        if (await db.ServiceCategories.AnyAsync(c =>
            c.Name == form.Name && c.ServiceCategoryId != form.ServiceCategoryId, ct))
            ModelState.AddModelError(nameof(form.Name), "This service category already exists.");

        if (!ModelState.IsValid)
            return View("CategoryEditor", form);

        ServiceCategory category;
        if (form.ServiceCategoryId == 0)
        {
            category = new ServiceCategory();
            db.ServiceCategories.Add(category);
        }
        else
        {
            var existing = await db.ServiceCategories.SingleOrDefaultAsync(
                c => c.ServiceCategoryId == form.ServiceCategoryId, ct);
            if (existing is null)
                return NotFound();
            category = existing;
        }

        category.Name = form.Name;
        category.NameAr = form.NameAr;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit(form.ServiceCategoryId == 0 ? "ServiceCategory.Create" : "ServiceCategory.Edit",
                "ServiceCategory", category.ServiceCategoryId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Unable to save service category. Check duplicate names.");
            return View("CategoryEditor", form);
        }

        TempData["Message"] = "Service category saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id, CancellationToken ct)
    {
        var category = await db.ServiceCategories.SingleOrDefaultAsync(
            c => c.ServiceCategoryId == id, ct);
        if (category is null)
            return NotFound();

        if (category.IsActive && await db.AlterationServices.AnyAsync(
                s => s.ServiceCategoryId == id && s.IsActive, ct))
        {
            TempData["Error"] = "Deactivate active services before deactivating the category.";
            return RedirectToAction(nameof(Index));
        }

        category.IsActive = !category.IsActive;
        AddAudit(category.IsActive ? "ServiceCategory.Activate" : "ServiceCategory.Deactivate",
            "ServiceCategory", id);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Service category status updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadCategoriesAsync(ct);
        return View("Editor", new AlterationServiceForm());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var service = await db.AlterationServices.AsNoTracking()
            .SingleOrDefaultAsync(s => s.ServiceId == id, ct);
        if (service is null)
            return NotFound();

        await LoadCategoriesAsync(ct);
        return View("Editor", new AlterationServiceForm
        {
            ServiceId = service.ServiceId,
            ServiceCategoryId = service.ServiceCategoryId,
            Name = service.Name,
            NameAr = service.NameAr,
            Description = service.Description,
            DescriptionAr = service.DescriptionAr,
            Price = service.Price,
            EstimatedMinutes = service.EstimatedMinutes
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AlterationServiceForm form, CancellationToken ct)
    {
        if (form.ServiceId < 0)
            return BadRequest();

        form.Name = form.Name?.Trim() ?? "";
        form.NameAr = form.NameAr?.Trim() ?? "";
        form.Description = form.Description?.Trim();
        form.DescriptionAr = form.DescriptionAr?.Trim();

        if (form.Name.Length == 0 || form.NameAr.Length == 0)
            ModelState.AddModelError("", "Enter English and Arabic service names.");
        if (form.Price < 0 || form.Price != decimal.Round(form.Price, 3))
            ModelState.AddModelError(nameof(form.Price), "Use a non-negative KWD price with up to three decimals.");

        if (!await db.ServiceCategories.AnyAsync(
            c => c.ServiceCategoryId == form.ServiceCategoryId && c.IsActive, ct))
            ModelState.AddModelError(nameof(form.ServiceCategoryId), "Select an active service category.");

        if (await db.AlterationServices.AnyAsync(
            s => s.ServiceId != form.ServiceId &&
                s.ServiceCategoryId == form.ServiceCategoryId && s.Name == form.Name, ct))
            ModelState.AddModelError(nameof(form.Name), "This service exists in the selected category.");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        AlterationService service;
        if (form.ServiceId == 0)
        {
            service = new AlterationService();
            db.AlterationServices.Add(service);
        }
        else
        {
            var existing = await db.AlterationServices.SingleOrDefaultAsync(
                s => s.ServiceId == form.ServiceId, ct);
            if (existing is null)
                return NotFound();
            service = existing;
        }

        service.ServiceCategoryId = form.ServiceCategoryId;
        service.Name = form.Name;
        service.NameAr = form.NameAr;
        service.Description = form.Description;
        service.DescriptionAr = form.DescriptionAr;
        service.Price = form.Price;
        service.EstimatedMinutes = form.EstimatedMinutes;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit(form.ServiceId == 0 ? "Service.Create" : "Service.Edit",
                "AlterationService", service.ServiceId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Unable to save service. Check duplicate names.");
            await LoadCategoriesAsync(ct);
            return View("Editor", form);
        }

        TempData["Message"] = "Service saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var service = await db.AlterationServices.SingleOrDefaultAsync(s => s.ServiceId == id, ct);
        if (service is null)
            return NotFound();

        if (!service.IsActive && !await db.ServiceCategories.AnyAsync(
                c => c.ServiceCategoryId == service.ServiceCategoryId && c.IsActive, ct))
        {
            TempData["Error"] = "Activate the service category first.";
            return RedirectToAction(nameof(Index));
        }

        service.IsActive = !service.IsActive;
        AddAudit(service.IsActive ? "Service.Activate" : "Service.Deactivate",
            "AlterationService", id);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = "Service status updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        ViewBag.Categories = await db.ServiceCategories.AsNoTracking()
            .Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);
    }

    private void AddAudit(string action, string entityName, int entityId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = entityName,
            EntityId = entityId
        });
    }
}
