using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MimoShop.Models;

public sealed class HeroSlideFormInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب")]
    [StringLength(160, ErrorMessage = "العنوان طويل جداً")]
    [Display(Name = "العنوان")]
    public string Title { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "النص الفرعي طويل جداً")]
    [Display(Name = "النص الفرعي")]
    public string Subtitle { get; set; } = string.Empty;

    [Display(Name = "صورة الشريحة")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "الصورة الحالية")]
    public string? ExistingImageUrl { get; set; }

    [Display(Name = "إزالة الصورة")]
    public bool RemoveImage { get; set; }

    [StringLength(80, ErrorMessage = "نص الزر طويل جداً")]
    [Display(Name = "نص الزر")]
    public string CtaText { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "رابط الزر طويل جداً")]
    [Display(Name = "رابط الزر")]
    public string CtaUrl { get; set; } = string.Empty;

    [Display(Name = "ترتيب العرض")]
    public int SortOrder { get; set; }

    [Display(Name = "مفعّلة")]
    public bool IsActive { get; set; } = true;
}
