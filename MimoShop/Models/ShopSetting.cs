namespace MimoShop.Models;

public sealed class ShopSetting
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LatinName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? WhatsApp { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? TelegramHandle { get; set; }

    public string? OpeningHours { get; set; }

    public string? LogoUrl { get; set; }
}
