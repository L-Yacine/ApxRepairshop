using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class QuickOrderForm
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [StringLength(120, ErrorMessage = "الاسم طويل جداً")]
    [Display(Name = "الاسم الكامل")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [StringLength(40, MinimumLength = 7, ErrorMessage = "رقم الهاتف غير صالح")]
    [RegularExpression(@"^[0-9 +\-]{7,40}$", ErrorMessage = "رقم الهاتف غير صالح")]
    [Display(Name = "رقم الهاتف")]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "واتساب (اختياري)")]
    public string? CustomerWhatsApp { get; set; }

    [Range(1, 99, ErrorMessage = "الكمية يجب أن تكون بين 1 و 99")]
    [Display(Name = "الكمية")]
    public int Quantity { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار الولاية")]
    [Display(Name = "الولاية")]
    public int WilayaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار البلدية")]
    [Display(Name = "البلدية")]
    public int CommuneId { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب")]
    [StringLength(300, ErrorMessage = "العنوان طويل جداً")]
    [Display(Name = "العنوان")]
    public string Address { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "الملاحظات طويلة جداً")]
    [Display(Name = "ملاحظات (اختياري)")]
    public string? Notes { get; set; }
}
