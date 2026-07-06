using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class InventoryPart
{
    public int Id { get; set; }

    [Required(ErrorMessage = "العلامة مطلوبة")]
    [Display(Name = "العلامة")]
    public int BrandId { get; set; }

    public Brand Brand { get; set; } = null!;

    [Required(ErrorMessage = "الموديل مطلوب")]
    [Display(Name = "الموديل")]
    public int PhoneModelId { get; set; }

    public PhoneModel PhoneModel { get; set; } = null!;

    [Required(ErrorMessage = "نوع القطعة مطلوب")]
    [Display(Name = "نوع القطعة")]
    public int PartTypeId { get; set; }

    public PartType PartType { get; set; } = null!;

    [Required(ErrorMessage = "النوعية مطلوبة")]
    [Display(Name = "النوعية")]
    public int PartVariantId { get; set; }

    public PartVariant PartVariant { get; set; } = null!;

    [Display(Name = "صورة القطعة")]
    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

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

    public DateTime UpdatedAt { get; set; }

    public ICollection<RepairPartUsage> RepairPartUsages { get; set; } = [];
}
