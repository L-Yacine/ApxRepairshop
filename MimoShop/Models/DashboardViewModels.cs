namespace MimoShop.Models;

public sealed class OwnerDashboardViewModel
{
    public DateTime GeneratedAt { get; set; }

    public int OpenJobsTotal { get; set; }

    public int CompletedTodayCount { get; set; }

    public decimal OutstandingBalanceTotal { get; set; }

    public int TodayMovementsCount { get; set; }

    public IReadOnlyList<JobStatusCountViewModel> StatusCounts { get; set; } = [];

    public IReadOnlyList<OutstandingPaymentViewModel> OutstandingPayments { get; set; } = [];

    public IReadOnlyList<RecentInventoryMovementViewModel> RecentInventoryMovements { get; set; } = [];

    // Storefront metrics
    public int NewShopOrdersCount { get; set; }
    public int ShippedTodayCount { get; set; }
    public decimal DeliveredTodayRevenue { get; set; }
    public int ReturnedTodayCount { get; set; }
}

public sealed class JobStatusCountViewModel
{
    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class OutstandingPaymentViewModel
{
    public string JobCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public decimal BalanceOwed { get; set; }

    public string PaymentStatusLabel { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public sealed class RecentInventoryMovementViewModel
{
    public DateTime OccurredAt { get; set; }

    public string PartDisplayName { get; set; } = string.Empty;

    public int QuantityChange { get; set; }

    public string MovementTypeLabel { get; set; } = string.Empty;

    public string CreatedByUsername { get; set; } = string.Empty;

    public string? RepairJobCode { get; set; }
}
