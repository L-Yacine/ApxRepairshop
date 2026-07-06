using System.ComponentModel.DataAnnotations;
using MimoShop.Services;

namespace MimoShop.Models;

public sealed class StaffListViewModel
{
    public required IReadOnlyCollection<StaffAccount> Staff { get; set; }
}

public sealed class CreateStaffViewModel
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [Display(Name = "اسم المستخدم")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم المعروض مطلوب")]
    [Display(Name = "الاسم المعروض")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "الصفة مطلوبة")]
    [Display(Name = "الصفة")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "كلمة المرور يجب أن تكون بين 4 و 100 حرف")]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "كلمة المرور غير متطابقة")]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class EditStaffViewModel
{
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم المعروض مطلوب")]
    [Display(Name = "الاسم المعروض")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "الصفة مطلوبة")]
    [Display(Name = "الصفة")]
    public string Role { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "كلمة المرور يجب أن تكون بين 4 و 100 حرف")]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور غير متطابقة")]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الحالية")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "كلمة المرور يجب أن تكون بين 4 و 100 حرف")]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور غير متطابقة")]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
