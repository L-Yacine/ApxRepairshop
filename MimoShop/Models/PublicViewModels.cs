using MimoShop.Localization;
using MimoShop.Services;

namespace MimoShop.Models;

public sealed class PublicBrandCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public string LocalizedName => PublicCulture.Name(DisplayNameAr, Name);
}

public sealed class PublicModelCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string BrandImageUrl { get; set; } = string.Empty;
    public string BrandThumbnailUrl { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string BrandDisplayNameAr { get; set; } = string.Empty;

    public string LocalizedName => PublicCulture.Name(DisplayNameAr, Name);
    public string LocalizedBrandName => PublicCulture.Name(BrandDisplayNameAr, BrandName);
}

public sealed class PublicPartTypeCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ModelImageUrl { get; set; } = string.Empty;
    public string ModelThumbnailUrl { get; set; } = string.Empty;

    public string LocalizedName => PublicCulture.Name(DisplayNameAr, Name);
}

public sealed class PublicVariantCard
{
    public int InventoryPartId { get; set; }
    public int VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string VariantDisplayNameAr { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string BrandDisplayNameAr { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelDisplayNameAr { get; set; } = string.Empty;
    public string PartTypeName { get; set; } = string.Empty;
    public string PartTypeDisplayNameAr { get; set; } = string.Empty;

    public string LocalizedBrandName => PublicCulture.Name(BrandDisplayNameAr, BrandName);
    public string LocalizedModelName => PublicCulture.Name(ModelDisplayNameAr, ModelName);
    public string LocalizedPartTypeName => PublicCulture.Name(PartTypeDisplayNameAr, PartTypeName);
    public string LocalizedVariantName => PublicCulture.Name(VariantDisplayNameAr, VariantName);
    public string LocalizedDisplayName =>
        $"{LocalizedBrandName} {LocalizedModelName} — {LocalizedPartTypeName} — {LocalizedVariantName}";
}

public sealed class PublicPartDetail
{
    public int InventoryPartId { get; set; }
    public int VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string VariantDisplayNameAr { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string BrandDisplayNameAr { get; set; } = string.Empty;
    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelDisplayNameAr { get; set; } = string.Empty;
    public int PartTypeId { get; set; }
    public string PartTypeName { get; set; } = string.Empty;
    public string PartTypeDisplayNameAr { get; set; } = string.Empty;
    public IReadOnlyList<WilayaOption> Wilayas { get; set; } = [];

    public string LocalizedBrandName => PublicCulture.Name(BrandDisplayNameAr, BrandName);
    public string LocalizedModelName => PublicCulture.Name(ModelDisplayNameAr, ModelName);
    public string LocalizedPartTypeName => PublicCulture.Name(PartTypeDisplayNameAr, PartTypeName);
    public string LocalizedVariantName => PublicCulture.Name(VariantDisplayNameAr, VariantName);
    public string LocalizedDisplayName =>
        $"{LocalizedBrandName} {LocalizedModelName} — {LocalizedPartTypeName} — {LocalizedVariantName}";
}

public sealed class PublicLandingViewModel
{
    public string ShopName { get; set; } = string.Empty;
    public string ShopLatinName { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public string ShopPhone { get; set; } = string.Empty;
    public string? ShopWhatsApp { get; set; }
    public string? ShopTelegramHandle { get; set; }
    public string ShopAddress { get; set; } = string.Empty;
    public string? OpeningHours { get; set; }
    public IReadOnlyList<PublicBrandCard> Brands { get; set; } = [];
    public IReadOnlyList<PublicPartTypeCard> Categories { get; set; } = [];
    public IReadOnlyList<PublicVariantCard> NewestParts { get; set; } = [];
    public IReadOnlyList<HeroSlide> HeroSlides { get; set; } = [];
}

public sealed class CartPageViewModel
{
    public IReadOnlyList<CartItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public int ItemCount { get; set; }
}