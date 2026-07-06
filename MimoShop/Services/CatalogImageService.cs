using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MimoShop.Services;

public sealed class CatalogImageService
{
    public const long MaxInputBytes = 8 * 1024 * 1024;
    private const int MaxImageDimension = 900;
    private const int MaxThumbnailDimension = 240;
    private const int ImageQuality = 82;
    private const int ThumbnailQuality = 78;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".heic",
        ".heif"
    };

    private readonly IWebHostEnvironment environment;
    private readonly ILogger<CatalogImageService> logger;

    public CatalogImageService(IWebHostEnvironment environment, ILogger<CatalogImageService> logger)
    {
        this.environment = environment;
        this.logger = logger;
    }

    public Task<CatalogImageResult?> SaveAsync(IFormFile? file, string entityType, string displayName)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<CatalogImageResult?>(null);
        }

        if (file.Length > MaxInputBytes)
        {
            throw new CatalogImageException("حجم الصورة كبير جداً. الحد الأقصى هو 8 ميغابايت.");
        }

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new CatalogImageException("صيغة الصورة غير مدعومة. استخدم JPG أو PNG أو WEBP أو HEIC.");
        }

        using Stream stream = file.OpenReadStream();
        CatalogImageResult result = SaveProcessedImage(stream, file.FileName, entityType, displayName);
        return Task.FromResult<CatalogImageResult?>(result);
    }

    public Task<CatalogImageResult> SaveAsync(Stream imageStream, string originalFileName, string entityType, string displayName)
    {
        if (imageStream is null)
        {
            throw new CatalogImageException("تعذر قراءة الصورة. جرّب صورة أخرى أو صيغة مختلفة.");
        }

        string extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension) && !AllowedExtensions.Contains(extension))
        {
            originalFileName = "image.jpg";
        }

        CatalogImageResult result = SaveProcessedImage(imageStream, originalFileName, entityType, displayName);
        return Task.FromResult(result);
    }

    public void DeleteStoredImage(string imageUrl, string thumbnailUrl)
    {
        DeletePublicPath(imageUrl);
        DeletePublicPath(thumbnailUrl);
    }

    private void DeletePublicPath(string publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith("/uploads/catalog/", StringComparison.Ordinal))
        {
            return;
        }

        string relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(environment.WebRootPath, relative);
        DeleteIfExists(fullPath);
    }

    private static void DeleteIfExists(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string ToPublicPath(string fullPath)
    {
        string relative = Path.GetRelativePath(environment.WebRootPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/');
        return "/" + relative;
    }

    private CatalogImageResult SaveProcessedImage(Stream imageStream, string originalFileName, string entityType, string displayName)
    {
        string safeEntityType = Slugify(entityType);
        string safeName = Slugify(displayName);
        string fileStem = $"{safeEntityType}-{safeName}-{Guid.NewGuid():N}";
        string imagesRoot = Path.Combine(environment.WebRootPath, "uploads", "catalog", "images");
        string thumbsRoot = Path.Combine(environment.WebRootPath, "uploads", "catalog", "thumbs");

        Directory.CreateDirectory(imagesRoot);
        Directory.CreateDirectory(thumbsRoot);

        string imagePath = Path.Combine(imagesRoot, fileStem + ".webp");
        string thumbnailPath = Path.Combine(thumbsRoot, fileStem + ".webp");

        try
        {
            using var image = new MagickImage(imageStream);
            image.AutoOrient();
            image.Strip();
            image.Resize(new MagickGeometry(MaxImageDimension, MaxImageDimension)
            {
                IgnoreAspectRatio = false,
                Greater = true
            });
            image.Format = MagickFormat.WebP;
            image.Quality = ImageQuality;
            image.Write(imagePath);

            using var thumbnail = image.Clone();
            thumbnail.Resize(new MagickGeometry(MaxThumbnailDimension, MaxThumbnailDimension)
            {
                IgnoreAspectRatio = false,
                Greater = true
            });
            thumbnail.Quality = ThumbnailQuality;
            thumbnail.Write(thumbnailPath);
        }
        catch (MagickException ex)
        {
            logger.LogWarning(ex, "Catalog image processing failed for {FileName}", originalFileName);
            DeleteIfExists(imagePath);
            DeleteIfExists(thumbnailPath);
            throw new CatalogImageException("تعذر قراءة الصورة. جرّب صورة أخرى أو صيغة مختلفة.", ex);
        }

        return new CatalogImageResult(ToPublicPath(imagePath), ToPublicPath(thumbnailPath));
    }

    private static string Slugify(string value)
    {
        string trimmed = string.IsNullOrWhiteSpace(value) ? "catalog" : value.Trim().ToLowerInvariant();
        Span<char> buffer = stackalloc char[Math.Min(trimmed.Length, 80)];
        int index = 0;

        foreach (char c in trimmed)
        {
            if (index >= buffer.Length)
            {
                break;
            }

            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                buffer[index++] = c;
            }
            else if (index > 0 && buffer[index - 1] != '-')
            {
                buffer[index++] = '-';
            }
        }

        string slug = new(buffer[..index]);
        return string.IsNullOrWhiteSpace(slug) ? "catalog" : slug.Trim('-');
    }
}

public sealed record CatalogImageResult(string ImageUrl, string ThumbnailUrl);

public sealed class CatalogImageException : Exception
{
    public CatalogImageException(string message)
        : base(message)
    {
    }

    public CatalogImageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
