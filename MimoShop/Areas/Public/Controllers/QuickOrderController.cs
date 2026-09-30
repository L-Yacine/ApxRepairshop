using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class QuickOrderController : Controller
{
    private readonly PublicCatalogQueryService catalog;
    private readonly DeliveryZoneService zoneService;
    private readonly MimoShop.Data.MimoShopDbContext dbContext;
    private readonly IStringLocalizer<PublicResources> localizer;

    public QuickOrderController(
        PublicCatalogQueryService catalog,
        DeliveryZoneService zoneService,
        MimoShop.Data.MimoShopDbContext dbContext,
        IStringLocalizer<PublicResources> localizer)
    {
        this.catalog = catalog;
        this.zoneService = zoneService;
        this.dbContext = dbContext;
        this.localizer = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(int id, QuickOrderForm form)
    {
        PublicPartDetail? part = await catalog.GetPartDetailAsync(id);
        if (part is null)
        {
            return NotFound();
        }

        if (form.Quantity > part.Quantity)
        {
            ModelState.AddModelError(nameof(form.Quantity), localizer["QuickOrder.QuantityLimit", part.Quantity].Value);
        }

        if (form.WilayaId > 0)
        {
            WilayaOption? wilaya = await zoneService.GetActiveWilayaAsync(form.WilayaId);
            if (wilaya is null)
            {
                ModelState.AddModelError(nameof(form.WilayaId), localizer["QuickOrder.WilayaUnavailable"].Value);
            }
            else
            {
                IReadOnlyList<CommuneOption> communes = await zoneService.GetActiveCommunesAsync(form.WilayaId);
                CommuneOption? commune = communes.FirstOrDefault(c => c.Id == form.CommuneId);
                if (commune is null)
                {
                    ModelState.AddModelError(nameof(form.CommuneId), localizer["QuickOrder.CommuneUnavailable"].Value);
                }
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["QuickOrderError"] = localizer["QuickOrder.FixErrors"].Value;
            return RedirectToAction("PartDetail", "Catalog", new { area = "Public", id });
        }

        WilayaOption confirmedWilaya = (await zoneService.GetActiveWilayaAsync(form.WilayaId))!;
        CommuneOption confirmedCommune = (await zoneService.GetActiveCommunesAsync(form.WilayaId))
            .First(c => c.Id == form.CommuneId);

        decimal unitPrice = part.SalePrice;
        decimal subtotal = unitPrice * form.Quantity;
        decimal shippingFee = confirmedWilaya.ShippingFee;
        decimal total = subtotal + shippingFee;

        var order = new ShopOrder
        {
            OrderCode = await GenerateOrderCodeAsync(),
            CustomerName = form.CustomerName.Trim(),
            CustomerPhone = form.CustomerPhone.Trim(),
            CustomerWhatsApp = string.IsNullOrWhiteSpace(form.CustomerWhatsApp) ? null : form.CustomerWhatsApp.Trim(),
            WilayaId = confirmedWilaya.Id,
            CommuneId = confirmedCommune.Id,
            WilayaName = $"{confirmedWilaya.Code} - {confirmedWilaya.NameAr}",
            CommuneName = confirmedCommune.NameAr,
            CommuneNameFr = confirmedCommune.NameFr,
            Address = form.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
            Status = ShopOrderStatuses.New,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            TotalAmount = total,
            CreatedAt = DateTime.Now,
            Lines =
            [
                new ShopOrderLine
                {
                    InventoryPartId = part.InventoryPartId,
                    BrandName = part.BrandName,
                    ModelName = part.ModelName,
                    PartTypeName = part.PartTypeName,
                    VariantName = part.VariantName,
                    PartDisplayName = part.LocalizedDisplayName,
                    Quantity = form.Quantity,
                    UnitPrice = unitPrice,
                    ImageUrl = !string.IsNullOrWhiteSpace(part.ThumbnailUrl) ? part.ThumbnailUrl : part.ImageUrl
                }
            ]
        };

        dbContext.ShopOrders.Add(order);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Confirmation", "Checkout", new { area = "Public", orderCode = order.OrderCode });
    }

    private async Task<string> GenerateOrderCodeAsync()
    {
        int maxId = 0;
        if (await dbContext.ShopOrders.AnyAsync())
        {
            maxId = await dbContext.ShopOrders.MaxAsync(o => o.Id);
        }
        int nextId = maxId + 1;
        string candidate = $"ORD-{nextId:D4}";
        while (await dbContext.ShopOrders.AnyAsync(o => o.OrderCode == candidate) && nextId < 99999)
        {
            nextId++;
            candidate = $"ORD-{nextId:D4}";
        }
        return candidate;
    }
}
