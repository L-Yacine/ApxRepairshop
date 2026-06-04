namespace MimoShop.Models;

public sealed class RepairStatusHistory
{
    public int Id { get; set; }

    public int RepairTicketId { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;

    public string Status { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public string ChangedByUsername { get; set; } = string.Empty;
}
