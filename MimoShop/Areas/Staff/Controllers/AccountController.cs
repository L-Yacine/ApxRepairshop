using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public class AccountController : Controller
{
    private readonly StaffAccountService staffAccountService;
    private readonly ShopSettingsService shopSettingsService;

    public AccountController(StaffAccountService staffAccountService, ShopSettingsService shopSettingsService)
    {
        this.staffAccountService = staffAccountService;
        this.shopSettingsService = shopSettingsService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ShopSetting settings = await shopSettingsService.GetSettingsAsync();
        ViewData["ShopName"] = settings.Name;
        ViewData["ShopLatin"] = settings.LatinName;
        ViewData["ShopMark"] = settings.LatinName.Length > 0 ? settings.LatinName[0].ToString() : "M";
        ViewData["ShopLogoUrl"] = settings.LogoUrl;

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        StaffAccount? account = await staffAccountService.ValidateCredentialsAsync(model.Username, model.Password);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيح");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Username),
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.Role, account.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        string username = User.Identity?.Name ?? string.Empty;
        StaffAccount? account = await staffAccountService.FindByUsernameAsync(username);
        if (account is null)
        {
            return NotFound();
        }

        return View(account);
    }

    [HttpGet]
    public IActionResult ChangePasswordModal()
    {
        return PartialView("_ChangePasswordModalPartial", new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ReturnChangePasswordAsync(model);
        }

        string username = User.Identity?.Name ?? string.Empty;
        bool success = await staffAccountService.ChangePasswordAsync(username, model.CurrentPassword, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "كلمة المرور الحالية غير صحيحة");
            return ReturnChangePasswordAsync(model);
        }

        if (IsAjaxRequest())
        {
            return Json(new { ok = true, reload = true, message = "تم تغيير كلمة المرور بنجاح." });
        }
        TempData["ProfileMessage"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToAction(nameof(Profile));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ReturnChangePasswordAsync(ChangePasswordViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_ChangePasswordModalPartial", model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
