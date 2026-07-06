using Microsoft.EntityFrameworkCore;
using MimoShop.Data;

namespace MimoShop.Services.Telegram;

public sealed class RepairStatusQueryService
{
    private readonly MimoShopDbContext dbContext;

    public RepairStatusQueryService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<RepairStatusSummary?> FindByCodeAsync(string? input)
    {
        string normalized = NormalizeInput(input);
        if (!IsWellFormed(normalized))
        {
            return null;
        }

        return await dbContext.RepairTickets
            .AsNoTracking()
            .Where(ticket => ticket.JobCode == normalized)
            .Select(ticket => new RepairStatusSummary(
                $"{ticket.DeviceBrand} {ticket.DeviceModel}",
                RepairJobStatuses.ToArabicLabel(ticket.Status),
                ticket.AssignedWorkerName))
            .SingleOrDefaultAsync();
    }

    public static string NormalizeInput(string? input)
    {
        return input?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static bool IsWellFormed(string normalized)
    {
        if (string.IsNullOrEmpty(normalized) || normalized.Length < 5)
        {
            return false;
        }

        if (!normalized.StartsWith("REP-", StringComparison.Ordinal))
        {
            return false;
        }

        for (int i = 4; i < normalized.Length; i++)
        {
            if (!char.IsDigit(normalized[i]))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record RepairStatusSummary(
    string DeviceName,
    string StatusLabel,
    string AssignedWorkerName);
