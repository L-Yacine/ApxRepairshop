using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class StaffAccountService
{
    private readonly MimoShopDbContext dbContext;

    public StaffAccountService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<StaffAccount?> ValidateCredentialsAsync(string username, string password)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        string candidatePassword = password ?? string.Empty;

        StaffMember? staff = await dbContext.StaffMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Username == normalizedUsername);

        if (staff is null)
        {
            return null;
        }

        string passwordHash = StaffMember.HashPassword(staff.Salt, candidatePassword);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(staff.PasswordHash),
            Encoding.UTF8.GetBytes(passwordHash))
            ? ToStaffAccount(staff)
            : null;
    }

    public async Task<IReadOnlyCollection<StaffAccount>> GetAssignableStaffAsync()
    {
        List<StaffMember> accounts = await dbContext.StaffMembers
            .AsNoTracking()
            .ToListAsync();

        return accounts
            .Where(account => account.Role is StaffRoles.Owner or StaffRoles.Worker)
            .OrderByDescending(account => account.Role == StaffRoles.Owner)
            .ThenBy(account => account.DisplayName)
            .Select(ToStaffAccount)
            .ToList();
    }

    public async Task<StaffAccount?> FindByUsernameAsync(string username)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;

        StaffMember? staff = await dbContext.StaffMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Username == normalizedUsername);

        return staff is null ? null : ToStaffAccount(staff);
    }

    private static StaffAccount ToStaffAccount(StaffMember staff)
    {
        return new StaffAccount(
            Username: staff.Username,
            DisplayName: staff.DisplayName,
            Role: staff.Role,
            Salt: staff.Salt,
            PasswordHash: staff.PasswordHash);
    }
}

public sealed record StaffAccount(
    string Username,
    string DisplayName,
    string Role,
    string Salt,
    string PasswordHash);

public static class StaffRoles
{
    public const string Owner = "Owner";
    public const string Worker = "Worker";
}
