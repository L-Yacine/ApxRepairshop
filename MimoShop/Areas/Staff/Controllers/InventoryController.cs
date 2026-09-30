using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public sealed class InventoryController : Controller
{
    private readonly InventoryService inventoryService;
    private readonly CatalogImageFetchService catalogImageFetchService;

    public InventoryController(
        InventoryService inventoryService,
        CatalogImageFetchService catalogImageFetchService)
    {
        this.inventoryService = inventoryService;
        this.catalogImageFetchService = catalogImageFetchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await inventoryService.GetIndexAsync());
    }

    [HttpGet]
    public async Task<IActionResult> FormModal(int? id)
    {
        InventoryPartFormViewModel? model;
        if (id.HasValue && id.Value > 0)
        {
            model = await inventoryService.FindForEditAsync(id.Value);
            if (model is null) return NotFound();
        }
        else
        {
            model = new InventoryPartFormViewModel
            {
                Options = await inventoryService.GetCategoryOptionsAsync()
            };
        }
        return PartialView("_FormModalPartial", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryPartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnFormAsync(model);
        }

        bool created;
        try
        {
            created = await inventoryService.CreateAsync(model);
        }
        catch (CatalogImageException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return await ReturnFormAsync(model);
        }

        if (!created)
        {
            ModelState.AddModelError(string.Empty, "هذه القطعة مسجلة من قبل لنفس العلامة والموديل والنوع والنوعية.");
            return await ReturnFormAsync(model);
        }

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تمت إضافة القطعة." });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryPartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnFormAsync(model);
        }

        bool updated;
        try
        {
            updated = await inventoryService.UpdateAsync(model);
        }
        catch (CatalogImageException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return await ReturnFormAsync(model);
        }

        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "تعذر حفظ القطعة. تأكد أنها موجودة وأنها غير مكررة.");
            return await ReturnFormAsync(model);
        }

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم حفظ التعديلات." });
        }
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IActionResult> ReturnFormAsync(InventoryPartFormViewModel model)
    {
        model.Options = await inventoryService.GetCategoryOptionsAsync();
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_FormModalPartial", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetPhoneModels(int brandId)
    {
        IReadOnlyList<LookupOptionViewModel> models = await inventoryService.GetPhoneModelsByBrandAsync(brandId);
        return Json(models.Select(model => new { id = model.Id, name = model.DisplayNameAr }));
    }

    [HttpGet]
    public async Task<IActionResult> SearchPartImages(int id, string? query, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<CatalogImageCandidate> candidates =
                await catalogImageFetchService.SearchPartImagesAsync(id, query, cancellationToken);

            return Json(candidates);
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPartImage(int id, string sourceUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { message = "اختر صورة أولاً." });
        }

        try
        {
            AppliedCatalogImageResult? result =
                await catalogImageFetchService.ApplyPartImageAsync(id, sourceUrl, cancellationToken);

            if (result is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { message = "القطعة غير موجودة." });
            }

            return Json(new
            {
                message = "تم حفظ صورة القطعة.",
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
}
