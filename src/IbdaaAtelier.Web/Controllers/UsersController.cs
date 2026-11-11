using System.Data;
using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UsersController(AtelierDbContext db, IPasswordHasher<ShopUser> hasher) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new ShopUser
            {
                UserId = u.UserId, UserName = u.UserName, Role = u.Role,
                IsActive = u.IsActive, CreatedAtUtc = u.CreatedAtUtc
            }).ToListAsync(ct);
        return View(users);
    }

    [HttpGet]
    public IActionResult Create() => View(new UserCreateForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateForm form, CancellationToken ct)
    {
        form.UserName = form.UserName?.Trim() ?? "";
        if (!ValidRole(form.Role))
            ModelState.AddModelError(nameof(form.Role), "Choose Admin or Cashier.");
        if (await db.Users.AnyAsync(u => u.UserName == form.UserName, ct))
            ModelState.AddModelError(nameof(form.UserName), "Username already exists.");

        if (!ModelState.IsValid)
            return View(form);

        var user = new ShopUser
        {
            UserName = form.UserName,
            Role = form.Role,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, form.Password);
        db.Users.Add(user);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit("User.Create", user.UserId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not create user. Check for duplicate username.");
            return View(form);
        }

        TempData["Message"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == id, ct);
        if (user is null)
            return NotFound();

        ViewBag.UserName = user.UserName;
        return View(new UserEditForm { UserId = user.UserId, Role = user.Role, IsActive = user.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditForm form, CancellationToken ct)
    {
        if (!ValidRole(form.Role))
            ModelState.AddModelError(nameof(form.Role), "Choose Admin or Cashier.");
        if (!ModelState.IsValid)
        {
            await LoadUserNameAsync(form.UserId, ct);
            return View(form);
        }

        var currentUserId = CurrentUserId();
        if (currentUserId == form.UserId && (!form.IsActive || form.Role != "Admin"))
        {
            ModelState.AddModelError("", "You cannot deactivate or demote your own administrator account.");
            await LoadUserNameAsync(form.UserId, ct);
            return View(form);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.FromSqlInterpolated(
            $"SELECT * FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = {form.UserId}")
            .SingleOrDefaultAsync(ct);
        if (user is null)
            return NotFound();

        if (user.IsActive && user.Role == "Admin" &&
            (!form.IsActive || form.Role != "Admin") &&
            await db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive, ct) <= 1)
        {
            ModelState.AddModelError("", "The last active administrator cannot be disabled.");
            ViewBag.UserName = user.UserName;
            return View(form);
        }

        user.Role = form.Role;
        user.IsActive = form.IsActive;
        AddAudit("User.Edit", user.UserId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData["Message"] = "User permissions saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == id, ct);
        if (user is null)
            return NotFound();

        ViewBag.UserName = user.UserName;
        return View(new UserPasswordForm { UserId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserPasswordForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadUserNameAsync(form.UserId, ct);
            return View(form);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var user = await db.Users.FromSqlInterpolated(
            $"SELECT * FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = {form.UserId}")
            .SingleOrDefaultAsync(ct);
        if (user is null)
            return NotFound();

        if (user.AuthVersion == int.MaxValue)
            throw new InvalidOperationException("Session version limit reached.");

        user.PasswordHash = hasher.HashPassword(user, form.Password);
        user.AuthVersion++;
        AddAudit("User.ResetPassword", user.UserId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData["Message"] = "Password reset. Existing sessions have been invalidated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadUserNameAsync(int id, CancellationToken ct)
    {
        ViewBag.UserName = await db.Users.AsNoTracking()
            .Where(u => u.UserId == id).Select(u => u.UserName).SingleOrDefaultAsync(ct);
    }

    private int CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private static bool ValidRole(string? role) => role is "Admin" or "Cashier";

    private void AddAudit(string action, int userId)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = CurrentUserId(),
            Action = action,
            EntityName = "User",
            EntityId = userId
        });
    }
}
