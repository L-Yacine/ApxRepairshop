namespace MimoShop.Models;

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? WhatsApp { get; set; }

    public string? Telegram { get; set; }

    public ICollection<RepairTicket> RepairTickets { get; set; } = [];
}
