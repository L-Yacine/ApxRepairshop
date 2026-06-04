using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class RepairIntakeViewModel
{
    [Required(ErrorMessage = "رقم هاتف العميل مطلوب")]
    [Display(Name = "رقم هاتف العميل")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم العميل مطلوب")]
    [Display(Name = "اسم العميل")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "واتساب")]
    public string? CustomerWhatsApp { get; set; }

    [Display(Name = "حساب تيليغرام")]
    public string? CustomerTelegram { get; set; }

    [Required(ErrorMessage = "علامة الجهاز مطلوبة")]
    [Display(Name = "علامة الجهاز")]
    public string DeviceBrand { get; set; } = string.Empty;

    [Required(ErrorMessage = "موديل الجهاز مطلوب")]
    [Display(Name = "موديل الجهاز")]
    public string DeviceModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "وصف المشكل مطلوب")]
    [Display(Name = "وصف المشكل")]
    public string ProblemDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "العامل المسؤول مطلوب")]
    [Display(Name = "العامل المسؤول")]
    public string AssignedWorkerUsername { get; set; } = string.Empty;

    [Range(0, 10_000_000, ErrorMessage = "السعر التقديري يجب أن يكون رقماً موجباً")]
    [Display(Name = "السعر التقديري (دج)")]
    public decimal EstimatedPrice { get; set; }

    [Display(Name = "ملاحظات")]
    public string? Notes { get; set; }

    public IReadOnlyList<DeviceBrandOption> DeviceOptions { get; set; } = [];

    public IReadOnlyList<StaffOption> StaffOptions { get; set; } = [];
}

public sealed record DeviceBrandOption(string Brand, IReadOnlyList<string> Models);

public sealed record StaffOption(string Username, string DisplayName);
