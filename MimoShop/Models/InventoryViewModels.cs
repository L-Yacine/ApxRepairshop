using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MimoShop.Models;

public sealed class InventoryIndexViewModel
{
    public IReadOnlyList<InventoryPartListItemViewModel> Parts { get; set; } = [];
}

public sealed class InventoryPartListItemViewModel
{
    public int Id { get; set; }

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string PartType { get; set; } = string.Empty;

    public string Variant { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public string BrandImageUrl { get; set; } = string.Empty;

    public string BrandThumbnailUrl { get; set; } = string.Empty;

    public string ModelImageUrl { get; set; } = string.Empty;

    public string ModelThumbnailUrl { get; set; } = string.Empty;

    public string PartTypeImageUrl { get; set; } = string.Empty;

    public string PartTypeThumbnailUrl { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCostPrice { get; set; }

    public decimal UnitSalePrice { get; set; }

    public bool IsStocked { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class InventoryPartFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "العلامة مطلوبة")]
    [Range(1, int.MaxValue, ErrorMessage = "اختر العلامة من القائمة")]
    [Display(Name = "العلامة")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "الموديل مطلوب")]
    [Range(1, int.MaxValue, ErrorMessage = "اختر الموديل من القائمة")]
    [Display(Name = "الموديل")]
    public int PhoneModelId { get; set; }

    [Required(ErrorMessage = "نوع القطعة مطلوب")]
    [Range(1, int.MaxValue, ErrorMessage = "اختر نوع القطعة من القائمة")]
    [Display(Name = "نوع القطعة")]
    public int PartTypeId { get; set; }

    [Required(ErrorMessage = "النوعية مطلوبة")]
    [Range(1, int.MaxValue, ErrorMessage = "اختر النوعية من القائمة")]
    [Display(Name = "النوعية")]
    public int PartVariantId { get; set; }

    [Display(Name = "صورة القطعة")]
    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; set; }

    [Range(0, 100000, ErrorMessage = "الكمية يجب أن تكون رقماً موجباً")]
    [Display(Name = "الكمية")]
    public int Quantity { get; set; }

    [Range(0, 10000000, ErrorMessage = "سعر التكلفة يجب أن يكون رقماً موجباً")]
    [Display(Name = "سعر التكلفة (دج)")]
    public decimal UnitCostPrice { get; set; }

    [Range(0, 10000000, ErrorMessage = "سعر البيع يجب أن يكون رقماً موجباً")]
    [Display(Name = "سعر البيع (دج)")]
    public decimal UnitSalePrice { get; set; }

    [Display(Name = "متوفرة في المخزون")]
    public bool IsStocked { get; set; } = true;

    public InventoryCategoryOptionsViewModel Options { get; set; } = new();
}

public sealed class InventoryCategoryOptionsViewModel
{
    public IReadOnlyList<LookupOptionViewModel> Brands { get; set; } = [];

    public IReadOnlyList<LookupOptionViewModel> PhoneModels { get; set; } = [];

    public IReadOnlyList<LookupOptionViewModel> PartTypes { get; set; } = [];

    public IReadOnlyList<LookupOptionViewModel> PartVariants { get; set; } = [];
}

public sealed class LookupOptionViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public int? ParentId { get; set; }
}
