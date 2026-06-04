using System.ComponentModel.DataAnnotations;

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
    [Display(Name = "العلامة")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "الموديل مطلوب")]
    [Display(Name = "الموديل")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع القطعة مطلوب")]
    [Display(Name = "نوع القطعة")]
    public string PartType { get; set; } = string.Empty;

    [Required(ErrorMessage = "النوعية مطلوبة")]
    [Display(Name = "النوعية")]
    public string Variant { get; set; } = string.Empty;

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
}
