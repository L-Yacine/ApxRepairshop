using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class ShopOrderLine
{
    public int Id { get; set; }

    public int ShopOrderId { get; set; }

    public ShopOrder? ShopOrder { get; set; }

    public int? InventoryPartId { get; set; }

    public InventoryPart? InventoryPart { get; set; }

    [Required]
    [StringLength(60)]
    public string BrandName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string ModelName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string PartTypeName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string VariantName { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string PartDisplayName { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "الكمية يجب أن تكون 1 على الأقل")]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}