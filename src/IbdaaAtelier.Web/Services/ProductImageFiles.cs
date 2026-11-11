using System.Globalization;

namespace IbdaaAtelier.Web.Services;

public sealed class ProductImageFiles(IWebHostEnvironment environment)
{
    public const int MaxFileBytes = 5 * 1024 * 1024;

    public async Task<(byte[] Content, string Extension)> ReadAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxFileBytes)
            throw new ArgumentException("Choose an image smaller than 5 MB.");

        await using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        if (buffer.Length is <= 0 or > MaxFileBytes)
            throw new ArgumentException("Invalid image size.");

        var bytes = buffer.ToArray();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var mime = file.ContentType?.ToLowerInvariant() ?? "";

        var jpeg = bytes.Length > 4 && bytes[0] == 0xff && bytes[1] == 0xd8 &&
                   bytes[2] == 0xff && (extension is ".jpg" or ".jpeg") && mime == "image/jpeg";
        var png = bytes.Length > 24 && bytes.AsSpan(0, 8).SequenceEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            extension == ".png" && mime == "image/png";
        var webp = bytes.Length > 16 &&
                   bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                   bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) &&
                   extension == ".webp" && mime == "image/webp";

        if (!jpeg && !png && !webp)
            throw new ArgumentException("Only supported JPEG, PNG or WebP image headers are accepted.");

        if (png)
        {
            var width = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
            var height = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
            if (width is 0 or > 8000 || height is 0 or > 8000)
                throw new ArgumentException("Image dimensions exceed 8000 pixels.");
        }

        return (bytes, jpeg ? ".jpg" : extension);
    }

    public async Task<(string PublicPath, string FullPath)> WriteAsync(
        int productId, int imageId, byte[] bytes, string extension, CancellationToken ct)
    {
        var folder = Folder(productId);
        Directory.CreateDirectory(folder);
        var fileName = imageId.ToString(CultureInfo.InvariantCulture) + "-" +
            DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + extension;
        var path = Path.Combine(folder, fileName);

        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true);
            await output.WriteAsync(bytes, ct);
        }
        catch
        {
            if (File.Exists(path))
                File.Delete(path);
            throw;
        }

        return ($"/uploads/products/{productId}/{fileName}", path);
    }

    public void Delete(int productId, string? publicPath)
    {
        var prefix = $"/uploads/products/{productId}/";
        if (string.IsNullOrWhiteSpace(publicPath) ||
            !publicPath.StartsWith(prefix, StringComparison.Ordinal))
            return;

        var fileName = publicPath[prefix.Length..];
        if (Path.GetFileName(fileName) != fileName || fileName.Contains('\\'))
            return;

        var path = Path.Combine(Folder(productId), fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string Folder(int productId)
    {
        if (string.IsNullOrEmpty(environment.WebRootPath) || productId <= 0)
            throw new InvalidOperationException("Product image storage is not configured.");

        return Path.Combine(environment.WebRootPath, "uploads", "products",
            productId.ToString(CultureInfo.InvariantCulture));
    }
}
