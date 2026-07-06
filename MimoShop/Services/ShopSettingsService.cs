using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class ShopSettingsService
{
    private readonly MimoShopDbContext dbContext;

    public ShopSettingsService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ShopSetting> GetSettingsAsync()
    {
        ShopSetting? settings = await dbContext.ShopSettings
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (settings is not null)
        {
            return settings;
        }

        settings = new ShopSetting
        {
            Id = 1,
            Name = "ميمو شوب",
            LatinName = "MimoShop",
            Phone = "000 00 00 00",
            Address = "الجزائر",
        };

        dbContext.ShopSettings.Add(settings);
        await dbContext.SaveChangesAsync();
        return settings;
    }

    public async Task UpdateSettingsAsync(ShopSetting settings)
    {
        ShopSetting? existing = await dbContext.ShopSettings.SingleOrDefaultAsync();
        if (existing is null)
        {
            settings.Id = 1;
            dbContext.ShopSettings.Add(settings);
        }
        else
        {
            existing.Name = settings.Name;
            existing.LatinName = settings.LatinName;
            existing.Phone = settings.Phone;
            existing.WhatsApp = settings.WhatsApp;
            existing.Address = settings.Address;
            existing.TelegramHandle = settings.TelegramHandle;
            existing.OpeningHours = settings.OpeningHours;
            existing.LogoUrl = settings.LogoUrl;
        }

        await dbContext.SaveChangesAsync();
    }
}
