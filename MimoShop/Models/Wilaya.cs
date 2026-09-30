using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class Wilaya
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرمز مطلوب")]
    [Display(Name = "الرمز")]
    [StringLength(2, ErrorMessage = "الرمز يجب أن يكون من حرفين")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم بالعربية مطلوب")]
    [Display(Name = "الاسم بالعربية")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم بالفرنسية مطلوب")]
    [Display(Name = "الاسم بالفرنسية")]
    [StringLength(100)]
    public string NameFr { get; set; } = string.Empty;

    [Display(Name = "رسوم التوصيل (دج)")]
    public decimal ShippingFee { get; set; }

    [Display(Name = "مفعّلة")]
    public bool IsActive { get; set; } = true;

    public ICollection<Commune> Communes { get; set; } = [];
}