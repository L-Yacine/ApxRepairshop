using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class SeedImportService
{
    private readonly MimoShopDbContext _db;
    private readonly string _seedDataPath;

    public SeedImportService(MimoShopDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _seedDataPath = Path.Combine(env.ContentRootPath, "Data", "Seed");
    }

    public async Task<List<ImportModelGroup>> GetMissingModelsAsync()
    {
        var entries = await ReadEntriesAsync();
        if (entries.Count == 0)
            return [];

        var brands = await _db.Brands.ToDictionaryAsync(b => b.Name);
        var groups = new List<ImportModelGroup>();

        foreach (var brandEntry in entries.GroupBy(e => e.Brand))
        {
            if (!brands.TryGetValue(brandEntry.Key, out var brand))
                continue;

            var missing = new List<ImportModelEntry>();

            foreach (var e in brandEntry)
            {
                var exists = await _db.PhoneModels
                    .AnyAsync(m => m.BrandId == brand.Id && m.Name == e.Name);

                if (!exists)
                {
                    missing.Add(new ImportModelEntry
                    {
                        Name = e.Name,
                        DisplayNameAr = e.DisplayNameAr
                    });
                }
            }

            if (missing.Count == 0)
                continue;

            groups.Add(new ImportModelGroup
            {
                Brand = brand.Name,
                DisplayNameAr = brand.DisplayNameAr,
                Models = missing
            });
        }

        return groups;
    }

    public async Task<SeedImportResult> ImportSelectedAsync(List<string> selectedKeys)
    {
        if (selectedKeys is null || selectedKeys.Count == 0)
            return new SeedImportResult(0, 0);

        var entries = await ReadEntriesAsync();
        var entriesByKey = entries
            .GroupBy(e => $"{e.Brand}||{e.Name}")
            .ToDictionary(g => g.Key, g => g.First());

        var brands = await _db.Brands.ToDictionaryAsync(b => b.Name);
        var phoneModelsToAdd = new List<PhoneModel>();

        foreach (var key in selectedKeys)
        {
            if (!entriesByKey.TryGetValue(key, out var entry))
                continue;

            if (!brands.TryGetValue(entry.Brand, out var brand))
                continue;

            var exists = await _db.PhoneModels
                .AnyAsync(m => m.BrandId == brand.Id && m.Name == entry.Name);

            if (exists)
                continue;

            phoneModelsToAdd.Add(new PhoneModel
            {
                BrandId = brand.Id,
                Name = entry.Name,
                DisplayNameAr = entry.DisplayNameAr,
                SortOrder = entry.SortOrder,
                IsActive = true
            });
        }

        if (phoneModelsToAdd.Count == 0)
            return new SeedImportResult(0, 0);

        _db.PhoneModels.AddRange(phoneModelsToAdd);
        await _db.SaveChangesAsync();

        var partTypes = await _db.PartTypes.ToListAsync();
        var variants = await _db.PartVariants
            .Where(v => v.Name == "Original OEM" || v.Name == "Compatible")
            .ToListAsync();

        var existingPartKeys = await _db.InventoryParts
            .Select(p => new { p.BrandId, p.PhoneModelId, p.PartTypeId, p.PartVariantId })
            .ToListAsync();

        var existingPartSet = existingPartKeys
            .Select(p => (p.BrandId, p.PhoneModelId, p.PartTypeId, p.PartVariantId))
            .ToHashSet();

        var inventoryPartsToAdd = new List<InventoryPart>();

        foreach (var model in phoneModelsToAdd)
        {
            foreach (var partType in partTypes)
            {
                foreach (var variant in variants)
                {
                    var key = (model.BrandId, model.Id, partType.Id, variant.Id);
                    if (existingPartSet.Contains(key))
                        continue;

                    inventoryPartsToAdd.Add(new InventoryPart
                    {
                        BrandId = model.BrandId,
                        PhoneModelId = model.Id,
                        PartTypeId = partType.Id,
                        PartVariantId = variant.Id,
                        Quantity = 0,
                        UnitCostPrice = 0,
                        UnitSalePrice = 0,
                        IsStocked = false,
                        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    });
                }
            }
        }

        if (inventoryPartsToAdd.Count > 0)
        {
            _db.InventoryParts.AddRange(inventoryPartsToAdd);
            await _db.SaveChangesAsync();
        }

        return new SeedImportResult(phoneModelsToAdd.Count, inventoryPartsToAdd.Count);
    }

    private async Task<List<PhoneModelEntry>> ReadEntriesAsync()
    {
        var jsonPath = Path.Combine(_seedDataPath, "PhoneModels.json");
        var json = await File.ReadAllTextAsync(jsonPath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<PhoneModelEntry>>(json, options) ?? [];
    }

    private sealed record PhoneModelEntry(string Brand, string Name, string DisplayNameAr, int SortOrder);
}

public sealed record SeedImportResult(int ModelsImported, int PartsImported);
