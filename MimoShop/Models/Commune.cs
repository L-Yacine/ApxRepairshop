using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class Commune
{
    public int Id { get; set; }

    public int WilayaId { get; set; }

    public Wilaya Wilaya { get; set; } = null!;

    [Required(ErrorMessage = "الاسم بالعربية مطلوب")]
    [Display(Name = "الاسم بالعربية")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم بالفرنسية مطلوب")]
    [Display(Name = "الاسم بالفرنسية")]
    [StringLength(100)]
    public string NameFr { get; set; } = string.Empty;

    [Display(Name = "مفعّلة")]
    public bool IsActive { get; set; } = true;
}