using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class InventoryService
{
    private readonly MimoShopDbContext dbContext;
    private readonly CatalogImageService catalogImageService;

    public InventoryService(MimoShopDbContext dbContext, CatalogImageService catalogImageService)
    {
        this.dbContext = dbContext;
        this.catalogImageService = catalogImageService;
    }

    public async Task<InventoryIndexViewModel> GetIndexAsync()
    {
        List<InventoryPartListItemViewModel> parts = await dbContext.InventoryParts
            .AsNoTracking()
            .Include(part => part.Brand)
            .Include(part => part.PhoneModel)
            .Include(part => part.PartType)
            .Include(part => part.PartVariant)
            .OrderBy(part => part.Brand.Name)
            .ThenBy(part => part.PhoneModel.Name)
            .ThenBy(part => part.PartType.SortOrder)
            .ThenBy(part => part.PartType.Name)
            .ThenBy(part => part.PartVariant.SortOrder)
            .ThenBy(part => part.PartVariant.Name)
            .Select(part => new InventoryPartListItemViewModel
            {
                Id = part.Id,
                Brand = part.Brand.Name,
                Model = part.PhoneModel.Name,
                PartType = part.PartType.Name,
                Variant = part.PartVariant.Name,
                ImageUrl = part.ImageUrl,
                ThumbnailUrl = part.ThumbnailUrl,
                BrandImageUrl = part.Brand.ImageUrl,
                BrandThumbnailUrl = part.Brand.ThumbnailUrl,
                ModelImageUrl = part.PhoneModel.ImageUrl,
                ModelThumbnailUrl = part.PhoneModel.ThumbnailUrl,
                PartTypeImageUrl = part.PartType.ImageUrl,
                PartTypeThumbnailUrl = part.PartType.ThumbnailUrl,
                Quantity = part.Quantity,
                UnitCostPrice = part.UnitCostPrice,
                UnitSalePrice = part.UnitSalePrice,
                IsStocked = part.IsStocked,
                UpdatedAt = part.UpdatedAt
            })
            .ToListAsync();

        return new InventoryIndexViewModel { Parts = parts };
    }

    public async Task<InventoryCategoryOptionsViewModel> GetCategoryOptionsAsync()
    {
        List<LookupOptionViewModel> brands = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.SortOrder)
            .ThenBy(brand => brand.Name)
            .Select(brand => new LookupOptionViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                DisplayNameAr = brand.DisplayNameAr
            })
            .ToListAsync();

        List<LookupOptionViewModel> phoneModels = await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.IsActive)
            .OrderBy(model => model.SortOrder)
            .ThenBy(model => model.Name)
            .Select(model => new LookupOptionViewModel
            {
                Id = model.Id,
                Name = model.Name,
                DisplayNameAr = model.DisplayNameAr,
                ParentId = model.BrandId
            })
            .ToListAsync();

        List<LookupOptionViewModel> partTypes = await dbContext.PartTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .Select(type => new LookupOptionViewModel
            {
                Id = type.Id,
                Name = type.Name,
                DisplayNameAr = type.DisplayNameAr
            })
            .ToListAsync();

        List<LookupOptionViewModel> partVariants = await dbContext.PartVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive)
            .OrderBy(variant => variant.SortOrder)
            .ThenBy(variant => variant.Name)
            .Select(variant => new LookupOptionViewModel
            {
                Id = variant.Id,
                Name = variant.Name,
                DisplayNameAr = variant.DisplayNameAr
            })
            .ToListAsync();

        return new InventoryCategoryOptionsViewModel
        {
            Brands = brands,
            PhoneModels = phoneModels,
            PartTypes = partTypes,
            PartVariants = partVariants
        };
    }

    public async Task<IReadOnlyList<LookupOptionViewModel>> GetPhoneModelsByBrandAsync(int brandId)
    {
        if (brandId <= 0)
        {
            return [];
        }

        return await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.IsActive && model.BrandId == brandId)
            .OrderBy(model => model.SortOrder)
            .ThenBy(model => model.Name)
            .Select(model => new LookupOptionViewModel
            {
                Id = model.Id,
                Name = model.Name,
                DisplayNameAr = model.DisplayNameAr
            })
            .ToListAsync();
    }

    public async Task<InventoryPartFormViewModel?> FindForEditAsync(int id)
    {
        InventoryPart? part = await dbContext.InventoryParts
            .AsNoTracking()
            .SingleOrDefaultAsync(part => part.Id == id);

        if (part is null)
        {
            return null;
        }

        InventoryPartFormViewModel form = ToForm(part);
        form.Options = await GetCategoryOptionsAsync();
        return form;
    }

    public async Task<bool> CreateAsync(InventoryPartFormViewModel model)
    {
        if (model.BrandId <= 0 || model.PhoneModelId <= 0 || model.PartTypeId <= 0 || model.PartVariantId <= 0)
        {
            return false;
        }

        var part = new InventoryPart();
        if (await HasDuplicateAsync(model, null))
        {
            return false;
        }

        await ApplyFormAsync(part, model);
        dbContext.InventoryParts.Add(part);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(InventoryPartFormViewModel model)
    {
        if (model.BrandId <= 0 || model.PhoneModelId <= 0 || model.PartTypeId <= 0 || model.PartVariantId <= 0)
        {
            return false;
        }

        InventoryPart? part = await dbContext.InventoryParts.SingleOrDefaultAsync(part => part.Id == model.Id);
        if (part is null)
        {
            return false;
        }

        if (await HasDuplicateAsync(model, part.Id))
        {
            return false;
        }

        await ApplyFormAsync(part, model);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HasDuplicateAsync(InventoryPartFormViewModel part, int? currentId)
    {
        return await dbContext.InventoryParts.AnyAsync(candidate =>
            candidate.BrandId == part.BrandId
            && candidate.PhoneModelId == part.PhoneModelId
            && candidate.PartTypeId == part.PartTypeId
            && candidate.PartVariantId == part.PartVariantId
            && (!currentId.HasValue || candidate.Id != currentId.Value));
    }

    private static InventoryPartFormViewModel ToForm(InventoryPart part)
    {
        return new InventoryPartFormViewModel
        {
            Id = part.Id,
            BrandId = part.BrandId,
            PhoneModelId = part.PhoneModelId,
            PartTypeId = part.PartTypeId,
            PartVariantId = part.PartVariantId,
            ImageUrl = part.ImageUrl,
            ThumbnailUrl = part.ThumbnailUrl,
            Quantity = part.Quantity,
            UnitCostPrice = part.UnitCostPrice,
            UnitSalePrice = part.UnitSalePrice,
            IsStocked = part.IsStocked
        };
    }

    private async Task ApplyFormAsync(InventoryPart part, InventoryPartFormViewModel model)
    {
        CatalogImageResult? image = await catalogImageService.SaveAsync(
            model.ImageFile,
            "part",
            $"{model.BrandId}-{model.PhoneModelId}-{model.PartTypeId}-{model.PartVariantId}");
        if (image is not null)
        {
            catalogImageService.DeleteStoredImage(part.ImageUrl, part.ThumbnailUrl);
            part.ImageUrl = image.ImageUrl;
            part.ThumbnailUrl = image.ThumbnailUrl;
        }

        part.BrandId = model.BrandId;
        part.PhoneModelId = model.PhoneModelId;
        part.PartTypeId = model.PartTypeId;
        part.PartVariantId = model.PartVariantId;
        part.Quantity = model.Quantity;
        part.UnitCostPrice = model.UnitCostPrice;
        part.UnitSalePrice = model.UnitSalePrice;
        part.IsStocked = model.IsStocked;
        part.UpdatedAt = DateTime.Now;
    }
}
