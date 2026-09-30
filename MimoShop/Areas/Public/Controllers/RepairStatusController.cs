using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MimoShop.Data;
using MimoShop.Models;
using MimoShop.Resources;
using MimoShop.Services;
using MimoShop.Services.Telegram;

namespace MimoShop.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class RepairStatusController : Controller
{
    private readonly MimoShopDbContext dbContext;
    private readonly IStringLocalizer<PublicResources> localizer;

    public RepairStatusController(MimoShopDbContext dbContext, IStringLocalizer<PublicResources> localizer)
    {
        this.dbContext = dbContext;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // jobCode arrives as a query parameter from the landing-page form's
        // no-JS fallback (method="get"). Read it from the query string — a
        // string parameter here would collide with the POST Index signature.
        string jobCode = Request.Query["jobCode"].ToString();
        if (string.IsNullOrWhiteSpace(jobCode))
        {
            ViewData["Title"] = localizer["RepairStatus.Title"].Value;
            return View(new PublicRepairStatusPageViewModel());
        }

        PublicRepairStatusPageViewModel model = await BuildModelAsync(jobCode);
        ViewData["Title"] = model.Result is not null
            ? localizer["RepairStatus.TitleFor", model.JobCodeInput].Value
            : localizer["RepairStatus.Title"].Value;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string jobCode)
    {
        PublicRepairStatusPageViewModel model = await BuildModelAsync(jobCode);
        ViewData["Title"] = model.Result is not null
            ? localizer["RepairStatus.TitleFor", model.JobCodeInput].Value
            : localizer["RepairStatus.Title"].Value;
        return View(model);
    }

    // AJAX endpoint used by the landing-page status modal. Returns the
    // result/error partial so the client can inject it into the modal body.
    [HttpGet]
    public async Task<IActionResult> Lookup(string jobCode)
    {
        PublicRepairStatusPageViewModel model = await BuildModelAsync(jobCode);
        return PartialView("_StatusResultPartial", model);
    }

    private async Task<PublicRepairStatusPageViewModel> BuildModelAsync(string? jobCode)
    {
        string normalized = RepairStatusQueryService.NormalizeInput(jobCode);
        var model = new PublicRepairStatusPageViewModel { JobCodeInput = normalized };

        if (!RepairStatusQueryService.IsWellFormed(normalized))
        {
            model.Message = localizer["RepairStatus.InvalidFormat"].Value;
            model.MessageKind = "error";
            return model;
        }

        PublicRepairStatusCard? result = await FindStatusAsync(normalized);
        if (result is null)
        {
            model.Message = localizer["RepairStatus.NotFound"].Value;
            model.MessageKind = "error";
            return model;
        }

        model.Result = result;
        return model;
    }

    private async Task<PublicRepairStatusCard?> FindStatusAsync(string normalizedCode)
    {
        RepairTicket? ticket = await dbContext.RepairTickets
            .AsNoTracking()
            .Include(t => t.StatusHistory)
            .SingleOrDefaultAsync(t => t.JobCode == normalizedCode);

        if (ticket is null) { return null; }

        return new PublicRepairStatusCard
        {
            JobCode = ticket.JobCode,
            DeviceName = $"{ticket.DeviceBrand} {ticket.DeviceModel}",
            Status = ticket.Status,
            StatusLabel = StatusLabel(ticket.Status),
            AssignedWorkerName = string.IsNullOrWhiteSpace(ticket.AssignedWorkerName)
                ? localizer["RepairStatus.Unassigned"].Value
                : ticket.AssignedWorkerName,
            ProblemDescription = ticket.ProblemDescription,
            CreatedAt = ticket.CreatedAt,
            LastStatusChangedAt = ticket.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => (DateTime?)h.ChangedAt)
                .FirstOrDefault()
        };
    }

    private string StatusLabel(string status) => status switch
    {
        RepairJobStatuses.New => localizer["Status.New"].Value,
        RepairJobStatuses.InProgress => localizer["Status.InProgress"].Value,
        RepairJobStatuses.WaitingForPart => localizer["Status.WaitingForPart"].Value,
        RepairJobStatuses.Done => localizer["Status.Done"].Value,
        RepairJobStatuses.Collected => localizer["Status.Collected"].Value,
        _ => status
    };
}
