namespace MimoShop.Models;

public sealed class RepairReceiptViewModel
{
    public required string ShopName { get; init; }

    public required string ShopContact { get; init; }

    public required string JobCode { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerPhone { get; init; }

    public required string DeviceBrand { get; init; }

    public required string DeviceModel { get; init; }

    public required string ProblemDescription { get; init; }

    public required decimal EstimatedPrice { get; init; }

    public required string AssignedWorkerName { get; init; }

    public string? Notes { get; init; }
}
