using System.Security.Cryptography;
using System.Text;

namespace MimoShop.Services;

public sealed class StaffAccountService
{
    private readonly IReadOnlyDictionary<string, StaffAccount> accounts =
        new List<StaffAccount>
        {
            CreateAccount("owner", "owner123", "صاحب المحل", StaffRoles.Owner),
            CreateAccount("worker1", "worker123", "العامل 1", StaffRoles.Worker),
            CreateAccount("worker2", "worker123", "العامل 2", StaffRoles.Worker)
        }.ToDictionary(account => account.Username, StringComparer.OrdinalIgnoreCase);

    public StaffAccount? ValidateCredentials(string username, string password)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        string candidatePassword = password ?? string.Empty;

        if (!accounts.TryGetValue(normalizedUsername, out StaffAccount? account))
        {
            return null;
        }

        string passwordHash = HashPassword(account.Salt, candidatePassword);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(account.PasswordHash),
            Encoding.UTF8.GetBytes(passwordHash))
            ? account
            : null;
    }

    private static StaffAccount CreateAccount(string username, string password, string displayName, string role)
    {
        string salt = $"mimoshop-v1-{username}";

        return new StaffAccount(
            Username: username,
            DisplayName: displayName,
            Role: role,
            Salt: salt,
            PasswordHash: HashPassword(salt, password));
    }

    private static string HashPassword(string salt, string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToHexString(bytes);
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
