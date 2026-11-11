using System.Data;
using System.Security.Claims;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ProductImagesController(
    AtelierDbContext db, ProductImageFiles files, ILogger<ProductImagesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int productId, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product is null)
            return NotFound();

        var images = await db.ProductImages.AsNoTracking()
            .Where(image => image.ProductId == productId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.ProductImageId)
            .ToListAsync(ct);

        return View(new ProductGalleryPage { Product = product, Images = images });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int productId, IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return Fail(productId, "Choose a JPEG, PNG or WebP image.");

        byte[] bytes;
        string extension;
        try
        {
            (bytes, extension) = await files.ReadAsync(file, ct);
        }
        catch (ArgumentException exception)
        {
            return Fail(productId, exception.Message);
        }

        string? uploadedPath = null;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        try
        {
            // Serialize uploads for a product so only the first photograph is primary.
            var product = await db.Products.FromSqlInterpolated(
                $"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE ProductId = {productId}")
                .SingleOrDefaultAsync(ct);
            if (product is null)
                return NotFound();

            var isFirst = !await db.ProductImages.AnyAsync(image =>
                image.ProductId == productId, ct);
            var image = new ProductImage
            {
                ProductId = productId,
                ImagePath = "pending",
                IsPrimary = isFirst,
                DisplayOrder = await db.ProductImages.CountAsync(i => i.ProductId == productId, ct)
            };

            db.ProductImages.Add(image);
            await db.SaveChangesAsync(ct);

            var saved = await files.WriteAsync(productId, image.ProductImageId, bytes, extension, ct);
            uploadedPath = saved.PublicPath;
            image.ImagePath = saved.PublicPath;
            AddAudit("ProductImage.Upload", image.ProductImageId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception exception) when (exception is DbUpdateException or IOException or UnauthorizedAccessException)
        {
            if (uploadedPath is not null)
                TryDelete(productId, uploadedPath);
            logger.LogWarning(exception, "Product image upload failed for product {ProductId}", productId);
            return Fail(productId, "Could not save the photograph. Check storage and retry.");
        }

        TempData["Message"] = "Photograph uploaded.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Replace(int productId, int imageId, IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return Fail(productId, "Choose a replacement image.");

        byte[] bytes;
        string extension;
        try
        {
            (bytes, extension) = await files.ReadAsync(file, ct);
        }
        catch (ArgumentException exception)
        {
            return Fail(productId, exception.Message);
        }

        string? newPath = null;
        string? oldPath = null;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        try
        {
            var image = await db.ProductImages.FromSqlInterpolated(
                $"SELECT * FROM dbo.ProductImages WITH (UPDLOCK, HOLDLOCK) WHERE ProductImageId = {imageId} AND ProductId = {productId}")
                .SingleOrDefaultAsync(ct);
            if (image is null)
                return NotFound();

            oldPath = image.ImagePath;
            var saved = await files.WriteAsync(productId, imageId, bytes, extension, ct);
            newPath = saved.PublicPath;
            image.ImagePath = newPath;

            AddAudit("ProductImage.Replace", imageId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception exception) when (exception is DbUpdateException or IOException or UnauthorizedAccessException)
        {
            if (newPath is not null)
                TryDelete(productId, newPath);
            logger.LogWarning(exception, "Image replacement failed for {ImageId}", imageId);
            return Fail(productId, "Could not replace the photograph.");
        }

        TryDelete(productId, oldPath);
        TempData["Message"] = "Photograph replaced.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MakePrimary(int productId, int imageId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var image = await db.ProductImages.FromSqlInterpolated(
            $"SELECT * FROM dbo.ProductImages WITH (UPDLOCK, HOLDLOCK) WHERE ProductImageId = {imageId} AND ProductId = {productId}")
            .SingleOrDefaultAsync(ct);
        if (image is null)
            return NotFound();
        if (image.IsPrimary)
            return RedirectToAction(nameof(Index), new { productId });

        await db.ProductImages.Where(i => i.ProductId == productId && i.IsPrimary)
            .ExecuteUpdateAsync(update => update.SetProperty(i => i.IsPrimary, false), ct);
        image.IsPrimary = true;
        AddAudit("ProductImage.MakePrimary", imageId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["Message"] = "Primary photograph updated.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int productId, int imageId, CancellationToken ct)
    {
        string? imagePath;
        await using (var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct))
        {
            var image = await db.ProductImages.FromSqlInterpolated(
                $"SELECT * FROM dbo.ProductImages WITH (UPDLOCK, HOLDLOCK) WHERE ProductImageId = {imageId} AND ProductId = {productId}")
                .SingleOrDefaultAsync(ct);
            if (image is null)
                return NotFound();

            imagePath = image.ImagePath;
            db.ProductImages.Remove(image);
            await db.SaveChangesAsync(ct);

            if (image.IsPrimary)
            {
                var next = await db.ProductImages.Where(i => i.ProductId == productId)
                    .OrderBy(i => i.DisplayOrder).ThenBy(i => i.ProductImageId)
                    .FirstOrDefaultAsync(ct);
                if (next is not null)
                    next.IsPrimary = true;
            }

            AddAudit("ProductImage.Delete", imageId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        TryDelete(productId, imagePath);
        TempData["Message"] = "Photograph removed.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    private IActionResult Fail(int productId, string error)
    {
        TempData["Error"] = error;
        return RedirectToAction(nameof(Index), new { productId });
    }

    private void TryDelete(int productId, string? path)
    {
        try { files.Delete(productId, path); }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not remove orphaned product image for {ProductId}", productId);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Cannot delete image for {ProductId}", productId);
        }
    }

    private void AddAudit(string action, int imageId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId > 0 ? userId : null,
            Action = action,
            EntityName = "ProductImage",
            EntityId = imageId
        });
    }
}
