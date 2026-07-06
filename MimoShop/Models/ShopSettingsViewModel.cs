using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MimoShop.Models;

public sealed class ShopSettingsViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المحل (بالعربية) مطلوب")]
    [Display(Name = "اسم المحل (بالعربية)")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم (لاتيني) مطلوب")]
    [Display(Name = "الاسم (لاتيني)")]
    [StringLength(120)]
    public string LatinName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Display(Name = "رقم الهاتف")]
    [StringLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "واتساب")]
    [StringLength(40)]
    public string? WhatsApp { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب")]
    [Display(Name = "العنوان")]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "معرف بوت التيليغرام")]
    [StringLength(80)]
    public string? TelegramHandle { get; set; }

    [Display(Name = "ساعات العمل")]
    [StringLength(200)]
    public string? OpeningHours { get; set; }

    [Display(Name = "شعار المحل")]
    public IFormFile? LogoFile { get; set; }

    [Display(Name = "الشعار الحالي")]
    public string? ExistingLogoUrl { get; set; }

    [Display(Name = "إزالة الشعار")]
    public bool RemoveLogo { get; set; }
}