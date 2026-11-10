using System.Globalization;
using IbdaaAtelier.Web.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AtelierDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AtelierDb")));

var app = builder.Build();

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
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
