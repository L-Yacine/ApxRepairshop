namespace MimoShop.Models;

public sealed class PublicRepairStatusPageViewModel
{
    public string JobCodeInput { get; set; } = string.Empty;
    public PublicRepairStatusCard? Result { get; set; }
    public string? Message { get; set; }
    public string? MessageKind { get; set; }
}

public sealed class PublicRepairStatusCard
{
    public string JobCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string AssignedWorkerName { get; set; } = string.Empty;
    public string? ProblemDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastStatusChangedAt { get; set; }
}
