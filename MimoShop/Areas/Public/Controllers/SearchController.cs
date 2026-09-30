using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class SearchController : Controller
{
    private readonly PublicCatalogQueryService catalog;
    private readonly IStringLocalizer<PublicResources> localizer;

    public SearchController(PublicCatalogQueryService catalog, IStringLocalizer<PublicResources> localizer)
    {
        this.catalog = catalog;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query, int? brandId, int? modelId, int? partTypeId)
    {
        PublicSearchPageViewModel model = await catalog.SearchAsync(query, brandId, modelId, partTypeId);
        ViewData["Title"] = string.IsNullOrWhiteSpace(model.Query)
            ? localizer["Search.Title"].Value
            : localizer["Search.TitleQuery", model.Query].Value;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(int brandId)
    {
        IReadOnlyList<PublicSearchFilterItem> items = await catalog.GetActiveModelsForBrandAsync(brandId);
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetPartTypes(int brandId, int modelId)
    {
        IReadOnlyList<PublicSearchFilterItem> items = await catalog.GetActivePartTypesForModelAsync(brandId, modelId);
        return Json(items);
    }
}
