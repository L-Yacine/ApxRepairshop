using System.ComponentModel.DataAnnotations;

namespace MimoShop.Models;

public sealed class RepairPaymentUpdateViewModel
{
    [Range(0, 10000000, ErrorMessage = "السعر يجب أن يكون رقماً موجباً")]
    [Display(Name = "السعر المتفق عليه (دج)")]
    public decimal EstimatedPrice { get; set; }

    [Range(0, 10000000, ErrorMessage = "المبلغ المدفوع يجب أن يكون رقماً موجباً")]
    [Display(Name = "المبلغ المدفوع (دج)")]
    public decimal AmountPaid { get; set; }
}

public sealed class RepairPartSelectionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "اختر قطعة")]
    public int InventoryPartId { get; set; }

    [Range(1, 100000, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public int Quantity { get; set; } = 1;
}
