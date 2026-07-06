using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class CatalogImageFetchService
{
    private readonly MimoShopDbContext dbContext;
    private readonly GsmArenaCatalogImageProvider gsmArenaProvider;
    private readonly IFixitCatalogImageProvider ifixitProvider;
    private readonly CatalogImageService catalogImageService;

    public CatalogImageFetchService(
        MimoShopDbContext dbContext,
        GsmArenaCatalogImageProvider gsmArenaProvider,
        IFixitCatalogImageProvider ifixitProvider,
        CatalogImageService catalogImageService)
    {
        this.dbContext = dbContext;
        this.gsmArenaProvider = gsmArenaProvider;
        this.ifixitProvider = ifixitProvider;
        this.catalogImageService = catalogImageService;
    }

    public async Task<IReadOnlyList<CatalogImageCandidate>> SearchPhoneModelImagesAsync(
        int phoneModelId,
        string? query,
        CancellationToken cancellationToken)
    {
        PhoneModel? phoneModel = await dbContext.PhoneModels
            .AsNoTracking()
            .Include(model => model.Brand)
            .SingleOrDefaultAsync(model => model.Id == phoneModelId, cancellationToken);

        if (phoneModel is null)
        {
            return [];
        }

        var request = new CatalogImageSearchRequest(
            "PhoneModel",
            phoneModel.Brand.Name,
            phoneModel.Name,
            null,
            null,
            query);

        return await gsmArenaProvider.SearchAsync(request, cancellationToken);
    }

    public async Task<AppliedCatalogImageResult?> ApplyPhoneModelImageAsync(
        int phoneModelId,
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        PhoneModel? phoneModel = await dbContext.PhoneModels
            .Include(model => model.Brand)
            .SingleOrDefaultAsync(model => model.Id == phoneModelId, cancellationToken);

        if (phoneModel is null)
        {
            return null;
        }

        Uri? fullImageUrl = await gsmArenaProvider.GetFullImageUrlAsync(sourceUrl, cancellationToken);
        if (fullImageUrl is null)
        {
            throw new CatalogImageException("لم يتم العثور على صورة مناسبة لهذا الموديل.");
        }

        await using Stream imageStream = await gsmArenaProvider.DownloadImageAsync(fullImageUrl, cancellationToken);
        string originalFileName = Path.GetFileName(fullImageUrl.LocalPath);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = "gsmarena-phone.jpg";
        }

        CatalogImageResult image = await catalogImageService.SaveAsync(
            imageStream,
            originalFileName,
            "model",
            $"{phoneModel.Brand.Name}-{phoneModel.Name}");

        catalogImageService.DeleteStoredImage(phoneModel.ImageUrl, phoneModel.ThumbnailUrl);
        phoneModel.ImageUrl = image.ImageUrl;
        phoneModel.ThumbnailUrl = image.ThumbnailUrl;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AppliedCatalogImageResult(image.ImageUrl, image.ThumbnailUrl);
    }

    public async Task<IReadOnlyList<CatalogImageCandidate>> SearchPartImagesAsync(
        int inventoryPartId,
        string? query,
        CancellationToken cancellationToken)
    {
        InventoryPart? part = await dbContext.InventoryParts
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.PhoneModel)
            .Include(p => p.PartType)
            .SingleOrDefaultAsync(p => p.Id == inventoryPartId, cancellationToken);

        if (part is null)
        {
            return [];
        }

        var request = new CatalogImageSearchRequest(
            "InventoryPart",
            part.Brand.Name,
            part.PhoneModel.Name,
            part.PartType.Name,
            null,
            query);

        return await ifixitProvider.SearchAsync(request, cancellationToken);
    }

    public async Task<AppliedCatalogImageResult?> ApplyPartImageAsync(
        int inventoryPartId,
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        InventoryPart? part = await dbContext.InventoryParts
            .Include(p => p.Brand)
            .Include(p => p.PhoneModel)
            .Include(p => p.PartType)
            .SingleOrDefaultAsync(p => p.Id == inventoryPartId, cancellationToken);

        if (part is null)
        {
            return null;
        }

        Uri? fullImageUrl = await ifixitProvider.GetFullImageUrlAsync(sourceUrl, cancellationToken);
        if (fullImageUrl is null)
        {
            throw new CatalogImageException("لم يتم العثور على صورة مناسبة لهذه القطعة.");
        }

        await using Stream imageStream = await ifixitProvider.DownloadImageAsync(fullImageUrl, cancellationToken);
        string originalFileName = Path.GetFileName(fullImageUrl.LocalPath);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = "ifixit-part.jpg";
        }

        CatalogImageResult image = await catalogImageService.SaveAsync(
            imageStream,
            originalFileName,
            "part",
            $"{part.Brand.Name}-{part.PhoneModel.Name}-{part.PartType.Name}");

        catalogImageService.DeleteStoredImage(part.ImageUrl, part.ThumbnailUrl);
        part.ImageUrl = image.ImageUrl;
        part.ThumbnailUrl = image.ThumbnailUrl;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AppliedCatalogImageResult(image.ImageUrl, image.ThumbnailUrl);
    }
}
