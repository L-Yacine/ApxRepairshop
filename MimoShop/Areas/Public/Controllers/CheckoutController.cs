using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MimoShop.Localization;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class CheckoutController : Controller
{
    private readonly ShopOrderService orderService;
    private readonly CartService cart;
    private readonly DeliveryZoneService zoneService;
    private readonly IStringLocalizer<PublicResources> localizer;

    public CheckoutController(
        ShopOrderService orderService,
        CartService cart,
        DeliveryZoneService zoneService,
        IStringLocalizer<PublicResources> localizer)
    {
        this.orderService = orderService;
        this.cart = cart;
        this.zoneService = zoneService;
        this.localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        CartSummary summary = await cart.GetCartSummaryAsync(HttpContext.Session);
        if (summary.ItemCount == 0)
        {
            return RedirectToAction("Index", "Cart", new { area = "Public" });
        }

        CheckoutPageViewModel model = await orderService.BuildCheckoutPageAsync(HttpContext.Session);
        ViewData["Title"] = localizer["Checkout.Title"].Value;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetCommunes(int id)
    {
        IReadOnlyList<CommuneOption> communes = await zoneService.GetActiveCommunesAsync(id);
        return Json(communes);
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingFee(int id)
    {
        WilayaOption? wilaya = await zoneService.GetActiveWilayaAsync(id);
        if (wilaya is null) { return NotFound(); }
        return Json(new { shippingFee = wilaya.ShippingFee });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutFormInput form)
    {
        if (string.IsNullOrWhiteSpace(form.CustomerName)
            || string.IsNullOrWhiteSpace(form.CustomerPhone)
            || form.WilayaId <= 0
            || form.CommuneId <= 0
            || string.IsNullOrWhiteSpace(form.Address))
        {
            ModelState.AddModelError(string.Empty, localizer["Checkout.FillRequired"].Value);
        }

        CartSummary summary = await cart.GetCartSummaryAsync(HttpContext.Session);
        if (summary.ItemCount == 0)
        {
            return RedirectToAction("Index", "Cart", new { area = "Public" });
        }

        if (!ModelState.IsValid)
        {
            CheckoutPageViewModel model = await orderService.BuildCheckoutPageAsync(HttpContext.Session);
            model.Form = form;
            ViewData["Title"] = localizer["Checkout.Title"].Value;
            return View(nameof(Index), model);
        }

        PlaceOrderResult result = await orderService.PlaceOrderAsync(HttpContext.Session, form);
        if (!result.Ok)
        {
            CheckoutPageViewModel model = await orderService.BuildCheckoutPageAsync(HttpContext.Session);
            model.Form = form;
            ModelState.AddModelError(string.Empty, result.Error ?? localizer["Checkout.OrderFailed"].Value);
            ViewData["Title"] = localizer["Checkout.Title"].Value;
            return View(nameof(Index), model);
        }

        return RedirectToAction(nameof(Confirmation), new { orderCode = result.OrderCode });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode)) { return RedirectToAction("Index", "Home", new { area = "Public" }); }

        ShopOrder? order = await orderService.FindByCodeAsync(orderCode);
        if (order is null) { return RedirectToAction("Index", "Home", new { area = "Public" }); }

        var model = new OrderConfirmationViewModel
        {
            OrderCode = order.OrderCode,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            WilayaName = order.WilayaName,
            CommuneName = order.CommuneName,
            Address = order.Address,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            Lines = order.Lines.ToList(),
            CreatedAt = order.CreatedAt
        };

        if (!PublicCulture.IsArabic)
        {
            WilayaOption? wilaya = await zoneService.GetActiveWilayaAsync(order.WilayaId);
            if (wilaya is not null)
            {
                model.WilayaName = $"{wilaya.Code} - {wilaya.LocalizedName}";
            }
            model.CommuneName = string.IsNullOrWhiteSpace(order.CommuneNameFr)
                ? order.CommuneName
                : order.CommuneNameFr;
        }

        ViewData["Title"] = localizer["Checkout.OrderConfirmedTitle", order.OrderCode].Value;
        return View(model);
    }
}
