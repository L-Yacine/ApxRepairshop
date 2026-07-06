using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class RepairIntakeService
{
    private readonly MimoShopDbContext dbContext;

    public RepairIntakeService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DeviceBrandOption>> GetDeviceOptionsAsync()
    {
        List<Brand> brands = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.SortOrder)
            .ThenBy(brand => brand.Name)
            .Include(brand => brand.PhoneModels.Where(model => model.IsActive)
                .OrderBy(model => model.SortOrder)
                .ThenBy(model => model.Name))
            .ToListAsync();

        return brands
            .Select(brand => new DeviceBrandOption(
                brand.Name,
                brand.PhoneModels.Select(model => model.Name).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetModelsForBrandAsync(string brand)
    {
        string normalizedBrand = brand?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedBrand))
        {
            return [];
        }

        return await dbContext.PhoneModels
            .AsNoTracking()
            .Where(model => model.IsActive && model.Brand!.Name == normalizedBrand)
            .OrderBy(model => model.SortOrder)
            .ThenBy(model => model.Name)
            .Select(model => model.Name)
            .ToListAsync();
    }

    public async Task<CustomerRecord?> FindCustomerByPhoneAsync(string phone)
    {
        string normalizedPhone = NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        Customer? customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(customer => customer.Phone == normalizedPhone);

        return customer is null ? null : ToCustomerRecord(customer);
    }

    public async Task<RepairTicketRecord?> FindTicketByCodeAsync(string jobCode)
    {
        string normalizedCode = jobCode?.Trim() ?? string.Empty;

        RepairTicket? ticket = await dbContext.RepairTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .SingleOrDefaultAsync(ticket => ticket.JobCode == normalizedCode);

        return ticket is null ? null : ToRepairTicketRecord(ticket);
    }

    public async Task<RepairTicketRecord> CreateTicketAsync(RepairIntakeViewModel model, StaffAccount assignedWorker)
    {
        string normalizedPhone = NormalizePhone(model.CustomerPhone);

        Customer? customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Phone == normalizedPhone);

        if (customer is null)
        {
            customer = new Customer
            {
                Name = model.CustomerName.Trim(),
                Phone = normalizedPhone
            };

            dbContext.Customers.Add(customer);
        }
        else
        {
            customer.Name = model.CustomerName.Trim();
        }

        customer.WhatsApp = NormalizeOptional(model.CustomerWhatsApp);
        customer.Telegram = NormalizeOptional(model.CustomerTelegram);

        var ticket = new RepairTicket
        {
            JobCode = ("PEND-" + Guid.NewGuid().ToString("N"))[..17],
            CreatedAt = DateTime.Now,
            Customer = customer,
            DeviceBrand = model.DeviceBrand.Trim(),
            DeviceModel = model.DeviceModel.Trim(),
            ProblemDescription = model.ProblemDescription.Trim(),
            AssignedWorkerUsername = assignedWorker.Username,
            AssignedWorkerName = assignedWorker.DisplayName,
            EstimatedPrice = model.EstimatedPrice,
            Notes = NormalizeOptional(model.Notes),
            Status = RepairJobStatuses.New,
            StatusHistory =
            [
                new RepairStatusHistory
                {
                    Status = RepairJobStatuses.New,
                    ChangedAt = DateTime.Now,
                    ChangedByUsername = assignedWorker.Username
                }
            ]
        };

        dbContext.RepairTickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        ticket.JobCode = $"REP-{ticket.Id:0000}";
        await dbContext.SaveChangesAsync();

        return ToRepairTicketRecord(ticket);
    }

    public async Task<bool> IsKnownBrandModelAsync(string brand, string model)
    {
        string normalizedBrand = brand?.Trim() ?? string.Empty;
        string normalizedModel = model?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedBrand) || string.IsNullOrWhiteSpace(normalizedModel))
        {
            return false;
        }

        return await dbContext.PhoneModels
            .AsNoTracking()
            .AnyAsync(model => model.IsActive
                && model.Brand!.Name == normalizedBrand
                && model.Name == normalizedModel);
    }

    private static string NormalizePhone(string phone)
    {
        return phone?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static CustomerRecord ToCustomerRecord(Customer customer)
    {
        return new CustomerRecord(
            Name: customer.Name,
            Phone: customer.Phone,
            WhatsApp: customer.WhatsApp,
            Telegram: customer.Telegram);
    }

    private static RepairTicketRecord ToRepairTicketRecord(RepairTicket ticket)
    {
        return new RepairTicketRecord(
            JobCode: ticket.JobCode,
            CreatedAt: ticket.CreatedAt,
            Customer: ToCustomerRecord(ticket.Customer),
            DeviceBrand: ticket.DeviceBrand,
            DeviceModel: ticket.DeviceModel,
            ProblemDescription: ticket.ProblemDescription,
            AssignedWorkerUsername: ticket.AssignedWorkerUsername,
            AssignedWorkerName: ticket.AssignedWorkerName,
            EstimatedPrice: ticket.EstimatedPrice,
            Notes: ticket.Notes,
            Status: ticket.Status);
    }
}

public sealed record CustomerRecord(
    string Name,
    string Phone,
    string? WhatsApp,
    string? Telegram);

public sealed record RepairTicketRecord(
    string JobCode,
    DateTime CreatedAt,
    CustomerRecord Customer,
    string DeviceBrand,
    string DeviceModel,
    string ProblemDescription,
    string AssignedWorkerUsername,
    string AssignedWorkerName,
    decimal EstimatedPrice,
    string? Notes,
    string Status);

public static class RepairJobStatuses
{
    public const string New = "New";
    public const string InProgress = "In progress";
    public const string WaitingForPart = "Waiting for part";
    public const string Done = "Done";
    public const string Collected = "Collected";

    private static readonly IReadOnlyList<string> all =
    [
        New,
        InProgress,
        WaitingForPart,
        Done,
        Collected
    ];

    private static readonly IReadOnlyDictionary<string, string> arabicLabels = new Dictionary<string, string>
    {
        [New] = "جديد",
        [InProgress] = "قيد العمل",
        [WaitingForPart] = "بانتظار قطعة",
        [Done] = "جاهز للتسليم",
        [Collected] = "تم التسليم"
    };

    public static IReadOnlyList<string> All => all;

    public static bool IsValid(string status)
    {
        return all.Contains(status);
    }

    public static string ToArabicLabel(string status)
    {
        return arabicLabels.TryGetValue(status, out string? label) ? label : status;
    }
}
