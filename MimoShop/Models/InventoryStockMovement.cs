namespace MimoShop.Models;

public sealed class InventoryStockMovement
{
    public int Id { get; set; }

    public int InventoryPartId { get; set; }

    public InventoryPart InventoryPart { get; set; } = null!;

    public int? RepairTicketId { get; set; }

    public RepairTicket? RepairTicket { get; set; }

    public int? RepairPartUsageId { get; set; }

    public RepairPartUsage? RepairPartUsage { get; set; }

    public int QuantityChange { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string CreatedByUsername { get; set; } = string.Empty;
}
