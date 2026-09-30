using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public sealed class ShopOrdersController : Controller
{
    private readonly ShopOrderService orderService;

    public ShopOrdersController(ShopOrderService orderService)
    {
        this.orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        string filter = string.IsNullOrWhiteSpace(status) ? ShopOrderStatuses.New : status;
        if (filter != "All" && !ShopOrderStatuses.All.Contains(filter))
        {
            filter = ShopOrderStatuses.New;
        }

        IReadOnlyList<ShopOrderSummary> orders = await orderService.ListOrdersAsync(filter);
        var allOrders = await orderService.ListOrdersAsync("All");

        var model = new ShopOrdersIndexViewModel
        {
            Orders = orders,
            StatusFilter = filter,
            NewCount = allOrders.Count(o => o.Status == ShopOrderStatuses.New),
            ConfirmedCount = allOrders.Count(o => o.Status == ShopOrderStatuses.Confirmed),
            ShippedCount = allOrders.Count(o => o.Status == ShopOrderStatuses.Shipped),
            DeliveredCount = allOrders.Count(o => o.Status == ShopOrderStatuses.Delivered),
            ReturnedCount = allOrders.Count(o => o.Status == ShopOrderStatuses.Returned),
            CancelledCount = allOrders.Count(o => o.Status == ShopOrderStatuses.Cancelled)
        };
        ViewData["Title"] = "الطلبات";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        ShopOrder? order = await orderService.FindForProcessingAsync(id);
        if (order is null) { return NotFound(); }

        var model = new ShopOrderDetailViewModel
        {
            Order = order,
            CanConfirm = order.Status == ShopOrderStatuses.New,
            CanShip = order.Status == ShopOrderStatuses.Confirmed,
            CanDeliver = order.Status == ShopOrderStatuses.Shipped,
            CanReturn = order.Status == ShopOrderStatuses.Shipped,
            CanCancel = order.Status == ShopOrderStatuses.New || order.Status == ShopOrderStatuses.Confirmed
        };
        ViewData["Title"] = $"الطلب {order.OrderCode}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var (ok, error) = await orderService.ConfirmAsync(id, User.Identity?.Name ?? "");
        TempData[ok ? "ShopOrdersMessage" : "ShopOrdersError"] = ok ? "تم تأكيد الطلب وتخفيض المخزون." : error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id)
    {
        var (ok, error) = await orderService.ShipAsync(id, User.Identity?.Name ?? "");
        TempData[ok ? "ShopOrdersMessage" : "ShopOrdersError"] = ok ? "تم شحن الطلب." : error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(int id)
    {
        var (ok, error) = await orderService.DeliverAsync(id, User.Identity?.Name ?? "");
        TempData[ok ? "ShopOrdersMessage" : "ShopOrdersError"] = ok ? "تم تسليم الطلب." : error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id, string reason)
    {
        var (ok, error) = await orderService.ReturnAsync(id, reason ?? "", User.Identity?.Name ?? "");
        TempData[ok ? "ShopOrdersMessage" : "ShopOrdersError"] = ok ? "تم إرجاع الطلب وإرجاع القطع للمخزون." : error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var (ok, error) = await orderService.CancelAsync(id, reason, User.Identity?.Name ?? "");
        TempData[ok ? "ShopOrdersMessage" : "ShopOrdersError"] = ok ? "تم إلغاء الطلب." : error;
        return RedirectToAction(nameof(Details), new { id });
    }
}