namespace MimoShop.Models;

public sealed class RepairPartUsage
{
    public int Id { get; set; }

    public int RepairTicketId { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;

    public int? InventoryPartId { get; set; }

    public InventoryPart? InventoryPart { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string PhoneModelName { get; set; } = string.Empty;

    public string PartTypeName { get; set; } = string.Empty;

    public string PartVariantName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCostPrice { get; set; }

    public decimal UnitSalePrice { get; set; }

    public bool IsOnDemand { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public string CreatedByUsername { get; set; } = string.Empty;
}
