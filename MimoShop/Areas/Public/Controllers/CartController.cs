using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class CartController : Controller
{
    private readonly CartService cart;
    private readonly PublicCatalogQueryService catalog;
    private readonly IStringLocalizer<PublicResources> localizer;

    public CartController(
        CartService cart,
        PublicCatalogQueryService catalog,
        IStringLocalizer<PublicResources> localizer)
    {
        this.cart = cart;
        this.catalog = catalog;
        this.localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        List<CartItem> items = await cart.GetCartAsync(HttpContext.Session);
        CartSummary summary = await cart.GetCartSummaryAsync(HttpContext.Session);

        var model = new CartPageViewModel
        {
            Items = items,
            Subtotal = summary.Subtotal,
            ItemCount = summary.ItemCount
        };
        ViewData["Title"] = localizer["Cart.Title"].Value;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1)
    {
        if (quantity < 1) { quantity = 1; }

        PublicPartDetail? part = await catalog.GetPartDetailAsync(id);
        if (part is null || part.Quantity <= 0)
        {
            string outOfStock = localizer["Cart.OutOfStockError"].Value;
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success = false, message = outOfStock });
            }
            TempData["CartError"] = outOfStock;
            return RedirectToAction(nameof(Index));
        }

        if (quantity > part.Quantity)
        {
            quantity = part.Quantity;
        }

        string displayName = part.LocalizedDisplayName;
        string thumb = !string.IsNullOrWhiteSpace(part.ThumbnailUrl) ? part.ThumbnailUrl : part.ImageUrl;

        var item = new CartItem(
            part.InventoryPartId,
            part.BrandDisplayNameAr,
            part.ModelDisplayNameAr,
            part.PartTypeDisplayNameAr,
            part.VariantDisplayNameAr,
            displayName,
            part.SalePrice,
            thumb,
            quantity);

        await cart.AddItemAsync(HttpContext.Session, item);

        if (Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            var summary = await cart.GetCartSummaryAsync(HttpContext.Session);
            return Json(new { success = true, cartCount = summary.ItemCount, message = localizer["Cart.Added"].Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int inventoryPartId, int quantity)
    {
        await cart.UpdateQuantityAsync(HttpContext.Session, inventoryPartId, quantity);

        if (Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            var items = await cart.GetCartAsync(HttpContext.Session);
            var summary = await cart.GetCartSummaryAsync(HttpContext.Session);
            var updatedItem = items.FirstOrDefault(i => i.InventoryPartId == inventoryPartId);
            return Json(new
            {
                success = true,
                cartCount = summary.ItemCount,
                subtotal = summary.Subtotal,
                removed = updatedItem == null,
                itemTotal = updatedItem != null ? (updatedItem.UnitPrice * updatedItem.Quantity) : 0m
            });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int inventoryPartId)
    {
        await cart.RemoveItemAsync(HttpContext.Session, inventoryPartId);

        if (Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            var summary = await cart.GetCartSummaryAsync(HttpContext.Session);
            return Json(new
            {
                success = true,
                cartCount = summary.ItemCount,
                subtotal = summary.Subtotal
            });
        }

        return RedirectToAction(nameof(Index));
    }
}
