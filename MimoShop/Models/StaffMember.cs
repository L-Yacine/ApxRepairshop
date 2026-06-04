using System.Security.Cryptography;
using System.Text;

namespace MimoShop.Models;

public sealed class StaffMember
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Salt { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public static StaffMember CreateSeed(int id, string username, string password, string displayName, string role)
    {
        string salt = $"mimoshop-v1-{username}";

        return new StaffMember
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            Role = role,
            Salt = salt,
            PasswordHash = HashPassword(salt, password)
        };
    }

    public static string HashPassword(string salt, string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToHexString(bytes);
    }
}
