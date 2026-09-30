using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class CatalogController : Controller
{
    private readonly PublicCatalogQueryService catalog;
    private readonly DeliveryZoneService zoneService;
    private readonly IStringLocalizer<PublicResources> localizer;

    public CatalogController(
        PublicCatalogQueryService catalog,
        DeliveryZoneService zoneService,
        IStringLocalizer<PublicResources> localizer)
    {
        this.catalog = catalog;
        this.zoneService = zoneService;
        this.localizer = localizer;
    }

    public async Task<IActionResult> Brands()
    {
        IReadOnlyList<PublicBrandCard> brands = await catalog.GetBrandsWithStockAsync();
        ViewData["Title"] = localizer["Catalog.BrowseBrands"].Value;
        return View(brands);
    }

    public async Task<IActionResult> Models(int id)
    {
        PublicBrandCard? brand = await catalog.GetBrandAsync(id);
        if (brand is null)
        {
            return NotFound();
        }

        IReadOnlyList<PublicModelCard> models = await catalog.GetModelsWithStockAsync(id);
        ViewData["Title"] = brand.LocalizedName;
        ViewData["BrandId"] = id;
        ViewData["BrandLabel"] = brand.LocalizedName;
        return View(models);
    }

    public async Task<IActionResult> PartTypes(int brandId, int modelId)
    {
        PublicModelCard? model = await catalog.GetModelAsync(brandId, modelId);
        if (model is null)
        {
            return NotFound();
        }

        IReadOnlyList<PublicPartTypeCard> partTypes = await catalog.GetPartTypesWithStockAsync(brandId, modelId);
        ViewData["Title"] = $"{model.LocalizedBrandName} {model.LocalizedName}";
        ViewData["BrandId"] = brandId;
        ViewData["ModelId"] = modelId;
        ViewData["BrandLabel"] = model.LocalizedBrandName;
        ViewData["ModelLabel"] = model.LocalizedName;
        return View(partTypes);
    }

    public async Task<IActionResult> Variants(int brandId, int modelId, int partTypeId)
    {
        PublicPartTypeCard? partType = await catalog.GetPartTypeAsync(brandId, modelId, partTypeId);
        PublicModelCard? model = await catalog.GetModelAsync(brandId, modelId);
        if (partType is null || model is null)
        {
            return NotFound();
        }

        IReadOnlyList<PublicVariantCard> variants = await catalog.GetVariantsAsync(brandId, modelId, partTypeId);
        ViewData["Title"] = $"{model.LocalizedName} — {partType.LocalizedName}";
        ViewData["BrandId"] = brandId;
        ViewData["ModelId"] = modelId;
        ViewData["PartTypeId"] = partTypeId;
        ViewData["BrandLabel"] = model.LocalizedBrandName;
        ViewData["ModelLabel"] = model.LocalizedName;
        ViewData["PartTypeLabel"] = partType.LocalizedName;
        return View(variants);
    }

    public async Task<IActionResult> PartDetail(int id)
    {
        PublicPartDetail? part = await catalog.GetPartDetailAsync(id);
        if (part is null)
        {
            return NotFound();
        }

        part.Wilayas = await zoneService.GetActiveWilayasAsync();

        ViewData["Title"] = part.LocalizedDisplayName;
        ViewData["BrandId"] = part.BrandId;
        ViewData["ModelId"] = part.ModelId;
        ViewData["PartTypeId"] = part.PartTypeId;
        ViewData["BrandLabel"] = part.LocalizedBrandName;
        ViewData["ModelLabel"] = part.LocalizedModelName;
        ViewData["PartTypeLabel"] = part.LocalizedPartTypeName;
        return View(part);
    }
}
