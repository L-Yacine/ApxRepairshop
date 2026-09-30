using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.SuperAdmin}")]
[Area("Staff")]
public class DashboardController : Controller
{
    private readonly DashboardService dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await dashboardService.BuildDashboardAsync());
    }
}
