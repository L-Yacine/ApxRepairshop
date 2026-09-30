using MimoShop.Localization;

namespace MimoShop.Models;

public sealed class PublicSearchPageViewModel
{
    public string Query { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
    public int? PartTypeId { get; set; }

    public IReadOnlyList<PublicSearchResultCard> Results { get; set; } = [];
    public PublicSearchFilterOptions FilterOptions { get; set; } = new();
}

public sealed class PublicSearchResultCard
{
    public int InventoryPartId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string BrandDisplayNameAr { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelDisplayNameAr { get; set; } = string.Empty;
    public int ModelId { get; set; }
    public string PartTypeName { get; set; } = string.Empty;
    public string PartTypeDisplayNameAr { get; set; } = string.Empty;
    public int PartTypeId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string VariantDisplayNameAr { get; set; } = string.Empty;
    public string PartDisplayName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public string LocalizedBrandName => PublicCulture.Name(BrandDisplayNameAr, BrandName);
    public string LocalizedModelName => PublicCulture.Name(ModelDisplayNameAr, ModelName);
    public string LocalizedPartTypeName => PublicCulture.Name(PartTypeDisplayNameAr, PartTypeName);
    public string LocalizedVariantName => PublicCulture.Name(VariantDisplayNameAr, VariantName);
    public string LocalizedDisplayName =>
        $"{LocalizedBrandName} {LocalizedModelName} — {LocalizedPartTypeName} — {LocalizedVariantName}";
}

public sealed class PublicSearchFilterOptions
{
    public IReadOnlyList<PublicSearchFilterItem> Brands { get; set; } = [];
    public IReadOnlyList<PublicSearchFilterItem> Models { get; set; } = [];
    public IReadOnlyList<PublicSearchFilterItem> PartTypes { get; set; } = [];
}

public sealed class PublicSearchFilterItem
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? LabelAr { get; set; }

    public string LocalizedLabel => PublicCulture.Name(LabelAr, Label);
}
