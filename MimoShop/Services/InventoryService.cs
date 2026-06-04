using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class InventoryService
{
    private readonly MimoShopDbContext dbContext;

    public InventoryService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<InventoryIndexViewModel> GetIndexAsync()
    {
        List<InventoryPartListItemViewModel> parts = await dbContext.InventoryParts
            .AsNoTracking()
            .OrderBy(part => part.Brand)
            .ThenBy(part => part.Model)
            .ThenBy(part => part.PartType)
            .ThenBy(part => part.Variant)
            .Select(part => new InventoryPartListItemViewModel
            {
                Id = part.Id,
                Brand = part.Brand,
                Model = part.Model,
                PartType = part.PartType,
                Variant = part.Variant,
                Quantity = part.Quantity,
                UnitCostPrice = part.UnitCostPrice,
                UnitSalePrice = part.UnitSalePrice,
                IsStocked = part.IsStocked,
                UpdatedAt = part.UpdatedAt
            })
            .ToListAsync();

        return new InventoryIndexViewModel { Parts = parts };
    }

    public async Task<InventoryPartFormViewModel?> FindForEditAsync(int id)
    {
        InventoryPart? part = await dbContext.InventoryParts
            .AsNoTracking()
            .SingleOrDefaultAsync(part => part.Id == id);

        return part is null ? null : ToForm(part);
    }

    public async Task<bool> CreateAsync(InventoryPartFormViewModel model)
    {
        var part = new InventoryPart();
        ApplyForm(part, model);

        if (await HasDuplicateAsync(part, null))
        {
            return false;
        }

        dbContext.InventoryParts.Add(part);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(InventoryPartFormViewModel model)
    {
        InventoryPart? part = await dbContext.InventoryParts.SingleOrDefaultAsync(part => part.Id == model.Id);
        if (part is null)
        {
            return false;
        }

        ApplyForm(part, model);

        if (await HasDuplicateAsync(part, part.Id))
        {
            return false;
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HasDuplicateAsync(InventoryPart part, int? currentId)
    {
        return await dbContext.InventoryParts.AnyAsync(candidate =>
            candidate.Brand == part.Brand
            && candidate.Model == part.Model
            && candidate.PartType == part.PartType
            && candidate.Variant == part.Variant
            && (!currentId.HasValue || candidate.Id != currentId.Value));
    }

    private static InventoryPartFormViewModel ToForm(InventoryPart part)
    {
        return new InventoryPartFormViewModel
        {
            Id = part.Id,
            Brand = part.Brand,
            Model = part.Model,
            PartType = part.PartType,
            Variant = part.Variant,
            Quantity = part.Quantity,
            UnitCostPrice = part.UnitCostPrice,
            UnitSalePrice = part.UnitSalePrice,
            IsStocked = part.IsStocked
        };
    }

    private static void ApplyForm(InventoryPart part, InventoryPartFormViewModel model)
    {
        part.Brand = NormalizeRequired(model.Brand);
        part.Model = NormalizeRequired(model.Model);
        part.PartType = NormalizeRequired(model.PartType);
        part.Variant = NormalizeRequired(model.Variant);
        part.Quantity = model.Quantity;
        part.UnitCostPrice = model.UnitCostPrice;
        part.UnitSalePrice = model.UnitSalePrice;
        part.IsStocked = model.IsStocked;
        part.UpdatedAt = DateTime.Now;
    }

    private static string NormalizeRequired(string value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
