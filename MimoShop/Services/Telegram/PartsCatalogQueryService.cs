using Microsoft.EntityFrameworkCore;
using MimoShop.Data;

namespace MimoShop.Services.Telegram;

public sealed class PartsCatalogQueryService
{
    public const int PageSize = 8;

    private readonly MimoShopDbContext dbContext;

    public PartsCatalogQueryService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<PaginatedResult<LookupItem>> GetBrandsWithStockAsync(int page)
    {
        IQueryable<LookupItem> query = dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.Brand.IsActive)
            .Select(part => new
            {
                part.BrandId,
                part.Brand.Name,
                part.Brand.SortOrder
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.BrandId, x.Name, string.Empty, string.Empty));

        return PaginateAsync(query, page);
    }

    public Task<PaginatedResult<ModelLookupItem>> GetModelsWithStockAsync(int brandId, int page)
    {
        IQueryable<ModelLookupItem> query = dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModel.IsActive)
            .Select(part => new
            {
                part.PhoneModelId,
                part.PhoneModel.Name,
                part.PhoneModel.SortOrder,
                BrandName = part.Brand.Name,
                BrandImageUrl = part.Brand.ImageUrl,
                BrandThumbnailUrl = part.Brand.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ModelLookupItem(
                x.PhoneModelId,
                x.Name,
                x.BrandName,
                x.BrandImageUrl,
                x.BrandThumbnailUrl));

        return PaginateAsync(query, page);
    }

    public Task<PaginatedResult<PartTypeLookupItem>> GetPartTypesWithStockAsync(int brandId, int modelId, int page)
    {
        IQueryable<PartTypeLookupItem> query = dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModelId == modelId && part.PhoneModel.IsActive)
            .Where(part => part.PartType.IsActive)
            .Select(part => new
            {
                part.PartTypeId,
                part.PartType.Name,
                part.PartType.SortOrder,
                ModelName = part.PhoneModel.Name,
                ModelImageUrl = part.PhoneModel.ImageUrl,
                ModelThumbnailUrl = part.PhoneModel.ThumbnailUrl,
                BrandName = part.Brand.Name,
                BrandImageUrl = part.Brand.ImageUrl,
                BrandThumbnailUrl = part.Brand.ThumbnailUrl
            })
            .Distinct()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PartTypeLookupItem(
                x.PartTypeId,
                x.Name,
                x.ModelName,
                x.ModelImageUrl,
                x.ModelThumbnailUrl,
                x.BrandName,
                x.BrandImageUrl,
                x.BrandThumbnailUrl));

        return PaginateAsync(query, page);
    }

    public Task<PaginatedResult<VariantItem>> GetVariantsAsync(int brandId, int modelId, int partTypeId, int page)
    {
        IQueryable<VariantItem> query = dbContext.InventoryParts
            .AsNoTracking()
            .Where(part => part.IsStocked && part.Quantity > 0)
            .Where(part => part.BrandId == brandId && part.Brand.IsActive)
            .Where(part => part.PhoneModelId == modelId && part.PhoneModel.IsActive)
            .Where(part => part.PartTypeId == partTypeId && part.PartType.IsActive)
            .Where(part => part.PartVariant.IsActive)
            .OrderBy(part => part.PartVariant.SortOrder)
            .ThenBy(part => part.PartVariant.Name)
            .Select(part => new VariantItem(
                part.PartVariantId,
                part.PartVariant.Name,
                part.UnitSalePrice,
                part.ImageUrl != string.Empty
                    ? part.ImageUrl
                    : part.PhoneModel.ImageUrl != string.Empty
                        ? part.PhoneModel.ImageUrl
                        : part.Brand.ImageUrl,
                part.ThumbnailUrl != string.Empty
                    ? part.ThumbnailUrl
                    : part.PhoneModel.ThumbnailUrl != string.Empty
                        ? part.PhoneModel.ThumbnailUrl
                        : part.Brand.ThumbnailUrl != string.Empty
                            ? part.Brand.ThumbnailUrl
                            : part.PhoneModel.ImageUrl != string.Empty
                                ? part.PhoneModel.ImageUrl
                                : part.Brand.ImageUrl,
                part.Brand.Name,
                part.PhoneModel.Name,
                part.PartType.Name));

        return PaginateAsync(query, page);
    }

    private static async Task<PaginatedResult<T>> PaginateAsync<T>(IQueryable<T> query, int page)
    {
        int safePage = page < 1 ? 1 : page;
        int totalCount = await query.CountAsync();
        List<T> items = await query
            .Skip((safePage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        return new PaginatedResult<T>(items, totalCount, safePage, PageSize);
    }
}

public sealed record LookupItem(int Id, string Name, string ImageUrl, string ThumbnailUrl);

public sealed record ModelLookupItem(
    int Id,
    string Name,
    string BrandName,
    string BrandImageUrl,
    string BrandThumbnailUrl);

public sealed record PartTypeLookupItem(
    int Id,
    string Name,
    string ModelName,
    string ModelImageUrl,
    string ModelThumbnailUrl,
    string BrandName,
    string BrandImageUrl,
    string BrandThumbnailUrl);

public sealed record VariantItem(
    int Id,
    string Name,
    decimal SalePrice,
    string ImageUrl,
    string ThumbnailUrl,
    string BrandName,
    string ModelName,
    string PartTypeName);

public sealed record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}