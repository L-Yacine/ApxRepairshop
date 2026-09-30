using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class DashboardService
{
    private readonly MimoShopDbContext dbContext;

    public DashboardService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<OwnerDashboardViewModel> BuildDashboardAsync()
    {
        DateTime generatedAt = DateTime.Now;
        DateTime todayStart = generatedAt.Date;
        DateTime tomorrowStart = todayStart.AddDays(1);

        List<RepairTicket> tickets = await dbContext.RepairTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.StatusHistory)
            .ToListAsync();

        List<JobStatusCountViewModel> statusCounts = RepairJobStatuses.All
            .Select(status => new JobStatusCountViewModel
            {
                Status = status,
                StatusLabel = RepairJobStatuses.ToArabicLabel(status),
                Count = tickets.Count(ticket => ticket.Status == status)
            })
            .ToList();

        int openJobsTotal = statusCounts
            .Where(item => item.Status != RepairJobStatuses.Collected)
            .Sum(item => item.Count);

        int completedTodayCount = tickets
            .Where(ticket => ticket.Status == RepairJobStatuses.Done)
            .Where(ticket => ticket.StatusHistory.Count > 0)
            .Count(ticket =>
            {
                DateTime latestChange = ticket.StatusHistory.Max(history => history.ChangedAt);
                return latestChange >= todayStart && latestChange < tomorrowStart;
            });

        List<OutstandingPaymentViewModel> outstandingPayments = tickets
            .Where(ticket => ticket.EstimatedPrice > ticket.AmountPaid)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(20)
            .Select(ticket => new OutstandingPaymentViewModel
            {
                JobCode = ticket.JobCode,
                CustomerName = ticket.Customer.Name,
                CustomerPhone = ticket.Customer.Phone,
                DeviceName = $"{ticket.DeviceBrand} {ticket.DeviceModel}",
                BalanceOwed = ticket.EstimatedPrice - ticket.AmountPaid,
                PaymentStatusLabel = GetPaymentStatusLabel(ticket.EstimatedPrice, ticket.AmountPaid),
                StatusLabel = RepairJobStatuses.ToArabicLabel(ticket.Status),
                CreatedAt = ticket.CreatedAt
            })
            .ToList();

        decimal outstandingBalanceTotal = tickets
            .Where(ticket => ticket.EstimatedPrice > ticket.AmountPaid)
            .Sum(ticket => ticket.EstimatedPrice - ticket.AmountPaid);

        List<InventoryStockMovement> todayMovements = await dbContext.InventoryStockMovements
            .AsNoTracking()
            .Where(movement => movement.OccurredAt >= todayStart && movement.OccurredAt < tomorrowStart)
            .Include(movement => movement.InventoryPart).ThenInclude(part => part!.Brand)
            .Include(movement => movement.InventoryPart).ThenInclude(part => part!.PhoneModel)
            .Include(movement => movement.InventoryPart).ThenInclude(part => part!.PartType)
            .Include(movement => movement.InventoryPart).ThenInclude(part => part!.PartVariant)
            .Include(movement => movement.RepairTicket)
            .OrderByDescending(movement => movement.OccurredAt)
            .Take(20)
            .ToListAsync();

        List<RecentInventoryMovementViewModel> recentMovements = todayMovements
            .Select(movement => new RecentInventoryMovementViewModel
            {
                OccurredAt = movement.OccurredAt,
                PartDisplayName = FormatPartName(movement.InventoryPart),
                QuantityChange = movement.QuantityChange,
                MovementTypeLabel = ToMovementTypeLabel(movement.MovementType),
                CreatedByUsername = movement.CreatedByUsername,
                RepairJobCode = movement.RepairTicket?.JobCode
            })
            .ToList();

        // Storefront metrics
        int newShopOrdersCount = await dbContext.ShopOrders
            .AsNoTracking()
            .CountAsync(order => order.Status == ShopOrderStatuses.New);

        int shippedTodayCount = await dbContext.ShopOrders
            .AsNoTracking()
            .CountAsync(order => order.Status == ShopOrderStatuses.Shipped
                && order.ShippedAt.HasValue
                && order.ShippedAt.Value >= todayStart
                && order.ShippedAt.Value < tomorrowStart);

        decimal deliveredTodayRevenue = await dbContext.ShopOrders
            .AsNoTracking()
            .Where(order => order.Status == ShopOrderStatuses.Delivered
                && order.DeliveredAt.HasValue
                && order.DeliveredAt.Value >= todayStart
                && order.DeliveredAt.Value < tomorrowStart)
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0m;

        int returnedTodayCount = await dbContext.ShopOrders
            .AsNoTracking()
            .CountAsync(order => order.Status == ShopOrderStatuses.Returned
                && order.ReturnedAt.HasValue
                && order.ReturnedAt.Value >= todayStart
                && order.ReturnedAt.Value < tomorrowStart);

        return new OwnerDashboardViewModel
        {
            GeneratedAt = generatedAt,
            OpenJobsTotal = openJobsTotal,
            CompletedTodayCount = completedTodayCount,
            OutstandingBalanceTotal = outstandingBalanceTotal,
            TodayMovementsCount = todayMovements.Count,
            StatusCounts = statusCounts,
            OutstandingPayments = outstandingPayments,
            RecentInventoryMovements = recentMovements,
            NewShopOrdersCount = newShopOrdersCount,
            ShippedTodayCount = shippedTodayCount,
            DeliveredTodayRevenue = deliveredTodayRevenue,
            ReturnedTodayCount = returnedTodayCount
        };
    }

    private static string FormatPartName(InventoryPart part)
    {
        return $"{part.Brand.Name} {part.PhoneModel.Name} - {part.PartType.Name} - {part.PartVariant.Name}";
    }

    private static string ToMovementTypeLabel(string movementType)
    {
        return movementType switch
        {
            InventoryMovementTypes.ConsumedForRepair => "استهلاك في صيانة",
            InventoryMovementTypes.ReceivedOnDemand => "استلام عند الطلب",
            InventoryMovementTypes.ShopOrder => "تأكيد طلب متجر",
            InventoryMovementTypes.ShopOrderReturn => "إرجاع طلب متجر",
            InventoryMovementTypes.ShopOrderCancel => "إلغاء طلب متجر",
            _ => movementType
        };
    }

    private static string GetPaymentStatusLabel(decimal estimatedPrice, decimal amountPaid)
    {
        if (amountPaid <= 0)
        {
            return "غير مدفوع";
        }

        return amountPaid >= estimatedPrice ? "مدفوع بالكامل" : "مدفوع جزئياً";
    }
}
