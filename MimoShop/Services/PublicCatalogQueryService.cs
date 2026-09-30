using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class PublicCatalogQueryService
{
    private readonly MimoShopDbContext dbContext;

    public PublicCatalogQueryService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicBrandCard>> GetBrandsWithStockAsync()
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.Brand.IsActive)
            .Select(part => new
            {
                part.BrandId,
                part.Brand.Name,
                part.Brand.DisplayNameAr,
                part.Brand.SortOrder,
                part.Brand.ImageUrl,
                part.Brand.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PublicBrandCard
            {
                Id = x.BrandId,
                Name = x.Name,
                DisplayNameAr = x.DisplayNameAr,
                ImageUrl = x.ImageUrl,
                ThumbnailUrl = x.ThumbnailUrl
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PublicPartTypeCard>> GetPartTypesWithStockAsync()
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.PartType.IsActive)
            .Select(part => new
            {
                part.PartTypeId,
                part.PartType.Name,
                part.PartType.DisplayNameAr,
                part.PartType.SortOrder,
                part.PartType.ImageUrl,
                part.PartType.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PublicPartTypeCard
            {
                Id = x.PartTypeId,
                Name = x.Name,
                DisplayNameAr = x.DisplayNameAr,
                ImageUrl = x.ImageUrl,
                ThumbnailUrl = x.ThumbnailUrl
            })
            .ToListAsync();
    }

    public async Task<PublicBrandCard?> GetBrandAsync(int brandId)
    {
        return await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Id == brandId && brand.IsActive)
            .Select(brand => new PublicBrandCard
            {
                Id = brand.Id,
                Name = brand.Name,
                DisplayNameAr = brand.DisplayNameAr,
                ImageUrl = brand.ImageUrl,
                ThumbnailUrl = brand.ThumbnailUrl
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<PublicModelCard>> GetModelsWithStockAsync(int brandId)
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModel.IsActive)
            .Select(part => new
            {
                part.PhoneModelId,
                part.PhoneModel.Name,
                part.PhoneModel.DisplayNameAr,
                part.PhoneModel.SortOrder,
                part.PhoneModel.ImageUrl,
                part.PhoneModel.ThumbnailUrl,
                BrandImageUrl = part.Brand.ImageUrl,
                BrandThumbnailUrl = part.Brand.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PublicModelCard
            {
                Id = x.PhoneModelId,
                Name = x.Name,
                DisplayNameAr = x.DisplayNameAr,
                ImageUrl = x.ImageUrl,
                ThumbnailUrl = x.ThumbnailUrl,
                BrandImageUrl = x.BrandImageUrl,
                BrandThumbnailUrl = x.BrandThumbnailUrl
            })
            .ToListAsync();
    }

    public async Task<PublicModelCard?> GetModelAsync(int brandId, int modelId)
    {
        return await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.Id == modelId && model.BrandId == brandId && model.IsActive)
            .Select(model => new PublicModelCard
            {
                Id = model.Id,
                Name = model.Name,
                DisplayNameAr = model.DisplayNameAr,
                ImageUrl = model.ImageUrl,
                ThumbnailUrl = model.ThumbnailUrl,
                BrandImageUrl = model.Brand.ImageUrl,
                BrandThumbnailUrl = model.Brand.ThumbnailUrl,
                BrandId = model.BrandId,
                BrandName = model.Brand.Name,
                BrandDisplayNameAr = model.Brand.DisplayNameAr
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<PublicPartTypeCard>> GetPartTypesWithStockAsync(int brandId, int modelId)
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModelId == modelId && part.PhoneModel.IsActive)
            .Where(part => part.PartType.IsActive)
            .Select(part => new
            {
                part.PartTypeId,
                part.PartType.Name,
                part.PartType.DisplayNameAr,
                part.PartType.SortOrder,
                part.PartType.ImageUrl,
                part.PartType.ThumbnailUrl,
                ModelImageUrl = part.PhoneModel.ImageUrl,
                ModelThumbnailUrl = part.PhoneModel.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PublicPartTypeCard
            {
                Id = x.PartTypeId,
                Name = x.Name,
                DisplayNameAr = x.DisplayNameAr,
                ImageUrl = x.ImageUrl,
                ThumbnailUrl = x.ThumbnailUrl,
                ModelImageUrl = x.ModelImageUrl,
                ModelThumbnailUrl = x.ModelThumbnailUrl
            })
            .ToListAsync();
    }

    public async Task<PublicPartTypeCard?> GetPartTypeAsync(int brandId, int modelId, int partTypeId)
    {
        return await dbContext.PartTypes
            .AsNoTracking()
            .Where(type => type.Id == partTypeId && type.IsActive)
            .Select(type => new PublicPartTypeCard
            {
                Id = type.Id,
                Name = type.Name,
                DisplayNameAr = type.DisplayNameAr,
                ImageUrl = type.ImageUrl,
                ThumbnailUrl = type.ThumbnailUrl
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<PublicVariantCard>> GetVariantsAsync(int brandId, int modelId, int partTypeId)
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModelId == modelId && part.PhoneModel.IsActive)
            .Where(part => part.PartTypeId == partTypeId && part.PartType.IsActive)
            .Where(part => part.PartVariant.IsActive)
            .OrderBy(part => part.PartVariant.SortOrder)
            .ThenBy(part => part.PartVariant.Name)
            .Select(part => new PublicVariantCard
            {
                InventoryPartId = part.Id,
                VariantId = part.PartVariantId,
                VariantName = part.PartVariant.Name,
                VariantDisplayNameAr = part.PartVariant.DisplayNameAr,
                SalePrice = part.UnitSalePrice,
                Quantity = part.Quantity,
                ImageUrl = part.ImageUrl != string.Empty
                    ? part.ImageUrl
                    : part.PhoneModel.ImageUrl != string.Empty
                        ? part.PhoneModel.ImageUrl
                        : part.Brand.ImageUrl,
                ThumbnailUrl = part.ThumbnailUrl != string.Empty
                    ? part.ThumbnailUrl
                    : part.PhoneModel.ThumbnailUrl != string.Empty
                        ? part.PhoneModel.ThumbnailUrl
                        : part.Brand.ThumbnailUrl != string.Empty
                            ? part.Brand.ThumbnailUrl
                            : part.PhoneModel.ImageUrl != string.Empty
                                ? part.PhoneModel.ImageUrl
                                : part.Brand.ImageUrl,
                BrandName = part.Brand.Name,
                BrandDisplayNameAr = part.Brand.DisplayNameAr,
                ModelName = part.PhoneModel.Name,
                ModelDisplayNameAr = part.PhoneModel.DisplayNameAr,
                PartTypeName = part.PartType.Name,
                PartTypeDisplayNameAr = part.PartType.DisplayNameAr
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PublicVariantCard>> GetNewestPartsAsync(int count)
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.Brand.IsActive && part.PhoneModel.IsActive && part.PartType.IsActive && part.PartVariant.IsActive)
            .OrderByDescending(part => part.UpdatedAt)
            .Take(count)
            .Select(part => new PublicVariantCard
            {
                InventoryPartId = part.Id,
                VariantId = part.PartVariantId,
                VariantName = part.PartVariant.Name,
                VariantDisplayNameAr = part.PartVariant.DisplayNameAr,
                SalePrice = part.UnitSalePrice,
                Quantity = part.Quantity,
                ImageUrl = part.ImageUrl != string.Empty
                    ? part.ImageUrl
                    : part.PhoneModel.ImageUrl != string.Empty
                        ? part.PhoneModel.ImageUrl
                        : part.Brand.ImageUrl,
                ThumbnailUrl = part.ThumbnailUrl != string.Empty
                    ? part.ThumbnailUrl
                    : part.PhoneModel.ThumbnailUrl != string.Empty
                        ? part.PhoneModel.ThumbnailUrl
                        : part.Brand.ThumbnailUrl != string.Empty
                            ? part.Brand.ThumbnailUrl
                            : part.PhoneModel.ImageUrl != string.Empty
                                ? part.PhoneModel.ImageUrl
                                : part.Brand.ImageUrl,
                BrandName = part.Brand.Name,
                BrandDisplayNameAr = part.Brand.DisplayNameAr,
                ModelName = part.PhoneModel.Name,
                ModelDisplayNameAr = part.PhoneModel.DisplayNameAr,
                PartTypeName = part.PartType.Name,
                PartTypeDisplayNameAr = part.PartType.DisplayNameAr
            })
            .ToListAsync();
    }

    public async Task<PublicPartDetail?> GetPartDetailAsync(int inventoryPartId)
    {
        return await dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.Id == inventoryPartId)
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.Brand.IsActive && part.PhoneModel.IsActive && part.PartType.IsActive && part.PartVariant.IsActive)
            .Select(part => new PublicPartDetail
            {
                InventoryPartId = part.Id,
                VariantId = part.PartVariantId,
                VariantName = part.PartVariant.Name,
                VariantDisplayNameAr = part.PartVariant.DisplayNameAr,
                SalePrice = part.UnitSalePrice,
                Quantity = part.Quantity,
                ImageUrl = part.ImageUrl != string.Empty
                    ? part.ImageUrl
                    : part.PhoneModel.ImageUrl != string.Empty
                        ? part.PhoneModel.ImageUrl
                        : part.Brand.ImageUrl,
                ThumbnailUrl = part.ThumbnailUrl != string.Empty
                    ? part.ThumbnailUrl
                    : part.PhoneModel.ThumbnailUrl != string.Empty
                        ? part.PhoneModel.ThumbnailUrl
                        : part.Brand.ThumbnailUrl != string.Empty
                            ? part.Brand.ThumbnailUrl
                            : part.PhoneModel.ImageUrl != string.Empty
                                ? part.PhoneModel.ImageUrl
                                : part.Brand.ImageUrl,
                BrandId = part.BrandId,
                BrandName = part.Brand.Name,
                BrandDisplayNameAr = part.Brand.DisplayNameAr,
                ModelId = part.PhoneModelId,
                ModelName = part.PhoneModel.Name,
                ModelDisplayNameAr = part.PhoneModel.DisplayNameAr,
                PartTypeId = part.PartTypeId,
                PartTypeName = part.PartType.Name,
                PartTypeDisplayNameAr = part.PartType.DisplayNameAr
            })
            .SingleOrDefaultAsync();
    }

    public async Task<PublicSearchPageViewModel> SearchAsync(
        string? query, int? brandId, int? modelId, int? partTypeId)
    {
        string normalizedQuery = query?.Trim() ?? string.Empty;

        IQueryable<InventoryPart> partsQuery = dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.Brand.IsActive && part.PhoneModel.IsActive && part.PartType.IsActive && part.PartVariant.IsActive);

        if (!string.IsNullOrEmpty(normalizedQuery))
        {
            string like = $"%{normalizedQuery}%";
            partsQuery = partsQuery.Where(part =>
                EF.Functions.Like(part.PartVariant.DisplayNameAr, like)
                || EF.Functions.Like(part.PartVariant.Name, like)
                || EF.Functions.Like(part.PhoneModel.DisplayNameAr, like)
                || EF.Functions.Like(part.PhoneModel.Name, like)
                || EF.Functions.Like(part.Brand.DisplayNameAr, like)
                || EF.Functions.Like(part.Brand.Name, like));
        }

        if (brandId.HasValue && brandId.Value > 0)
        {
            int target = brandId.Value;
            partsQuery = partsQuery.Where(part => part.BrandId == target);
        }
        if (modelId.HasValue && modelId.Value > 0)
        {
            int target = modelId.Value;
            partsQuery = partsQuery.Where(part => part.PhoneModelId == target);
        }
        if (partTypeId.HasValue && partTypeId.Value > 0)
        {
            int target = partTypeId.Value;
            partsQuery = partsQuery.Where(part => part.PartTypeId == target);
        }

        IReadOnlyList<PublicSearchResultCard> results = await partsQuery
            .OrderBy(part => part.Brand.SortOrder).ThenBy(part => part.Brand.Name)
            .ThenBy(part => part.PhoneModel.SortOrder).ThenBy(part => part.PhoneModel.Name)
            .ThenBy(part => part.PartType.SortOrder).ThenBy(part => part.PartType.Name)
            .ThenBy(part => part.PartVariant.SortOrder).ThenBy(part => part.PartVariant.Name)
            .Take(60)
            .Select(part => new PublicSearchResultCard
            {
                InventoryPartId = part.Id,
                BrandId = part.BrandId,
                BrandName = part.Brand.Name,
                BrandDisplayNameAr = part.Brand.DisplayNameAr,
                ModelId = part.PhoneModelId,
                ModelName = part.PhoneModel.Name,
                ModelDisplayNameAr = part.PhoneModel.DisplayNameAr,
                PartTypeId = part.PartTypeId,
                PartTypeName = part.PartType.Name,
                PartTypeDisplayNameAr = part.PartType.DisplayNameAr,
                VariantName = part.PartVariant.Name,
                VariantDisplayNameAr = part.PartVariant.DisplayNameAr,
                PartDisplayName = $"{part.Brand.DisplayNameAr} {part.PhoneModel.DisplayNameAr} — {part.PartType.DisplayNameAr} — {part.PartVariant.DisplayNameAr}",
                SalePrice = part.UnitSalePrice,
                Quantity = part.Quantity,
                ImageUrl = part.ImageUrl != string.Empty
                    ? part.ImageUrl
                    : part.PhoneModel.ImageUrl != string.Empty
                        ? part.PhoneModel.ImageUrl
                        : part.Brand.ImageUrl,
                ThumbnailUrl = part.ThumbnailUrl != string.Empty
                    ? part.ThumbnailUrl
                    : part.PhoneModel.ThumbnailUrl != string.Empty
                        ? part.PhoneModel.ThumbnailUrl
                        : part.Brand.ThumbnailUrl
            })
            .ToListAsync();

        PublicSearchFilterOptions filters = await GetFilterOptionsAsync(brandId, modelId);

        return new PublicSearchPageViewModel
        {
            Query = normalizedQuery,
            BrandId = brandId,
            ModelId = modelId,
            PartTypeId = partTypeId,
            Results = results,
            FilterOptions = filters
        };
    }

    public async Task<PublicSearchFilterOptions> GetFilterOptionsAsync(int? brandId, int? modelId)
    {
        var brands = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.SortOrder).ThenBy(brand => brand.Name)
            .Select(brand => new PublicSearchFilterItem
            {
                Id = brand.Id,
                Label = brand.Name,
                LabelAr = brand.DisplayNameAr
            })
            .ToListAsync();

        IReadOnlyList<PublicSearchFilterItem> models = brandId is int bid && bid > 0
            ? await dbContext.PhoneModels
                .AsNoTracking()
                .Where(model => model.BrandId == bid && model.IsActive)
                .OrderBy(model => model.SortOrder).ThenBy(model => model.Name)
                .Select(model => new PublicSearchFilterItem
                {
                    Id = model.Id,
                    Label = model.Name,
                    LabelAr = model.DisplayNameAr
                })
                .ToListAsync()
            : [];

        IReadOnlyList<PublicSearchFilterItem> partTypes =
            brandId is int b2 && b2 > 0 && modelId is int mid && mid > 0
                ? await dbContext.PartTypes
                    .AsNoTracking()
                    .Where(type => type.IsActive
                        && dbContext.InventoryParts.Any(part =>
                            part.IsStocked && part.Quantity > 0
                            && part.BrandId == b2
                            && part.PhoneModelId == mid
                            && part.PartTypeId == type.Id))
                    .OrderBy(type => type.SortOrder).ThenBy(type => type.Name)
                    .Select(type => new PublicSearchFilterItem
                    {
                        Id = type.Id,
                        Label = type.Name,
                        LabelAr = type.DisplayNameAr
                    })
                    .ToListAsync()
                : [];

        return new PublicSearchFilterOptions
        {
            Brands = brands,
            Models = models,
            PartTypes = partTypes
        };
    }

    public async Task<IReadOnlyList<PublicSearchFilterItem>> GetActiveModelsForBrandAsync(int brandId)
    {
        if (brandId <= 0) { return []; }
        return await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.BrandId == brandId && model.IsActive)
            .OrderBy(model => model.SortOrder).ThenBy(model => model.Name)
            .Select(model => new PublicSearchFilterItem
            {
                Id = model.Id,
                Label = model.Name,
                LabelAr = model.DisplayNameAr
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PublicSearchFilterItem>> GetActivePartTypesForModelAsync(int brandId, int modelId)
    {
        if (brandId <= 0 || modelId <= 0) { return []; }
        return await dbContext.PartTypes
            .AsNoTracking()
            .Where(type => type.IsActive
                && dbContext.InventoryParts.Any(part =>
                    part.IsStocked && part.Quantity > 0
                    && part.BrandId == brandId
                    && part.PhoneModelId == modelId
                    && part.PartTypeId == type.Id))
            .OrderBy(type => type.SortOrder).ThenBy(type => type.Name)
            .Select(type => new PublicSearchFilterItem
            {
                Id = type.Id,
                Label = type.Name,
                LabelAr = type.DisplayNameAr
            })
            .ToListAsync();
    }
}