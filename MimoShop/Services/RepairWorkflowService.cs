using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class RepairWorkflowService
{
    private readonly MimoShopDbContext dbContext;

    public RepairWorkflowService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<RepairTicketListViewModel> GetTicketListAsync()
    {
        List<RepairTicket> tickets = await dbContext.RepairTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.StatusHistory)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();

        List<RepairTicketListItemViewModel> items = tickets
            .Select(ToListItem)
            .ToList();

        return new RepairTicketListViewModel
        {
            ActiveJobs = items
                .Where(ticket => ticket.Status != RepairJobStatuses.WaitingForPart
                    && ticket.Status != RepairJobStatuses.Collected)
                .ToList(),
            WaitingForPartJobs = items
                .Where(ticket => ticket.Status == RepairJobStatuses.WaitingForPart)
                .ToList(),
            CollectedJobs = items
                .Where(ticket => ticket.Status == RepairJobStatuses.Collected)
                .Take(10)
                .ToList()
        };
    }

    public async Task<RepairTicketDetailsViewModel?> FindTicketDetailsAsync(string jobCode)
    {
        string normalizedCode = NormalizeJobCode(jobCode);

        RepairTicket? ticket = await dbContext.RepairTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.StatusHistory)
            .Include(ticket => ticket.PartUsages)
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);

        if (ticket is null)
        {
            return null;
        }

        int? brandId = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Name == ticket.DeviceBrand)
            .Select(brand => (int?)brand.Id)
            .SingleOrDefaultAsync();

        int? phoneModelId = await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.BrandId == brandId && model.Name == ticket.DeviceModel)
            .Select(model => (int?)model.Id)
            .SingleOrDefaultAsync();

        List<InventoryPart> matchingParts = brandId.HasValue && phoneModelId.HasValue
            ? await dbContext.InventoryParts
                .AsNoTracking()
                .Where(part => part.BrandId == brandId.Value && part.PhoneModelId == phoneModelId.Value)
                .Include(part => part.Brand)
                .Include(part => part.PhoneModel)
                .Include(part => part.PartType)
                .Include(part => part.PartVariant)
                .OrderBy(part => part.PartType.SortOrder)
                .ThenBy(part => part.PartType.Name)
                .ThenBy(part => part.PartVariant.SortOrder)
                .ThenBy(part => part.PartVariant.Name)
                .ToListAsync()
            : [];

        return ToDetails(ticket, matchingParts);
    }

    public async Task<bool> UpdateStatusAsync(string jobCode, string status, string changedByUsername)
    {
        string normalizedCode = NormalizeJobCode(jobCode);
        string normalizedStatus = status?.Trim() ?? string.Empty;
        string normalizedUsername = changedByUsername?.Trim() ?? string.Empty;

        if (!RepairJobStatuses.IsValid(normalizedStatus) || string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        RepairTicket? ticket = await dbContext.RepairTickets
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);

        if (ticket is null)
        {
            return false;
        }

        if (ticket.Status == normalizedStatus)
        {
            return true;
        }

        ticket.Status = normalizedStatus;
        dbContext.RepairStatusHistories.Add(new RepairStatusHistory
        {
            RepairTicket = ticket,
            Status = normalizedStatus,
            ChangedAt = DateTime.Now,
            ChangedByUsername = normalizedUsername
        });

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConsumeStockedPartAsync(string jobCode, int inventoryPartId, int quantity, string username)
    {
        string normalizedCode = NormalizeJobCode(jobCode);
        string normalizedUsername = username?.Trim() ?? string.Empty;

        if (inventoryPartId <= 0 || quantity <= 0 || string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        RepairTicket? ticket = await dbContext.RepairTickets
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);
        InventoryPart? part = await dbContext.InventoryParts
            .Include(part => part.Brand)
            .Include(part => part.PhoneModel)
            .Include(part => part.PartType)
            .Include(part => part.PartVariant)
            .SingleOrDefaultAsync(part => part.Id == inventoryPartId);

        if (ticket is null || part is null || !part.IsStocked || part.Quantity < quantity)
        {
            return false;
        }

        DateTime now = DateTime.Now;
        part.Quantity -= quantity;
        part.UpdatedAt = now;

        var usage = new RepairPartUsage
        {
            RepairTicket = ticket,
            InventoryPart = part,
            BrandName = part.Brand.Name,
            PhoneModelName = part.PhoneModel.Name,
            PartTypeName = part.PartType.Name,
            PartVariantName = part.PartVariant.Name,
            Quantity = quantity,
            UnitCostPrice = part.UnitCostPrice,
            UnitSalePrice = part.UnitSalePrice,
            IsOnDemand = false,
            RequestedAt = now,
            ConsumedAt = now,
            CreatedByUsername = normalizedUsername
        };

        dbContext.RepairPartUsages.Add(usage);
        dbContext.InventoryStockMovements.Add(new InventoryStockMovement
        {
            InventoryPart = part,
            RepairTicket = ticket,
            RepairPartUsage = usage,
            QuantityChange = -quantity,
            MovementType = InventoryMovementTypes.ConsumedForRepair,
            OccurredAt = now,
            CreatedByUsername = normalizedUsername
        });

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RequestOnDemandPartAsync(string jobCode, int inventoryPartId, int quantity, string username)
    {
        string normalizedCode = NormalizeJobCode(jobCode);
        string normalizedUsername = username?.Trim() ?? string.Empty;

        if (inventoryPartId <= 0 || quantity <= 0 || string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        RepairTicket? ticket = await dbContext.RepairTickets
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);
        InventoryPart? part = await dbContext.InventoryParts
            .Include(part => part.Brand)
            .Include(part => part.PhoneModel)
            .Include(part => part.PartType)
            .Include(part => part.PartVariant)
            .SingleOrDefaultAsync(part => part.Id == inventoryPartId);

        if (ticket is null || part is null || part.IsStocked)
        {
            return false;
        }

        DateTime now = DateTime.Now;
        var usage = new RepairPartUsage
        {
            RepairTicket = ticket,
            InventoryPart = part,
            BrandName = part.Brand.Name,
            PhoneModelName = part.PhoneModel.Name,
            PartTypeName = part.PartType.Name,
            PartVariantName = part.PartVariant.Name,
            Quantity = quantity,
            UnitCostPrice = part.UnitCostPrice,
            UnitSalePrice = part.UnitSalePrice,
            IsOnDemand = true,
            RequestedAt = now,
            CreatedByUsername = normalizedUsername
        };

        dbContext.RepairPartUsages.Add(usage);
        SetStatus(ticket, RepairJobStatuses.WaitingForPart, normalizedUsername, now);

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReceiveOnDemandPartAsync(string jobCode, int partUsageId, string username)
    {
        string normalizedCode = NormalizeJobCode(jobCode);
        string normalizedUsername = username?.Trim() ?? string.Empty;

        if (partUsageId <= 0 || string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        RepairPartUsage? usage = await dbContext.RepairPartUsages
            .Include(usage => usage.RepairTicket)
            .Include(usage => usage.InventoryPart)
            .SingleOrDefaultAsync(usage => usage.Id == partUsageId
                && usage.RepairTicket.JobCode == normalizedCode);

        if (usage is null || !usage.IsOnDemand || usage.InventoryPart is null || usage.ConsumedAt.HasValue)
        {
            return false;
        }

        DateTime now = DateTime.Now;
        usage.ReceivedAt = now;
        usage.ConsumedAt = now;
        usage.InventoryPart.UpdatedAt = now;

        dbContext.InventoryStockMovements.Add(new InventoryStockMovement
        {
            InventoryPart = usage.InventoryPart,
            RepairTicket = usage.RepairTicket,
            RepairPartUsage = usage,
            QuantityChange = usage.Quantity,
            MovementType = InventoryMovementTypes.ReceivedOnDemand,
            OccurredAt = now,
            CreatedByUsername = normalizedUsername
        });
        dbContext.InventoryStockMovements.Add(new InventoryStockMovement
        {
            InventoryPart = usage.InventoryPart,
            RepairTicket = usage.RepairTicket,
            RepairPartUsage = usage,
            QuantityChange = -usage.Quantity,
            MovementType = InventoryMovementTypes.ConsumedForRepair,
            OccurredAt = now,
            CreatedByUsername = normalizedUsername
        });

        SetStatus(usage.RepairTicket, RepairJobStatuses.InProgress, normalizedUsername, now);

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePaymentAsync(string jobCode, decimal estimatedPrice, decimal amountPaid)
    {
        string normalizedCode = NormalizeJobCode(jobCode);

        if (estimatedPrice < 0 || amountPaid < 0)
        {
            return false;
        }

        RepairTicket? ticket = await dbContext.RepairTickets
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);

        if (ticket is null)
        {
            return false;
        }

        if (ticket.Status != RepairJobStatuses.Collected)
        {
            ticket.EstimatedPrice = estimatedPrice;
        }

        ticket.AmountPaid = amountPaid;
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static RepairTicketListItemViewModel ToListItem(RepairTicket ticket)
    {
        return new RepairTicketListItemViewModel
        {
            JobCode = ticket.JobCode,
            CreatedAt = ticket.CreatedAt,
            CustomerName = ticket.Customer.Name,
            CustomerPhone = ticket.Customer.Phone,
            DeviceName = $"{ticket.DeviceBrand} {ticket.DeviceModel}",
            ProblemDescription = ticket.ProblemDescription,
            AssignedWorkerName = ticket.AssignedWorkerName,
            Status = ticket.Status,
            StatusLabel = RepairJobStatuses.ToArabicLabel(ticket.Status),
            LastStatusChangedAt = ticket.StatusHistory
                .OrderByDescending(history => history.ChangedAt)
                .Select(history => (DateTime?)history.ChangedAt)
                .FirstOrDefault()
        };
    }

    private static RepairTicketDetailsViewModel ToDetails(RepairTicket ticket, IReadOnlyList<InventoryPart> matchingParts)
    {
        var stockedParts = dbPartOptions(stocked: true);
        var onDemandParts = dbPartOptions(stocked: false);

        return new RepairTicketDetailsViewModel
        {
            JobCode = ticket.JobCode,
            CreatedAt = ticket.CreatedAt,
            CustomerName = ticket.Customer.Name,
            CustomerPhone = ticket.Customer.Phone,
            DeviceName = $"{ticket.DeviceBrand} {ticket.DeviceModel}",
            ProblemDescription = ticket.ProblemDescription,
            AssignedWorkerName = ticket.AssignedWorkerName,
            EstimatedPrice = ticket.EstimatedPrice,
            AmountPaid = ticket.AmountPaid,
            BalanceOwed = CalculateBalanceOwed(ticket.EstimatedPrice, ticket.AmountPaid),
            PaymentStatusLabel = GetPaymentStatusLabel(ticket.EstimatedPrice, ticket.AmountPaid),
            Notes = ticket.Notes,
            Status = ticket.Status,
            StatusLabel = RepairJobStatuses.ToArabicLabel(ticket.Status),
            StatusOptions = RepairJobStatuses.All
                .Select(status => new RepairStatusOptionViewModel
                {
                    Value = status,
                    Label = RepairJobStatuses.ToArabicLabel(status)
                })
                .ToList(),
            StatusHistory = ticket.StatusHistory
                .OrderByDescending(history => history.ChangedAt)
                .Select(history => new RepairStatusHistoryViewModel
                {
                    Status = history.Status,
                    StatusLabel = RepairJobStatuses.ToArabicLabel(history.Status),
                    ChangedAt = history.ChangedAt,
                    ChangedByUsername = history.ChangedByUsername
                })
                .ToList(),
            StockedPartOptions = stockedParts,
            OnDemandPartOptions = onDemandParts,
            StockedPartTypes = BuildPartTypeTabs(stockedParts),
            OnDemandPartTypes = BuildPartTypeTabs(onDemandParts),
            PartUsages = ticket.PartUsages
                .OrderByDescending(usage => usage.RequestedAt)
                .Select(usage => new RepairPartUsageViewModel
                {
                    Id = usage.Id,
                    DisplayName = FormatPartName(usage.BrandName, usage.PhoneModelName, usage.PartTypeName, usage.PartVariantName),
                    Quantity = usage.Quantity,
                    UnitSalePrice = usage.UnitSalePrice,
                    IsOnDemand = usage.IsOnDemand,
                    RequestedAt = usage.RequestedAt,
                    ReceivedAt = usage.ReceivedAt,
                    ConsumedAt = usage.ConsumedAt,
                    CreatedByUsername = usage.CreatedByUsername
                })
                .ToList()
        };

        IReadOnlyList<RepairPartOptionViewModel> dbPartOptions(bool stocked)
        {
            return matchingParts
                .Where(part => part.IsStocked == stocked
                    && (!stocked || part.Quantity > 0))
                .Select(part => new RepairPartOptionViewModel
                {
                    Id = part.Id,
                    PartTypeId = part.PartType.Id,
                    PartTypeName = string.IsNullOrWhiteSpace(part.PartType.DisplayNameAr)
                        ? part.PartType.Name
                        : part.PartType.DisplayNameAr,
                    PartVariantName = part.PartVariant.Name,
                    DisplayName = $"{part.PartType.Name} - {part.PartVariant.Name}",
                    ThumbnailUrl = part.PartType.ThumbnailUrl,
                    Quantity = part.Quantity,
                    UnitSalePrice = part.UnitSalePrice,
                    IsStocked = part.IsStocked
                })
                .ToList();
        }

        static IReadOnlyList<RepairPartTypeTabViewModel> BuildPartTypeTabs(IReadOnlyList<RepairPartOptionViewModel> parts)
        {
            return parts
                .GroupBy(part => part.PartTypeId)
                .Select(group => new RepairPartTypeTabViewModel
                {
                    Id = group.Key,
                    Name = group.First().PartTypeName,
                    ThumbnailUrl = group.First().ThumbnailUrl,
                    VariantCount = group.Count(),
                    Parts = group.ToList()
                })
                .ToList();
        }
    }

    private static string NormalizeJobCode(string jobCode)
    {
        return jobCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private static void SetStatus(RepairTicket ticket, string status, string changedByUsername, DateTime changedAt)
    {
        if (ticket.Status == status)
        {
            return;
        }

        ticket.Status = status;
        ticket.StatusHistory.Add(new RepairStatusHistory
        {
            Status = status,
            ChangedAt = changedAt,
            ChangedByUsername = changedByUsername
        });
    }

    private static decimal CalculateBalanceOwed(decimal estimatedPrice, decimal amountPaid)
    {
        return Math.Max(estimatedPrice - amountPaid, 0);
    }

    private static string GetPaymentStatusLabel(decimal estimatedPrice, decimal amountPaid)
    {
        if (amountPaid <= 0)
        {
            return "غير مدفوع";
        }

        return amountPaid >= estimatedPrice ? "مدفوع بالكامل" : "مدفوع جزئياً";
    }

    private static string FormatPartName(string brand, string model, string partType, string variant)
    {
        return $"{brand} {model} - {partType} - {variant}";
    }
}

public static class InventoryMovementTypes
{
    public const string ConsumedForRepair = "Consumed for repair";
    public const string ReceivedOnDemand = "Received on demand";

    // Shop order confirmation decrements stock, return/cancel restore it.
    public const string ShopOrder = "ShopOrder";
    public const string ShopOrderReturn = "ShopOrderReturn";
    public const string ShopOrderCancel = "ShopOrderCancel";
}
