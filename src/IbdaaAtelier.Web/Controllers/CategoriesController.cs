using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class CategoriesController(AtelierDbContext db) : Controller
{
    private const int PageSize = 25;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var query = db.ProductCategories.AsNoTracking();
        if (search.Length > 0)
            query = query.Where(category => category.Name.Contains(search) ||
                (category.NameAr != null && category.NameAr.Contains(search)));

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, totalPages);

        var categories = await query.OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.CategoryId)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);

        var parentIds = categories.Where(item => item.ParentCategoryId.HasValue)
            .Select(item => item.ParentCategoryId!.Value).Distinct().ToList();
        var parents = await db.ProductCategories.AsNoTracking()
            .Where(item => parentIds.Contains(item.CategoryId))
            .ToDictionaryAsync(item => item.CategoryId, item => item.Name, ct);

        return View(new CategoryListPage
        {
            Search = search,
            Page = page,
            TotalPages = totalPages,
            Categories = categories.Select(item => new CategoryListRow(
                item.CategoryId, item.Name, item.NameAr,
                item.ParentCategoryId is int parentId && parents.TryGetValue(parentId, out var name) ? name : "",
                item.IsActive, item.DisplayOrder)).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadParentsAsync(null, ct);
        return View("Editor", new CategoryForm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryForm form, CancellationToken ct)
    {
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(null, ct);
            return View("Editor", form);
        }

        var category = new ProductCategory();
        CopyValues(form, category);
        db.ProductCategories.Add(category);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit("Category.Create", category.CategoryId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not save category. Check duplicate names.");
            await LoadParentsAsync(null, ct);
            return View("Editor", form);
        }

        TempData["Message"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var category = await db.ProductCategories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == id, ct);
        if (category is null)
            return NotFound();

        await LoadParentsAsync(id, ct);
        return View("Editor", new CategoryForm
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            NameAr = category.NameAr ?? "",
            Description = category.Description,
            DescriptionAr = category.DescriptionAr,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryForm form, CancellationToken ct)
    {
        if (form.CategoryId <= 0)
            return BadRequest();

        var category = await db.ProductCategories.SingleOrDefaultAsync(
            item => item.CategoryId == form.CategoryId, ct);
        if (category is null)
            return NotFound();

        await ValidateAsync(form, ct);
        if (form.ParentCategoryId.HasValue &&
            await db.ProductCategories.AnyAsync(item => item.ParentCategoryId == category.CategoryId, ct))
            ModelState.AddModelError(nameof(form.ParentCategoryId),
                "A category with subcategories must remain top-level.");

        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(form.CategoryId, ct);
            return View("Editor", form);
        }

        CopyValues(form, category);
        AddAudit("Category.Edit", category.CategoryId);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not save category. Check duplicate names.");
            await LoadParentsAsync(form.CategoryId, ct);
            return View("Editor", form);
        }

        TempData["Message"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var category = await db.ProductCategories.SingleOrDefaultAsync(item => item.CategoryId == id, ct);
        if (category is null)
            return NotFound();

        if (category.IsActive &&
            await db.ProductCategories.AnyAsync(item => item.ParentCategoryId == id && item.IsActive, ct))
        {
            TempData["Error"] = "Deactivate active subcategories first.";
            return RedirectToAction(nameof(Index));
        }

        if (category.IsActive &&
            await db.Products.AnyAsync(item => item.CategoryId == id && item.IsActive, ct))
        {
            TempData["Error"] = "Deactivate active products in this category first.";
            return RedirectToAction(nameof(Index));
        }

        category.IsActive = !category.IsActive;
        AddAudit(category.IsActive ? "Category.Activate" : "Category.Deactivate", id);
        await db.SaveChangesAsync(ct);
        TempData["Message"] = category.IsActive ? "Category activated." : "Category deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(CategoryForm form, CancellationToken ct)
    {
        form.Name = form.Name?.Trim() ?? "";
        form.NameAr = form.NameAr?.Trim() ?? "";
        form.Description = form.Description?.Trim();
        form.DescriptionAr = form.DescriptionAr?.Trim();

        if (form.Name.Length == 0)
            ModelState.AddModelError(nameof(form.Name), "English name is required.");
        if (form.NameAr.Length == 0)
            ModelState.AddModelError(nameof(form.NameAr), "Arabic name is required.");

        if (form.ParentCategoryId is int parentId)
        {
            var validParent = await db.ProductCategories.AsNoTracking()
                .AnyAsync(item => item.CategoryId == parentId &&
                    item.ParentCategoryId == null && item.IsActive, ct);
            if (!validParent || parentId == form.CategoryId)
                ModelState.AddModelError(nameof(form.ParentCategoryId), "Select an active top-level category.");
        }

        if (form.Name.Length > 0 &&
            await db.ProductCategories.AnyAsync(item =>
                item.CategoryId != form.CategoryId &&
                item.ParentCategoryId == form.ParentCategoryId &&
                item.Name == form.Name, ct))
            ModelState.AddModelError(nameof(form.Name), "A category with this name already exists at this level.");
    }

    private async Task LoadParentsAsync(int? excludeId, CancellationToken ct)
    {
        ViewBag.Parents = await db.ProductCategories.AsNoTracking()
            .Where(item => item.IsActive && item.ParentCategoryId == null &&
                (!excludeId.HasValue || item.CategoryId != excludeId.Value))
            .OrderBy(item => item.Name).ToListAsync(ct);
    }

    private static void CopyValues(CategoryForm form, ProductCategory category)
    {
        category.Name = form.Name;
        category.NameAr = form.NameAr;
        category.Description = form.Description;
        category.DescriptionAr = form.DescriptionAr;
        category.ParentCategoryId = form.ParentCategoryId;
        category.DisplayOrder = form.DisplayOrder;
    }

    private void AddAudit(string action, int categoryId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = "ProductCategory",
            EntityId = categoryId
        });
    }
}
