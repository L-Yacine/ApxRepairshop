using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class ShopOrder
{
    public int Id { get; set; }

    [Required]
    [StringLength(12)]
    public string OrderCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الزبون مطلوب")]
    [StringLength(120)]
    [Display(Name = "اسم الزبون")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [StringLength(40)]
    [Display(Name = "رقم الهاتف")]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "واتساب")]
    public string? CustomerWhatsApp { get; set; }

    public int WilayaId { get; set; }

    public Wilaya? Wilaya { get; set; }

    public int CommuneId { get; set; }

    public Commune? Commune { get; set; }

    [Required]
    [StringLength(100)]
    public string WilayaName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CommuneName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CommuneNameFr { get; set; } = string.Empty;

    [Required(ErrorMessage = "العنوان مطلوب")]
    [StringLength(300)]
    [Display(Name = "العنوان")]
    public string Address { get; set; } = string.Empty;

    public int? ShipmentId { get; set; }

    [StringLength(500)]
    [Display(Name = "ملاحظات")]
    public string? Notes { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = ShopOrderStatuses.New;

    [Required]
    public decimal Subtotal { get; set; }

    [Required]
    public decimal ShippingFee { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReturnedAt { get; set; }

    [StringLength(300)]
    public string? ReturnReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    [StringLength(300)]
    public string? CancelledReason { get; set; }

    public ICollection<ShopOrderLine> Lines { get; set; } = [];
}

public static class ShopOrderStatuses
{
    public const string New = "New";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Returned = "Returned";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        New,
        Confirmed,
        Shipped,
        Delivered,
        Returned,
        Cancelled
    ];

    public static string ToArabicLabel(string status) => status switch
    {
        New => "جديد",
        Confirmed => "مؤكد",
        Shipped => "تم الشحن",
        Delivered => "تم التسليم",
        Returned => "مرتجع",
        Cancelled => "ملغى",
        _ => status
    };
}