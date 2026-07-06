using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class StaffAccountService
{
    private readonly MimoShopDbContext dbContext;
    private readonly PasswordHasher<StaffMember> passwordHasher;

    public StaffAccountService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
        this.passwordHasher = new PasswordHasher<StaffMember>();
    }

    public async Task<StaffAccount?> ValidateCredentialsAsync(string username, string password)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        string candidatePassword = password ?? string.Empty;

        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == normalizedUsername);

        if (staff is null || !staff.IsActive)
        {
            return null;
        }

        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(staff, staff.PasswordHash, candidatePassword);

        if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
        {
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                staff.PasswordHash = passwordHasher.HashPassword(staff, candidatePassword);
                await dbContext.SaveChangesAsync();
            }
            return ToStaffAccount(staff);
        }

        string oldHash = StaffMember.HashPassword(staff.Salt, candidatePassword);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(staff.PasswordHash),
                Encoding.UTF8.GetBytes(oldHash)))
        {
            return null;
        }

        staff.PasswordHash = passwordHasher.HashPassword(staff, candidatePassword);
        staff.Salt = string.Empty;
        await dbContext.SaveChangesAsync();

        return ToStaffAccount(staff);
    }

    public async Task<StaffAccount?> FindByUsernameAsync(string username)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;

        StaffMember? staff = await dbContext.StaffMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Username == normalizedUsername);

        return staff is null ? null : ToStaffAccount(staff);
    }

    public async Task<IReadOnlyCollection<StaffAccount>> GetAllStaffAsync()
    {
        List<StaffMember> accounts = await dbContext.StaffMembers
            .AsNoTracking()
            .OrderBy(account => account.Role == StaffRoles.SuperAdmin ? 0 : account.Role == StaffRoles.Owner ? 1 : 2)
            .ThenBy(account => account.DisplayName)
            .ToListAsync();

        return accounts.Select(ToStaffAccount).ToList();
    }

    public async Task<IReadOnlyCollection<StaffAccount>> GetAssignableStaffAsync()
    {
        List<StaffMember> accounts = await dbContext.StaffMembers
            .AsNoTracking()
            .Where(account => account.IsActive)
            .ToListAsync();

        return accounts
            .Where(account => account.Role is StaffRoles.Owner or StaffRoles.Worker or StaffRoles.SuperAdmin)
            .OrderByDescending(account => account.Role == StaffRoles.Owner)
            .ThenBy(account => account.DisplayName)
            .Select(ToStaffAccount)
            .ToList();
    }

    public async Task<StaffAccount> CreateStaffAsync(string username, string displayName, string role, string password)
    {
        var staff = new StaffMember
        {
            Username = username.Trim(),
            DisplayName = displayName.Trim(),
            Role = role,
            Salt = string.Empty,
            PasswordHash = passwordHasher.HashPassword(new StaffMember(), password),
            IsActive = true
        };

        dbContext.StaffMembers.Add(staff);
        await dbContext.SaveChangesAsync();

        return ToStaffAccount(staff);
    }

    public async Task<StaffAccount?> UpdateStaffAsync(string username, string displayName, string role)
    {
        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == username.Trim());

        if (staff is null)
        {
            return null;
        }

        staff.DisplayName = displayName.Trim();
        staff.Role = role;

        await dbContext.SaveChangesAsync();
        return ToStaffAccount(staff);
    }

    public async Task<bool> SetPasswordAsync(string username, string newPassword)
    {
        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == username.Trim());

        if (staff is null)
        {
            return false;
        }

        staff.PasswordHash = passwordHasher.HashPassword(staff, newPassword);
        staff.Salt = string.Empty;
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == username.Trim());

        if (staff is null)
        {
            return false;
        }

        PasswordVerificationResult verify = passwordHasher.VerifyHashedPassword(staff, staff.PasswordHash, currentPassword);
        if (verify is PasswordVerificationResult.Failed)
        {
            string oldHash = StaffMember.HashPassword(staff.Salt, currentPassword);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(staff.PasswordHash),
                    Encoding.UTF8.GetBytes(oldHash)))
            {
                return false;
            }
        }

        staff.PasswordHash = passwordHasher.HashPassword(staff, newPassword);
        staff.Salt = string.Empty;
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateStaffAsync(string username)
    {
        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == username.Trim());

        if (staff is null)
        {
            return false;
        }

        staff.IsActive = false;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateStaffAsync(string username)
    {
        StaffMember? staff = await dbContext.StaffMembers
            .SingleOrDefaultAsync(account => account.Username == username.Trim());

        if (staff is null)
        {
            return false;
        }

        staff.IsActive = true;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await dbContext.StaffMembers
            .AsNoTracking()
            .AnyAsync(account => account.Username == username.Trim());
    }

    private static StaffAccount ToStaffAccount(StaffMember staff)
    {
        return new StaffAccount(
            Username: staff.Username,
            DisplayName: staff.DisplayName,
            Role: staff.Role,
            Salt: staff.Salt,
            PasswordHash: staff.PasswordHash,
            IsActive: staff.IsActive);
    }
}

public sealed record StaffAccount(
    string Username,
    string DisplayName,
    string Role,
    string Salt,
    string PasswordHash,
    bool IsActive = true);

public static class StaffRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string Worker = "Worker";

    public static string ToArabicLabel(string role) => role switch
    {
        SuperAdmin => "مدير النظام",
        Owner => "مالك",
        Worker => "عامل",
        _ => role
    };
}
