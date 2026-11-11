using System.Security.Claims;
using System.Globalization;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

public sealed class AuthController(AtelierDbContext db, IPasswordHasher<ShopUser> hasher) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl :
                Url.Action("Index", User.IsInRole("Admin") ? "Admin" : "Pos")!);

        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        var user = await db.Users.SingleOrDefaultAsync(
            item => item.UserName == form.UserName.Trim(), ct);

        var now = DateTime.UtcNow;
        var verified = user is not null &&
            user.IsActive &&
            (user.LockedUntilUtc is null || user.LockedUntilUtc <= now) &&
            hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password) != PasswordVerificationResult.Failed;

        if (!verified)
        {
            if (user is { IsActive: true } &&
                (user.LockedUntilUtc is null || user.LockedUntilUtc <= now))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= 5)
                {
                    user.FailedLoginCount = 0;
                    user.LockedUntilUtc = now.AddMinutes(15);
                }
                await db.SaveChangesAsync(ct);
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(form);
        }

        user!.FailedLoginCount = 0;
        user.LockedUntilUtc = null;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = user.UserId,
            Action = "User.Login",
            EntityName = "User",
            EntityId = user.UserId
        });
        await db.SaveChangesAsync(ct);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("auth_version", user.AuthVersion.ToString(CultureInfo.InvariantCulture))
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ?
            form.ReturnUrl : Url.Action("Index", user.Role == "Admin" ? "Admin" : "Pos")!);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "User.Logout",
                EntityName = "User",
                EntityId = userId
            });
            await db.SaveChangesAsync(ct);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Authorize]
    public IActionResult Denied() => View();
}
