using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public sealed class CustomersController(AtelierDbContext db) : Controller
{
    private const int PageSize = 25;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            search = search[..100];

        var query = db.Customers.AsNoTracking();
        if (search.Length > 0)
        {
            var mobileSearch = PhoneNumbers.Normalize(search);
            if (long.TryParse(search, out var customerId))
                query = query.Where(c => c.CustomerId == customerId ||
                    (mobileSearch != null && c.Mobile.Contains(mobileSearch)) ||
                    c.Name.Contains(search) || c.NameAr.Contains(search));
            else
                query = query.Where(c =>
                    (mobileSearch != null && c.Mobile.Contains(mobileSearch)) ||
                    c.Name.Contains(search) || c.NameAr.Contains(search));
        }

        var count = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, totalPages);

        return View(new CustomerListPage
        {
            Search = search,
            Page = page,
            TotalPages = totalPages,
            Customers = await query.OrderByDescending(c => c.CustomerId)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct)
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Editor", new CustomerForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerForm form, CancellationToken ct)
    {
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
            return View("Editor", form);

        var customer = new Customer();
        CopyValues(form, customer);
        db.Customers.Add(customer);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit("Customer.Create", customer.CustomerId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not save customer. Mobile number may already exist.");
            return View("Editor", form);
        }

        TempData["Message"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == id, ct);
        if (customer is null)
            return NotFound();

        return View("Editor", new CustomerForm
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            NameAr = customer.NameAr,
            Mobile = customer.Mobile,
            AlternateMobile = customer.AlternateMobile,
            Address = customer.Address,
            Notes = customer.Notes
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerForm form, CancellationToken ct)
    {
        if (form.CustomerId <= 0)
            return BadRequest();

        var customer = await db.Customers.SingleOrDefaultAsync(
            c => c.CustomerId == form.CustomerId, ct);
        if (customer is null)
            return NotFound();

        await ValidateAsync(form, ct);
        if (!ModelState.IsValid)
            return View("Editor", form);

        CopyValues(form, customer);
        AddAudit("Customer.Edit", customer.CustomerId);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Could not update customer. Mobile number may already exist.");
            return View("Editor", form);
        }

        TempData["Message"] = "Customer updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(CustomerForm form, CancellationToken ct)
    {
        form.Name = form.Name?.Trim() ?? "";
        form.NameAr = form.NameAr?.Trim() ?? "";
        form.Address = form.Address?.Trim();
        form.Notes = form.Notes?.Trim();

        var mobile = PhoneNumbers.Normalize(form.Mobile);
        var alternate = string.IsNullOrWhiteSpace(form.AlternateMobile) ?
            null : PhoneNumbers.Normalize(form.AlternateMobile);

        if (form.Name.Length == 0)
            ModelState.AddModelError(nameof(form.Name), "English name is required.");
        if (form.NameAr.Length == 0)
            ModelState.AddModelError(nameof(form.NameAr), "Arabic name is required.");
        if (mobile is null)
            ModelState.AddModelError(nameof(form.Mobile), "Enter a valid mobile number (8–15 digits).");
        if (!string.IsNullOrWhiteSpace(form.AlternateMobile) && alternate is null)
            ModelState.AddModelError(nameof(form.AlternateMobile), "Enter a valid alternate mobile.");
        if (mobile is not null && await db.Customers.AnyAsync(c =>
            c.Mobile == mobile && c.CustomerId != form.CustomerId, ct))
            ModelState.AddModelError(nameof(form.Mobile), "This mobile number belongs to another customer.");

        if (mobile is not null)
            form.Mobile = mobile;
        form.AlternateMobile = alternate;
    }

    private static void CopyValues(CustomerForm form, Customer customer)
    {
        customer.Name = form.Name;
        customer.NameAr = form.NameAr;
        customer.Mobile = form.Mobile;
        customer.AlternateMobile = form.AlternateMobile;
        customer.Address = form.Address;
        customer.Notes = form.Notes;
    }

    private void AddAudit(string action, long id)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = "Customer",
            EntityId = id
        });
    }
}
