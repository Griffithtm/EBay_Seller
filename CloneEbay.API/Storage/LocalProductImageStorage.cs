using CloneEbay.Application.Common;

namespace CloneEbay.API.Storage;

public sealed class LocalProductImageStorage(IWebHostEnvironment environment) : IProductImageStorage
{
    private const long MaximumFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<IReadOnlyList<string>> SaveManyAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count is < 1 or > 8)
            throw new AppProblemException(422, "PRODUCT_IMAGES_INVALID", "Upload between 1 and 8 product images.");

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            if (file.Length is <= 0 or > MaximumFileSize)
                throw new AppProblemException(422, "PRODUCT_IMAGE_SIZE_INVALID", "Each product image must be between 1 byte and 5 MB.");
            if (!AllowedExtensions.Contains(extension))
                throw new AppProblemException(422, "PRODUCT_IMAGE_TYPE_INVALID", "Product images must be JPG, PNG, or WEBP.");
            if (!await HasValidSignatureAsync(file, extension, cancellationToken))
                throw new AppProblemException(422, "PRODUCT_IMAGE_CONTENT_INVALID", "An uploaded file does not contain a valid image.");
        }

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(directory);
        var urls = new List<string>(files.Count);
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = File.Create(Path.Combine(directory, fileName));
            await file.CopyToAsync(stream, cancellationToken);
            urls.Add($"/uploads/products/{fileName}");
        }
        return urls;
    }

    public Task DeleteManyAsync(IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "products"));
        foreach (var imageUrl in imageUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(imageUrl);
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            var candidate = Path.GetFullPath(Path.Combine(uploadRoot, fileName));
            if (!candidate.StartsWith(uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                if (File.Exists(candidate)) File.Delete(candidate);
            }
            catch (IOException)
            {
                // Cleanup is best effort; the database operation must remain authoritative.
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup is best effort; operators can reconcile orphaned uploads later.
            }
        }
        return Task.CompletedTask;
    }

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header, cancellationToken);
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
