using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Authorize(Roles = StaffRoles.SuperAdmin)]
[Area("Staff")]
public sealed class StaffController : Controller
{
    private readonly StaffAccountService staffAccountService;

    public StaffController(StaffAccountService staffAccountService)
    {
        this.staffAccountService = staffAccountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IReadOnlyCollection<StaffAccount> staff = await staffAccountService.GetAllStaffAsync();
        return View(new StaffListViewModel { Staff = staff });
    }

    [HttpGet]
    public IActionResult CreateModal()
    {
        return PartialView("_CreateModalPartial", new CreateStaffViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ReturnCreateAsync(model);
        }

        if (await staffAccountService.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "اسم المستخدم موجود مسبقاً");
            return ReturnCreateAsync(model);
        }

        await staffAccountService.CreateStaffAsync(model.Username, model.DisplayName, model.Role, model.Password);

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم إنشاء الحساب بنجاح." });
        }
        TempData["StaffMessage"] = "تم إنشاء الحساب بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(string id)
    {
        StaffAccount? account = await staffAccountService.FindByUsernameAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        return PartialView("_EditModalPartial", new EditStaffViewModel
        {
            Username = account.Username,
            DisplayName = account.DisplayName,
            Role = account.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditStaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ReturnEditAsync(model);
        }

        StaffAccount? updated = await staffAccountService.UpdateStaffAsync(model.Username, model.DisplayName, model.Role);
        if (updated is null)
        {
            return NotFound();
        }

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم تحديث الحساب بنجاح." });
        }
        TempData["StaffMessage"] = "تم تحديث الحساب بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPasswordModal(string id)
    {
        StaffAccount? account = await staffAccountService.FindByUsernameAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        return PartialView("_ResetPasswordModalPartial", new ResetPasswordViewModel
        {
            Username = account.Username,
            DisplayName = account.DisplayName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ReturnResetPasswordAsync(model);
        }

        bool success = await staffAccountService.SetPasswordAsync(model.Username, model.NewPassword);
        if (!success)
        {
            return NotFound();
        }

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم إعادة تعيين كلمة المرور بنجاح." });
        }
        TempData["StaffMessage"] = "تم إعادة تعيين كلمة المرور بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string username)
    {
        bool success = await staffAccountService.DeactivateStaffAsync(username);
        if (!success)
        {
            return NotFound();
        }

        TempData["StaffMessage"] = "تم تعطيل الحساب.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string username)
    {
        bool success = await staffAccountService.ActivateStaffAsync(username);
        if (!success)
        {
            return NotFound();
        }

        TempData["StaffMessage"] = "تم تفعيل الحساب.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ReturnCreateAsync(CreateStaffViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_CreateModalPartial", model);
    }

    private IActionResult ReturnEditAsync(EditStaffViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_EditModalPartial", model);
    }

    private IActionResult ReturnResetPasswordAsync(ResetPasswordViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_ResetPasswordModalPartial", model);
    }
}
