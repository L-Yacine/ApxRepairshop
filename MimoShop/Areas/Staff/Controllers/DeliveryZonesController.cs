using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Areas.Staff.Controllers;

[Area("Staff")]
public sealed class DeliveryZonesController : Controller
{
    private readonly DeliveryZoneService zoneService;

    public DeliveryZonesController(DeliveryZoneService zoneService)
    {
        this.zoneService = zoneService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? selectedWilayaId)
    {
        DeliveryZonesIndexViewModel model = await BuildIndexAsync(selectedWilayaId ?? 0);
        return View(model);
    }

    [HttpGet]
    public IActionResult CreateWilaya()
    {
        ViewData["Title"] = "إضافة ولاية";
        return View(new WilayaFormInput { IsActive = true, ShippingFee = 0m });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWilaya(WilayaFormInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        bool ok = await zoneService.CreateWilayaAsync(input);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "الرمز أو الاسم مستخدم بالفعل لولاية أخرى.");
            return View(input);
        }
        TempData["DeliveryZonesMessage"] = "تمت إضافة الولاية.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditWilaya(int id)
    {
        Wilaya? wilaya = await zoneService.FindWilayaAsync(id);
        if (wilaya is null) { return NotFound(); }

        var input = new WilayaFormInput
        {
            Code = wilaya.Code,
            NameAr = wilaya.NameAr,
            NameFr = wilaya.NameFr,
            ShippingFee = wilaya.ShippingFee,
            IsActive = wilaya.IsActive
        };
        ViewData["Title"] = $"تعديل الولاية — {wilaya.NameAr}";
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWilaya(int id, WilayaFormInput input)
    {
        if (!ModelState.IsValid) { return View(input); }

        bool ok = await zoneService.UpdateWilayaAsync(id, input);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "الرمز أو الاسم مستخدم بالفعل لولاية أخرى.");
            return View(input);
        }
        TempData["DeliveryZonesMessage"] = "تم تحديث الولاية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWilaya(int id)
    {
        await zoneService.ToggleWilayaAsync(id);
        TempData["DeliveryZonesMessage"] = "تم تحديث حالة الولاية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = id });
    }

    [Authorize(Roles = "SuperAdmin,Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWilaya(int id)
    {
        var (ok, error) = await zoneService.DeleteWilayaAsync(id);
        if (!ok)
        {
            TempData["DeliveryZonesError"] = error;
            return RedirectToAction(nameof(Index), new { selectedWilayaId = id });
        }
        TempData["DeliveryZonesMessage"] = "تم حذف الولاية.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateCommune(int wilayaId)
    {
        Wilaya? wilaya = await zoneService.FindWilayaAsync(wilayaId);
        if (wilaya is null) { return NotFound(); }

        ViewData["WilayaId"] = wilayaId;
        ViewData["WilayaName"] = wilaya.NameAr;
        ViewData["Title"] = $"إضافة بلدية — {wilaya.NameAr}";
        return View(new CommuneFormInput { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCommune(int wilayaId, CommuneFormInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewData["WilayaId"] = wilayaId;
            return View(input);
        }

        bool ok = await zoneService.CreateCommuneAsync(wilayaId, input);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "اسم البلدية بالعربية مستخدم بالفعل لهذه الولاية.");
            ViewData["WilayaId"] = wilayaId;
            return View(input);
        }
        TempData["DeliveryZonesMessage"] = "تمت إضافة البلدية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = wilayaId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCommune(int id)
    {
        Commune? commune = await zoneService.FindCommuneAsync(id);
        if (commune is null) { return NotFound(); }
        Wilaya? wilaya = await zoneService.FindWilayaAsync(commune.WilayaId);

        var input = new CommuneFormInput
        {
            NameAr = commune.NameAr,
            NameFr = commune.NameFr,
            IsActive = commune.IsActive
        };
        ViewData["WilayaId"] = commune.WilayaId;
        ViewData["WilayaName"] = wilaya?.NameAr ?? "";
        ViewData["Title"] = $"تعديل البلدية — {commune.NameAr}";
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCommune(int id, CommuneFormInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        bool ok = await zoneService.UpdateCommuneAsync(id, input);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "اسم البلدية بالعربية مستخدم بالفعل لهذه الولاية.");
            return View(input);
        }
        Commune? commune = await zoneService.FindCommuneAsync(id);
        TempData["DeliveryZonesMessage"] = "تم تحديث البلدية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = commune?.WilayaId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCommune(int id)
    {
        Commune? commune = await zoneService.FindCommuneAsync(id);
        if (commune is null) { return NotFound(); }
        await zoneService.ToggleCommuneAsync(id);
        TempData["DeliveryZonesMessage"] = "تم تحديث حالة البلدية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = commune.WilayaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCommune(int id)
    {
        Commune? commune = await zoneService.FindCommuneAsync(id);
        if (commune is null) { return NotFound(); }

        var (ok, error) = await zoneService.DeleteCommuneAsync(id);
        if (!ok)
        {
            TempData["DeliveryZonesError"] = error;
            return RedirectToAction(nameof(Index), new { selectedWilayaId = commune.WilayaId });
        }
        TempData["DeliveryZonesMessage"] = "تم حذف البلدية.";
        return RedirectToAction(nameof(Index), new { selectedWilayaId = commune.WilayaId });
    }

    private async Task<DeliveryZonesIndexViewModel> BuildIndexAsync(int selectedWilayaId)
    {
        IReadOnlyList<WilayaSummary> wilayas = await zoneService.ListWilayasAsync();

        DeliveryZonesIndexViewModel model = new()
        {
            Wilayas = wilayas,
            SelectedWilayaId = selectedWilayaId
        };

        if (selectedWilayaId > 0)
        {
            model.SelectedWilaya = wilayas.FirstOrDefault(w => w.Id == selectedWilayaId);
            model.Communes = await zoneService.ListCommunesAsync(selectedWilayaId);
        }

        return model;
    }
}