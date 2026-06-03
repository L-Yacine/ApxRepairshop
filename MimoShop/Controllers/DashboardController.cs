using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Services;

namespace MimoShop.Controllers;

[Authorize(Roles = StaffRoles.Owner)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
