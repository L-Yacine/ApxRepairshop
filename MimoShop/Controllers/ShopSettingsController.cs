using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Controllers;

public sealed class ShopSettingsController : Controller
{
    public const long MaxLogoBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp"
    };

    private const string LogoPublicFolder = "images";
    private const string LogoFileNamePrefix = "logo";
    private const string LogoFileName = LogoFileNamePrefix + ".webp";
    private const int LogoMaxDimension = 300;
    private const int LogoQuality = 88;

    private readonly ShopSettingsService shopSettingsService;
    private readonly IWebHostEnvironment environment;

    public ShopSettingsController(ShopSettingsService shopSettingsService, IWebHostEnvironment environment)
    {
        this.shopSettingsService = shopSettingsService;
        this.environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> SettingsModal()
    {
        ShopSetting settings = await shopSettingsService.GetSettingsAsync();
        return PartialView("_SettingsModalPartial", ToViewModel(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxLogoBytes + 1024)]
    public async Task<IActionResult> Index(ShopSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ReturnSettingsAsync(model);
        }

        string? currentLogoUrl = (await shopSettingsService.GetSettingsAsync()).LogoUrl;
        string? resolvedLogoUrl = currentLogoUrl;

        if (model.RemoveLogo && model.LogoFile is { Length: > 0 })
        {
            ModelState.AddModelError(nameof(model.LogoFile), "لا يمكنك رفع شعار جديد وعلامه إزالة الشعار في آن واحد.");
            model.ExistingLogoUrl = currentLogoUrl;
            return ReturnSettingsAsync(model);
        }

        if (model.RemoveLogo)
        {
            DeleteLogoFile(currentLogoUrl);
            resolvedLogoUrl = null;
        }
        else if (model.LogoFile is { Length: > 0 } file)
        {
            string? uploadedUrl = await TrySaveLogoAsync(file);
            if (uploadedUrl is null)
            {
                model.ExistingLogoUrl = currentLogoUrl;
                return ReturnSettingsAsync(model);
            }

            DeleteLogoFile(currentLogoUrl, uploadedUrl);
            resolvedLogoUrl = uploadedUrl;
        }
        else
        {
            model.ExistingLogoUrl = currentLogoUrl;
        }

        await shopSettingsService.UpdateSettingsAsync(ToEntity(model, resolvedLogoUrl));

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم حفظ الإعدادات." });
        }
        TempData["SettingsMessage"] = "تم حفظ الإعدادات.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ReturnSettingsAsync(ShopSettingsViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_SettingsModalPartial", model);
    }

    private static ShopSettingsViewModel ToViewModel(ShopSetting settings)
    {
        return new ShopSettingsViewModel
        {
            Id = settings.Id,
            Name = settings.Name,
            LatinName = settings.LatinName,
            Phone = settings.Phone,
            WhatsApp = settings.WhatsApp,
            Address = settings.Address,
            TelegramHandle = settings.TelegramHandle,
            OpeningHours = settings.OpeningHours,
            ExistingLogoUrl = settings.LogoUrl
        };
    }

    private static ShopSetting ToEntity(ShopSettingsViewModel model, string? logoUrl)
    {
        return new ShopSetting
        {
            Id = model.Id == 0 ? 1 : model.Id,
            Name = model.Name,
            LatinName = model.LatinName,
            Phone = model.Phone,
            WhatsApp = model.WhatsApp,
            Address = model.Address,
            TelegramHandle = model.TelegramHandle,
            OpeningHours = model.OpeningHours,
            LogoUrl = logoUrl
        };
    }

    private async Task<string?> TrySaveLogoAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(ShopSettingsViewModel.LogoFile), "الملف المختار فارغ.");
            return null;
        }

        if (file.Length > MaxLogoBytes)
        {
            ModelState.AddModelError(nameof(ShopSettingsViewModel.LogoFile), "حجم الشعار كبير جداً. الحد الأقصى هو 2 ميغابايت.");
            return null;
        }

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedLogoExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(ShopSettingsViewModel.LogoFile), "صيغة غير مدعومة. استخدم PNG أو JPG أو WEBP.");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !AllowedLogoContentTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError(nameof(ShopSettingsViewModel.LogoFile), "نوع المحتوى غير مدعوم.");
            return null;
        }

        string folder = Path.Combine(environment.WebRootPath, LogoPublicFolder);
        Directory.CreateDirectory(folder);
        string fullPath = Path.Combine(folder, LogoFileName);

        try
        {
            await using Stream input = file.OpenReadStream();
            using var image = new MagickImage(input);
            image.AutoOrient();
            image.Strip();
            image.Resize(new MagickGeometry(LogoMaxDimension, LogoMaxDimension)
            {
                IgnoreAspectRatio = false,
                Greater = true
            });
            image.Format = MagickFormat.WebP;
            image.Quality = LogoQuality;
            await image.WriteAsync(fullPath);
        }
        catch (MagickException)
        {
            ModelState.AddModelError(nameof(ShopSettingsViewModel.LogoFile), "تعذرت معالجة الشعار. جرّب صورة أخرى أو صيغة مختلفة.");
            return null;
        }

        return $"/{LogoPublicFolder}/{LogoFileName}";
    }

    private void DeleteLogoFile(string? logoUrl, string? excludeUrl = null)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)
            || !logoUrl.StartsWith($"/{LogoPublicFolder}/", StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(logoUrl, excludeUrl, StringComparison.Ordinal))
        {
            return;
        }

        string relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(environment.WebRootPath, relative);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the database still references the new logo.
            }
        }
    }
}