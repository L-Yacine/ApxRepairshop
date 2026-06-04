using Microsoft.AspNetCore.Mvc;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Controllers;

public sealed class InventoryController : Controller
{
    private readonly InventoryService inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        this.inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await inventoryService.GetIndexAsync());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new InventoryPartFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryPartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        bool created = await inventoryService.CreateAsync(model);
        if (!created)
        {
            ModelState.AddModelError(string.Empty, "هذه القطعة مسجلة من قبل لنفس العلامة والموديل والنوع والنوعية.");
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        InventoryPartFormViewModel? model = await inventoryService.FindForEditAsync(id);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryPartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        bool updated = await inventoryService.UpdateAsync(model);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "تعذر حفظ القطعة. تأكد أنها موجودة وأنها غير مكررة.");
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
