using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public sealed class RepairTicketsController : Controller
{
    private readonly RepairIntakeService repairIntakeService;
    private readonly RepairWorkflowService repairWorkflowService;
    private readonly StaffAccountService staffAccountService;
    private readonly ShopSettingsService shopSettingsService;

    public RepairTicketsController(
        RepairIntakeService repairIntakeService,
        RepairWorkflowService repairWorkflowService,
        StaffAccountService staffAccountService,
        ShopSettingsService shopSettingsService)
    {
        this.repairIntakeService = repairIntakeService;
        this.repairWorkflowService = repairWorkflowService;
        this.staffAccountService = staffAccountService;
        this.shopSettingsService = shopSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["TutorialKey"] = "staff.RepairTickets.index";
        return View(await repairWorkflowService.GetTicketListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal()
    {
        return PartialView("_CreateModalPartial", await BuildViewModelAsync(new RepairIntakeViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepairIntakeViewModel model, string action = "print")
    {
        StaffAccount? assignedWorker = await staffAccountService.FindByUsernameAsync(model.AssignedWorkerUsername);
        if (assignedWorker is null)
        {
            ModelState.AddModelError(nameof(model.AssignedWorkerUsername), "اختر العامل المسؤول");
        }

        if (!await repairIntakeService.IsKnownBrandModelAsync(model.DeviceBrand, model.DeviceModel))
        {
            ModelState.AddModelError(nameof(model.DeviceModel), "اختر موديلاً من القائمة الخاصة بالعلامة");
        }

        if (!ModelState.IsValid || assignedWorker is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_CreateModalPartial", await BuildViewModelAsync(model));
        }

        RepairTicketRecord ticket = await repairIntakeService.CreateTicketAsync(model, assignedWorker);

        if (IsAjaxRequest())
        {
            if (string.Equals(action, "continue", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    ok = true,
                    replace = "repairCreate",
                    message = $"تم حفظ البطاقة {ticket.JobCode}. ابدأ بطاقة جديدة."
                });
            }
            return Json(new
            {
                ok = true,
                redirectUrl = Url.Action(nameof(Receipt), new { jobCode = ticket.JobCode })
            });
        }

        if (string.Equals(action, "continue", StringComparison.OrdinalIgnoreCase))
        {
            TempData["IntakeMessage"] = $"تم حفظ البطاقة {ticket.JobCode}. ابدأ بطاقة جديدة.";
            return RedirectToAction(nameof(Create));
        }

        return RedirectToAction(nameof(Receipt), new { jobCode = ticket.JobCode });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(string jobCode)
    {
        RepairTicketRecord? ticket = await repairIntakeService.FindTicketByCodeAsync(jobCode);
        if (ticket is null)
        {
            return NotFound();
        }

        ShopSetting settings = await shopSettingsService.GetSettingsAsync();

        return View(new RepairReceiptViewModel
        {
            ShopName = settings.Name,
            ShopLatinName = settings.LatinName,
            ShopPhone = settings.Phone,
            ShopAddress = settings.Address,
            ShopTelegramHandle = settings.TelegramHandle,
            ShopLogoUrl = settings.LogoUrl,
            JobCode = ticket.JobCode,
            CreatedAt = ticket.CreatedAt,
            CustomerName = ticket.Customer.Name,
            CustomerPhone = ticket.Customer.Phone,
            DeviceBrand = ticket.DeviceBrand,
            DeviceModel = ticket.DeviceModel,
            ProblemDescription = ticket.ProblemDescription,
            EstimatedPrice = ticket.EstimatedPrice,
            AssignedWorkerName = ticket.AssignedWorkerName,
            Notes = ticket.Notes
        });
    }

    [HttpGet]
    public async Task<IActionResult> LookupCustomer(string phone)
    {
        CustomerRecord? customer = await repairIntakeService.FindCustomerByPhoneAsync(phone);
        if (customer is null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            name = customer.Name,
            phone = customer.Phone,
            whatsApp = customer.WhatsApp,
            telegram = customer.Telegram
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string jobCode)
    {
        RepairTicketDetailsViewModel? ticket = await repairWorkflowService.FindTicketDetailsAsync(jobCode);
        if (ticket is null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string jobCode, string status)
    {
        string? currentUsername = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUsername is null)
        {
            return Challenge();
        }

        bool updated = await repairWorkflowService.UpdateStatusAsync(jobCode, status, currentUsername);
        if (!updated)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { jobCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConsumeStockedPart(string jobCode, RepairPartSelectionViewModel model)
    {
        string? currentUsername = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUsername is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["RepairMessage"] = "اختر قطعة مخزنة وكمية صحيحة.";
            return RedirectToAction(nameof(Details), new { jobCode });
        }

        bool consumed = await repairWorkflowService.ConsumeStockedPartAsync(
            jobCode,
            model.InventoryPartId,
            model.Quantity,
            currentUsername);

        TempData["RepairMessage"] = consumed
            ? "تم استهلاك القطعة وتحديث الكمية."
            : "تعذر استهلاك القطعة. تحقق من توفر الكمية.";

        return RedirectToAction(nameof(Details), new { jobCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestOnDemandPart(string jobCode, RepairPartSelectionViewModel model)
    {
        string? currentUsername = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUsername is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["RepairMessage"] = "اختر قطعة عند الطلب وكمية صحيحة.";
            return RedirectToAction(nameof(Details), new { jobCode });
        }

        bool requested = await repairWorkflowService.RequestOnDemandPartAsync(
            jobCode,
            model.InventoryPartId,
            model.Quantity,
            currentUsername);

        TempData["RepairMessage"] = requested
            ? "تم طلب القطعة ونقل البطاقة إلى بانتظار قطعة."
            : "تعذر طلب القطعة. تحقق من بيانات القطعة.";

        return RedirectToAction(nameof(Details), new { jobCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveOnDemandPart(string jobCode, int partUsageId)
    {
        string? currentUsername = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUsername is null)
        {
            return Challenge();
        }

        bool received = await repairWorkflowService.ReceiveOnDemandPartAsync(jobCode, partUsageId, currentUsername);
        TempData["RepairMessage"] = received
            ? "تم استلام القطعة واستهلاكها واستئناف العمل."
            : "تعذر استلام القطعة المطلوبة.";

        return RedirectToAction(nameof(Details), new { jobCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(string jobCode, RepairPaymentUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["RepairMessage"] = "أدخل مبالغ صحيحة للدفع.";
            return RedirectToAction(nameof(Details), new { jobCode });
        }

        bool updated = await repairWorkflowService.UpdatePaymentAsync(
            jobCode,
            model.EstimatedPrice,
            model.AmountPaid);

        TempData["RepairMessage"] = updated
            ? "تم حفظ بيانات الدفع."
            : "تعذر حفظ بيانات الدفع.";

        return RedirectToAction(nameof(Details), new { jobCode });
    }

    private async Task<RepairIntakeViewModel> BuildViewModelAsync(RepairIntakeViewModel model)
    {
        string? currentUsername = User.FindFirstValue(ClaimTypes.NameIdentifier);

        model.DeviceOptions = await repairIntakeService.GetDeviceOptionsAsync();
        model.StaffOptions = (await staffAccountService.GetAssignableStaffAsync())
            .Select(account => new StaffOption(account.Username, account.DisplayName))
            .ToList();

        if (string.IsNullOrWhiteSpace(model.AssignedWorkerUsername) && currentUsername is not null)
        {
            model.AssignedWorkerUsername = currentUsername;
        }

        return model;
    }
}
