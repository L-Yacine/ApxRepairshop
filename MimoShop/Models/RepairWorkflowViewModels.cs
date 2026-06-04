namespace MimoShop.Models;

public sealed class RepairTicketListViewModel
{
    public IReadOnlyList<RepairTicketListItemViewModel> ActiveJobs { get; set; } = [];

    public IReadOnlyList<RepairTicketListItemViewModel> WaitingForPartJobs { get; set; } = [];

    public IReadOnlyList<RepairTicketListItemViewModel> CollectedJobs { get; set; } = [];
}

public sealed class RepairTicketListItemViewModel
{
    public string JobCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string ProblemDescription { get; set; } = string.Empty;

    public string AssignedWorkerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime? LastStatusChangedAt { get; set; }
}

public sealed class RepairTicketDetailsViewModel
{
    public string JobCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string ProblemDescription { get; set; } = string.Empty;

    public string AssignedWorkerName { get; set; } = string.Empty;

    public decimal EstimatedPrice { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal BalanceOwed { get; set; }

    public string PaymentStatusLabel { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public IReadOnlyList<RepairStatusOptionViewModel> StatusOptions { get; set; } = [];

    public IReadOnlyList<RepairStatusHistoryViewModel> StatusHistory { get; set; } = [];

    public IReadOnlyList<RepairPartOptionViewModel> StockedPartOptions { get; set; } = [];

    public IReadOnlyList<RepairPartOptionViewModel> OnDemandPartOptions { get; set; } = [];

    public IReadOnlyList<RepairPartUsageViewModel> PartUsages { get; set; } = [];
}

public sealed class RepairStatusOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public sealed class RepairStatusHistoryViewModel
{
    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public string ChangedByUsername { get; set; } = string.Empty;
}

public sealed class RepairPartOptionViewModel
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitSalePrice { get; set; }

    public bool IsStocked { get; set; }
}

public sealed class RepairPartUsageViewModel
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitSalePrice { get; set; }

    public bool IsOnDemand { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public string CreatedByUsername { get; set; } = string.Empty;
}
