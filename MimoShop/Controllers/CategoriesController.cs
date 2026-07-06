using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Controllers;

public sealed class CategoriesController : Controller
{
    private readonly MimoShopDbContext dbContext;
    private readonly SeedImportService seedImportService;
    private readonly CatalogImageService catalogImageService;
    private readonly CatalogImageFetchService catalogImageFetchService;

    public CategoriesController(
        MimoShopDbContext dbContext,
        SeedImportService seedImportService,
        CatalogImageService catalogImageService,
        CatalogImageFetchService catalogImageFetchService)
    {
        this.dbContext = dbContext;
        this.seedImportService = seedImportService;
        this.catalogImageService = catalogImageService;
        this.catalogImageFetchService = catalogImageFetchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        CategoriesIndexViewModel model = await BuildIndexModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand(CategoriesIndexViewModel model)
    {
        BrandFormInput input = model.NewBrand;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية للعلامة.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.Brands.AnyAsync(brand => brand.Name == normalizedName))
        {
            TempData["CategoriesError"] = "هذه العلامة موجودة من قبل.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "brand", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        dbContext.Brands.Add(new Brand
        {
            Name = normalizedName,
            DisplayNameAr = input.DisplayNameAr.Trim(),
            ImageUrl = image?.ImageUrl ?? string.Empty,
            ThumbnailUrl = image?.ThumbnailUrl ?? string.Empty,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تمت إضافة العلامة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBrand(int id, CategoriesIndexViewModel model)
    {
        Brand? brand = await dbContext.Brands.SingleOrDefaultAsync(brand => brand.Id == id);
        if (brand is null)
        {
            return NotFound();
        }

        BrandFormInput input = model.NewBrand;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية للعلامة.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.Brands.AnyAsync(candidate => candidate.Name == normalizedName && candidate.Id != id))
        {
            TempData["CategoriesError"] = "اسم العلامة مستخدم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "brand", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        if (image is not null)
        {
            catalogImageService.DeleteStoredImage(brand.ImageUrl, brand.ThumbnailUrl);
            brand.ImageUrl = image.ImageUrl;
            brand.ThumbnailUrl = image.ThumbnailUrl;
        }

        brand.Name = normalizedName;
        brand.DisplayNameAr = input.DisplayNameAr.Trim();
        brand.SortOrder = input.SortOrder;
        brand.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تم تحديث العلامة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBrandActive(int id, bool isActive)
    {
        Brand? brand = await dbContext.Brands.SingleOrDefaultAsync(brand => brand.Id == id);
        if (brand is null)
        {
            return NotFound();
        }

        brand.IsActive = isActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = isActive ? "تم تفعيل العلامة." : "تم تعطيل العلامة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePhoneModel(CategoriesIndexViewModel model)
    {
        PhoneModelFormInput input = model.NewPhoneModel;
        if (input.BrandId <= 0
            || string.IsNullOrWhiteSpace(input.Name)
            || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "اختر العلامة وأدخل الاسم والاسم بالعربية للموديل.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PhoneModels.AnyAsync(model => model.BrandId == input.BrandId && model.Name == normalizedName))
        {
            TempData["CategoriesError"] = "هذا الموديل مسجل من قبل لهذه العلامة.";
            return RedirectToAction(nameof(Index));
        }

        if (!await dbContext.Brands.AnyAsync(brand => brand.Id == input.BrandId))
        {
            TempData["CategoriesError"] = "العلامة غير موجودة.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "model", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        dbContext.PhoneModels.Add(new PhoneModel
        {
            BrandId = input.BrandId,
            Name = normalizedName,
            DisplayNameAr = input.DisplayNameAr.Trim(),
            ImageUrl = image?.ImageUrl ?? string.Empty,
            ThumbnailUrl = image?.ThumbnailUrl ?? string.Empty,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تمت إضافة الموديل.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhoneModel(int id, CategoriesIndexViewModel model)
    {
        PhoneModel? phoneModel = await dbContext.PhoneModels.SingleOrDefaultAsync(model => model.Id == id);
        if (phoneModel is null)
        {
            return NotFound();
        }

        PhoneModelFormInput input = model.NewPhoneModel;
        if (input.BrandId <= 0
            || string.IsNullOrWhiteSpace(input.Name)
            || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "اختر العلامة وأدخل الاسم والاسم بالعربية للموديل.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PhoneModels.AnyAsync(candidate => candidate.BrandId == input.BrandId
                && candidate.Name == normalizedName
                && candidate.Id != id))
        {
            TempData["CategoriesError"] = "اسم الموديل مستخدم بالفعل لهذه العلامة.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "model", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        if (image is not null)
        {
            catalogImageService.DeleteStoredImage(phoneModel.ImageUrl, phoneModel.ThumbnailUrl);
            phoneModel.ImageUrl = image.ImageUrl;
            phoneModel.ThumbnailUrl = image.ThumbnailUrl;
        }

        phoneModel.BrandId = input.BrandId;
        phoneModel.Name = normalizedName;
        phoneModel.DisplayNameAr = input.DisplayNameAr.Trim();
        phoneModel.SortOrder = input.SortOrder;
        phoneModel.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تم تحديث الموديل.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPhoneModelActive(int id, bool isActive)
    {
        PhoneModel? phoneModel = await dbContext.PhoneModels.SingleOrDefaultAsync(model => model.Id == id);
        if (phoneModel is null)
        {
            return NotFound();
        }

        phoneModel.IsActive = isActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = isActive ? "تم تفعيل الموديل." : "تم تعطيل الموديل.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SearchPhoneModelImages(int id, string? query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CatalogImageCandidate> candidates =
            await catalogImageFetchService.SearchPhoneModelImagesAsync(id, query, cancellationToken);

        return Json(candidates);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPhoneModelImage(int id, string sourceUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { message = "اختر صورة أولاً." });
        }

        try
        {
            AppliedCatalogImageResult? result =
                await catalogImageFetchService.ApplyPhoneModelImageAsync(id, sourceUrl, cancellationToken);

            if (result is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { message = "الموديل غير موجود." });
            }

            return Json(new
            {
                message = "تم حفظ صورة الموديل.",
                result.ImageUrl,
                result.ThumbnailUrl
            });
        }
        catch (CatalogImageException ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            return Json(new { message = "تعذر الاتصال بمصدر الصورة. جرّب مرة أخرى." });
        }
        catch (TaskCanceledException)
        {
            Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            return Json(new { message = "انتهت مهلة جلب الصورة. جرّب مرة أخرى." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePartType(CategoriesIndexViewModel model)
    {
        PartTypeFormInput input = model.NewPartType;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية لنوع القطعة.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PartTypes.AnyAsync(type => type.Name == normalizedName))
        {
            TempData["CategoriesError"] = "هذا النوع مسجل من قبل.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "parttype", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        dbContext.PartTypes.Add(new PartType
        {
            Name = normalizedName,
            DisplayNameAr = input.DisplayNameAr.Trim(),
            ImageUrl = image?.ImageUrl ?? string.Empty,
            ThumbnailUrl = image?.ThumbnailUrl ?? string.Empty,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تمت إضافة نوع القطعة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePartType(int id, CategoriesIndexViewModel model)
    {
        PartType? partType = await dbContext.PartTypes.SingleOrDefaultAsync(type => type.Id == id);
        if (partType is null)
        {
            return NotFound();
        }

        PartTypeFormInput input = model.NewPartType;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية لنوع القطعة.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PartTypes.AnyAsync(candidate => candidate.Name == normalizedName && candidate.Id != id))
        {
            TempData["CategoriesError"] = "اسم النوع مستخدم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        CatalogImageResult? image;
        try
        {
            image = await catalogImageService.SaveAsync(input.ImageFile, "parttype", normalizedName);
        }
        catch (CatalogImageException ex)
        {
            TempData["CategoriesError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        if (image is not null)
        {
            catalogImageService.DeleteStoredImage(partType.ImageUrl, partType.ThumbnailUrl);
            partType.ImageUrl = image.ImageUrl;
            partType.ThumbnailUrl = image.ThumbnailUrl;
        }

        partType.Name = normalizedName;
        partType.DisplayNameAr = input.DisplayNameAr.Trim();
        partType.SortOrder = input.SortOrder;
        partType.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تم تحديث نوع القطعة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPartTypeActive(int id, bool isActive)
    {
        PartType? partType = await dbContext.PartTypes.SingleOrDefaultAsync(type => type.Id == id);
        if (partType is null)
        {
            return NotFound();
        }

        partType.IsActive = isActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = isActive ? "تم تفعيل نوع القطعة." : "تم تعطيل نوع القطعة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePartVariant(CategoriesIndexViewModel model)
    {
        PartVariantFormInput input = model.NewPartVariant;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية للنوعية.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PartVariants.AnyAsync(variant => variant.Name == normalizedName))
        {
            TempData["CategoriesError"] = "هذه النوعية مسجلة من قبل.";
            return RedirectToAction(nameof(Index));
        }

        dbContext.PartVariants.Add(new PartVariant
        {
            Name = normalizedName,
            DisplayNameAr = input.DisplayNameAr.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تمت إضافة النوعية.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePartVariant(int id, CategoriesIndexViewModel model)
    {
        PartVariant? partVariant = await dbContext.PartVariants.SingleOrDefaultAsync(variant => variant.Id == id);
        if (partVariant is null)
        {
            return NotFound();
        }

        PartVariantFormInput input = model.NewPartVariant;
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.DisplayNameAr))
        {
            TempData["CategoriesError"] = "أدخل الاسم والاسم بالعربية للنوعية.";
            return RedirectToAction(nameof(Index));
        }

        string normalizedName = input.Name.Trim();
        if (await dbContext.PartVariants.AnyAsync(candidate => candidate.Name == normalizedName && candidate.Id != id))
        {
            TempData["CategoriesError"] = "اسم النوعية مستخدم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        partVariant.Name = normalizedName;
        partVariant.DisplayNameAr = input.DisplayNameAr.Trim();
        partVariant.SortOrder = input.SortOrder;
        partVariant.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = "تم تحديث النوعية.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPartVariantActive(int id, bool isActive)
    {
        PartVariant? partVariant = await dbContext.PartVariants.SingleOrDefaultAsync(variant => variant.Id == id);
        if (partVariant is null)
        {
            return NotFound();
        }

        partVariant.IsActive = isActive;
        await dbContext.SaveChangesAsync();
        TempData["CategoriesMessage"] = isActive ? "تم تفعيل النوعية." : "تم تعطيل النوعية.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Owner")]
    public async Task<IActionResult> ImportPreviewModal()
    {
        var groups = await seedImportService.GetMissingModelsAsync();
        if (groups.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status204NoContent;
            return new EmptyResult();
        }
        var model = new ImportPreviewViewModel { Groups = groups };
        return PartialView("_ImportPreviewModalPartial", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Owner")]
    public async Task<IActionResult> ImportSelected(List<string> selectedModels)
    {
        if (selectedModels is null || selectedModels.Count == 0)
        {
            if (IsAjaxRequest())
            {
                return Json(new { ok = false, message = "لم تختر أي موديل للاستيراد." });
            }
            TempData["CategoriesError"] = "لم تختر أي موديل للاستيراد.";
            return RedirectToAction(nameof(Index));
        }

        var result = await seedImportService.ImportSelectedAsync(selectedModels);

        string message = result.ModelsImported == 0 && result.PartsImported == 0
            ? "جميع الموديلات المحددة موجودة مسبقاً."
            : $"تم استيراد {result.ModelsImported} موديل و {result.PartsImported} قطعة جديدة.";

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message });
        }
        TempData["CategoriesMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<CategoriesIndexViewModel> BuildIndexModelAsync()
    {
        List<Brand> brands = await dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.SortOrder)
            .ThenBy(brand => brand.DisplayNameAr)
            .ToListAsync();

        List<PhoneModel> phoneModels = await dbContext.PhoneModels
            .AsNoTracking()
            .OrderBy(model => model.BrandId)
            .ThenBy(model => model.SortOrder)
            .ThenBy(model => model.DisplayNameAr)
            .ToListAsync();

        List<PartType> partTypes = await dbContext.PartTypes
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.DisplayNameAr)
            .ToListAsync();

        List<PartVariant> partVariants = await dbContext.PartVariants
            .AsNoTracking()
            .OrderBy(variant => variant.SortOrder)
            .ThenBy(variant => variant.DisplayNameAr)
            .ToListAsync();

        return new CategoriesIndexViewModel
        {
            Brands = brands,
            PhoneModels = phoneModels,
            PartTypes = partTypes,
            PartVariants = partVariants,
            NewBrand = new BrandFormInput(),
            NewPhoneModel = new PhoneModelFormInput(),
            NewPartType = new PartTypeFormInput(),
            NewPartVariant = new PartVariantFormInput()
        };
    }

}
