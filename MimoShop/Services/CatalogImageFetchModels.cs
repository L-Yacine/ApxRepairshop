namespace MimoShop.Services;

public interface ICatalogImageProvider
{
    Task<IReadOnlyList<CatalogImageCandidate>> SearchAsync(
        CatalogImageSearchRequest request,
        CancellationToken cancellationToken);
}

public sealed record CatalogImageSearchRequest(
    string EntityType,
    string BrandName,
    string? ModelName,
    string? PartTypeName,
    string? VariantName,
    string? Query);

public sealed record CatalogImageCandidate(
    string Title,
    string SourceName,
    string SourceUrl,
    string ImageUrl,
    int? Width,
    int? Height);

public sealed record AppliedCatalogImageResult(
    string ImageUrl,
    string ThumbnailUrl);
