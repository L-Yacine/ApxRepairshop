using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public sealed class HeroSlidesController : Controller
{
    public const long MaxImageBytes = 3 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp"
    };

    private const string ImagePublicFolder = "uploads/hero";
    private const int ImageMaxWidth = 1600;
    private const int ImageMaxHeight = 900;
    private const int ImageQuality = 82;

    private readonly HeroSlideService slideService;
    private readonly IWebHostEnvironment environment;

    public HeroSlidesController(HeroSlideService slideService, IWebHostEnvironment environment)
    {
        this.slideService = slideService;
        this.environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<HeroSlide> slides = await slideService.ListAllAsync();
        ViewData["Title"] = "شرائح الواجهة";
        return View(slides);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "إضافة شريحة";
        return View(new HeroSlideFormInput { IsActive = true, SortOrder = 10 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageBytes + 1024)]
    public async Task<IActionResult> Create(HeroSlideFormInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        string? imageUrl = await ResolveImageAsync(input, existingImageUrl: null);
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await slideService.CreateAsync(new HeroSlide
        {
            Title = input.Title.Trim(),
            Subtitle = input.Subtitle.Trim(),
            ImageUrl = imageUrl ?? string.Empty,
            CtaText = input.CtaText.Trim(),
            CtaUrl = input.CtaUrl.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });

        TempData["HeroSlidesMessage"] = "تمت إضافة الشريحة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        HeroSlide? slide = await slideService.FindAsync(id);
        if (slide is null) { return NotFound(); }

        var input = new HeroSlideFormInput
        {
            Id = slide.Id,
            Title = slide.Title,
            Subtitle = slide.Subtitle,
            ExistingImageUrl = slide.ImageUrl,
            CtaText = slide.CtaText,
            CtaUrl = slide.CtaUrl,
            SortOrder = slide.SortOrder,
            IsActive = slide.IsActive
        };
        ViewData["Title"] = $"تعديل الشريحة — {slide.Title}";
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageBytes + 1024)]
    public async Task<IActionResult> Edit(int id, HeroSlideFormInput input)
    {
        HeroSlide? existing = await slideService.FindAsync(id);
        if (existing is null) { return NotFound(); }

        if (!ModelState.IsValid)
        {
            input.ExistingImageUrl = existing.ImageUrl;
            return View(input);
        }

        string? imageUrl = await ResolveImageAsync(input, existing.ImageUrl);
        if (!ModelState.IsValid)
        {
            input.ExistingImageUrl = existing.ImageUrl;
            return View(input);
        }

        bool ok = await slideService.UpdateAsync(new HeroSlide
        {
            Id = id,
            Title = input.Title.Trim(),
            Subtitle = input.Subtitle.Trim(),
            ImageUrl = imageUrl ?? string.Empty,
            CtaText = input.CtaText.Trim(),
            CtaUrl = input.CtaUrl.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });

        if (!ok) { return NotFound(); }

        TempData["HeroSlidesMessage"] = "تم تحديث الشريحة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        await slideService.ToggleAsync(id);
        TempData["HeroSlidesMessage"] = "تم تحديث حالة الشريحة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        HeroSlide? slide = await slideService.FindAsync(id);
        if (slide is null) { return NotFound(); }

        await slideService.DeleteAsync(id);
        DeleteImageFile(slide.ImageUrl);
        TempData["HeroSlidesMessage"] = "تم حذف الشريحة.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> ResolveImageAsync(HeroSlideFormInput input, string? existingImageUrl)
    {
        if (input.RemoveImage && input.ImageFile is { Length: > 0 })
        {
            ModelState.AddModelError(nameof(input.ImageFile), "لا يمكنك رفع صورة جديدة وطلب إزالة الصورة في آن واحد.");
            return null;
        }

        if (input.RemoveImage)
        {
            DeleteImageFile(existingImageUrl);
            return null;
        }

        if (input.ImageFile is { Length: > 0 } file)
        {
            string? uploadedUrl = await TrySaveImageAsync(file);
            if (uploadedUrl is null)
            {
                return null;
            }

            DeleteImageFile(existingImageUrl, uploadedUrl);
            return uploadedUrl;
        }

        return existingImageUrl;
    }

    private async Task<string?> TrySaveImageAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(HeroSlideFormInput.ImageFile), "الملف المختار فارغ.");
            return null;
        }

        if (file.Length > MaxImageBytes)
        {
            ModelState.AddModelError(nameof(HeroSlideFormInput.ImageFile), "حجم الصورة كبير جداً. الحد الأقصى هو 3 ميغابايت.");
            return null;
        }

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(HeroSlideFormInput.ImageFile), "صيغة غير مدعومة. استخدم PNG أو JPG أو WEBP.");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError(nameof(HeroSlideFormInput.ImageFile), "نوع المحتوى غير مدعوم.");
            return null;
        }

        string folder = Path.Combine(environment.WebRootPath, ImagePublicFolder);
        Directory.CreateDirectory(folder);
        string fullPath = Path.Combine(folder, $"hero-{Guid.NewGuid():N}.webp");

        try
        {
            await using Stream input = file.OpenReadStream();
            using var image = new MagickImage(input);
            image.AutoOrient();
            image.Strip();
            image.Resize(new MagickGeometry(ImageMaxWidth, ImageMaxHeight)
            {
                IgnoreAspectRatio = false,
                Greater = true
            });
            image.Format = MagickFormat.WebP;
            image.Quality = ImageQuality;
            await image.WriteAsync(fullPath);
        }
        catch (MagickException)
        {
            ModelState.AddModelError(nameof(HeroSlideFormInput.ImageFile), "تعذرت معالجة الصورة. جرّب صورة أخرى أو صيغة مختلفة.");
            return null;
        }

        string relative = Path.GetRelativePath(environment.WebRootPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/');
        return "/" + relative;
    }

    private void DeleteImageFile(string? imageUrl, string? excludeUrl = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)
            || !imageUrl.StartsWith($"/{ImagePublicFolder}/", StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(imageUrl, excludeUrl, StringComparison.Ordinal))
        {
            return;
        }

        string relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(environment.WebRootPath, relative);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
