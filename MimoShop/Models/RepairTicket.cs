using MimoShop.Services;

namespace MimoShop.Models;
public sealed class RepairTicket
{
    public int Id { get; set; }

    public string JobCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public string DeviceBrand { get; set; } = string.Empty;

    public string DeviceModel { get; set; } = string.Empty;

    public string ProblemDescription { get; set; } = string.Empty;

    public string AssignedWorkerUsername { get; set; } = string.Empty;

    public string AssignedWorkerName { get; set; } = string.Empty;

    public decimal EstimatedPrice { get; set; }

    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = RepairJobStatuses.New;

    public ICollection<RepairStatusHistory> StatusHistory { get; set; } = [];

    public ICollection<RepairPartUsage> PartUsages { get; set; } = [];
}
