using Microsoft.AspNetCore.Http;

namespace MimoShop.Models;

public sealed class CategoriesIndexViewModel
{
    public IReadOnlyList<Brand> Brands { get; set; } = [];

    public IReadOnlyList<PhoneModel> PhoneModels { get; set; } = [];

    public IReadOnlyList<PartType> PartTypes { get; set; } = [];

    public IReadOnlyList<PartVariant> PartVariants { get; set; } = [];

    public BrandFormInput NewBrand { get; set; } = new();

    public PhoneModelFormInput NewPhoneModel { get; set; } = new();

    public PartTypeFormInput NewPartType { get; set; } = new();

    public PartVariantFormInput NewPartVariant { get; set; } = new();
}

public sealed class BrandFormInput
{
    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class PhoneModelFormInput
{
    public int BrandId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class PartTypeFormInput
{
    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class PartVariantFormInput
{
    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class ImportPreviewViewModel
{
    public List<ImportModelGroup> Groups { get; set; } = [];
}

public sealed class ImportModelGroup
{
    public string Brand { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public List<ImportModelEntry> Models { get; set; } = [];
}

public sealed class ImportModelEntry
{
    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;
}
