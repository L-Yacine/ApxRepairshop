namespace MimoShop.Models;

public sealed record CartItem(
    int InventoryPartId,
    string BrandName,
    string ModelName,
    string PartTypeName,
    string VariantName,
    string PartDisplayName,
    decimal UnitPrice,
    string ThumbnailUrl,
    int Quantity);