using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class HomeController : Controller
{
    private readonly ShopSettingsService shopSettings;
    private readonly PublicCatalogQueryService catalog;
    private readonly HeroSlideService heroSlides;

    public HomeController(ShopSettingsService shopSettings, PublicCatalogQueryService catalog, HeroSlideService heroSlides)
    {
        this.shopSettings = shopSettings;
        this.catalog = catalog;
        this.heroSlides = heroSlides;
    }

    public async Task<IActionResult> Index()
    {
        ShopSetting settings = await shopSettings.GetSettingsAsync();
        IReadOnlyList<PublicBrandCard> brands = await catalog.GetBrandsWithStockAsync();
        IReadOnlyList<PublicPartTypeCard> categories = await catalog.GetPartTypesWithStockAsync();
        IReadOnlyList<PublicVariantCard> newestParts = await catalog.GetNewestPartsAsync(12);
        IReadOnlyList<HeroSlide> slides = await heroSlides.GetActiveSlidesAsync();

        var model = new PublicLandingViewModel
        {
            ShopName = settings.Name,
            ShopLatinName = settings.LatinName,
            ShopLogoUrl = string.IsNullOrWhiteSpace(settings.LogoUrl) ? null : settings.LogoUrl,
            ShopPhone = settings.Phone,
            ShopWhatsApp = string.IsNullOrWhiteSpace(settings.WhatsApp) ? null : settings.WhatsApp,
            ShopTelegramHandle = string.IsNullOrWhiteSpace(settings.TelegramHandle) ? null : settings.TelegramHandle,
            ShopAddress = settings.Address,
            OpeningHours = string.IsNullOrWhiteSpace(settings.OpeningHours) ? null : settings.OpeningHours,
            Brands = brands,
            Categories = categories,
            NewestParts = newestParts,
            HeroSlides = slides
        };

        return View(model);
    }
}