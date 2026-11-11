using System.Globalization;
using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AtelierDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AtelierDb")));
builder.Services.AddScoped<IPasswordHasher<ShopUser>, PasswordHasher<ShopUser>>();
builder.Services.AddScoped<OrderLineBuilder>();
builder.Services.AddScoped<PosOrderService>();
builder.Services.AddScoped<ProductImageFiles>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<OrderStatusService>();
builder.Services.AddScoped<InvoiceReportService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Denied";
        options.Cookie.Name = "IbdaaAtelier.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var identity = context.Principal;
            if (identity is null ||
                !int.TryParse(identity.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                userId <= 0)
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AtelierDbContext>();
            var user = await db.Users.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.IsActive, item.Role, item.UserName, item.AuthVersion })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (user is null || !user.IsActive ||
                user.Role != identity.FindFirstValue(ClaimTypes.Role) ||
                user.UserName != identity.FindFirstValue(ClaimTypes.Name) ||
                user.AuthVersion.ToString(CultureInfo.InvariantCulture) != identity.FindFirstValue("auth_version"))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase))
{
    var userName = app.Configuration["Bootstrap:AdminUser"]?.Trim();
    var password = app.Configuration["Bootstrap:AdminPassword"];

    if (string.IsNullOrWhiteSpace(userName) || userName.Length > 60 ||
        string.IsNullOrWhiteSpace(password) || password.Length < 12)
        throw new InvalidOperationException("Configure Bootstrap:AdminUser and a password of at least 12 characters using local user-secrets.");

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();
    if (await db.Users.AnyAsync())
        throw new InvalidOperationException("User records already exist. The one-time admin bootstrap is disabled.");

    var admin = new ShopUser { UserName = userName, Role = "Admin" };
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ShopUser>>();
    admin.PasswordHash = hasher.HashPassword(admin, password);
    db.Users.Add(admin);
    await db.SaveChangesAsync();
    Console.WriteLine("Initial administrator created. Remove the bootstrap password from user-secrets.");
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var cultures = new[] { new CultureInfo("en-US"), new CultureInfo("ar-KW") };
var localization = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultures[0]),
    SupportedCultures = cultures,
    SupportedUICultures = cultures
};
localization.RequestCultureProviders.Clear();
localization.RequestCultureProviders.Add(new CookieRequestCultureProvider());

app.UseRequestLocalization(localization);
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
