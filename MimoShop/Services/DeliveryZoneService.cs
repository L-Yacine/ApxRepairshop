using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Localization;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class DeliveryZoneService
{
    private readonly MimoShopDbContext dbContext;

    public DeliveryZoneService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WilayaSummary>> ListWilayasAsync()
    {
        return await dbContext.Wilayas
            .AsNoTracking()
            .OrderBy(w => w.Code)
            .Select(w => new WilayaSummary
            {
                Id = w.Id,
                Code = w.Code,
                NameAr = w.NameAr,
                NameFr = w.NameFr,
                ShippingFee = w.ShippingFee,
                IsActive = w.IsActive,
                CommuneCount = w.Communes.Count
            })
            .ToListAsync();
    }

    public async Task<Wilaya?> FindWilayaAsync(int id)
    {
        return await dbContext.Wilayas.SingleOrDefaultAsync(w => w.Id == id);
    }

    public async Task<bool> CreateWilayaAsync(WilayaFormInput input)
    {
        string code = Normalize(input.Code);
        string nameAr = Normalize(input.NameAr);
        string nameFr = Normalize(input.NameFr);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameFr))
        {
            return false;
        }

        if (await dbContext.Wilayas.AnyAsync(w => w.Code == code || w.NameAr == nameAr || w.NameFr == nameFr))
        {
            return false;
        }

        dbContext.Wilayas.Add(new Wilaya
        {
            Code = code,
            NameAr = nameAr,
            NameFr = nameFr,
            ShippingFee = Math.Max(0m, input.ShippingFee),
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateWilayaAsync(int id, WilayaFormInput input)
    {
        Wilaya? wilaya = await dbContext.Wilayas.SingleOrDefaultAsync(w => w.Id == id);
        if (wilaya is null)
        {
            return false;
        }

        string code = Normalize(input.Code);
        string nameAr = Normalize(input.NameAr);
        string nameFr = Normalize(input.NameFr);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameFr))
        {
            return false;
        }

        if (await dbContext.Wilayas.AnyAsync(w => w.Id != id && (w.Code == code || w.NameAr == nameAr || w.NameFr == nameFr)))
        {
            return false;
        }

        wilaya.Code = code;
        wilaya.NameAr = nameAr;
        wilaya.NameFr = nameFr;
        wilaya.ShippingFee = Math.Max(0m, input.ShippingFee);
        wilaya.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task ToggleWilayaAsync(int id)
    {
        Wilaya? wilaya = await dbContext.Wilayas.SingleOrDefaultAsync(w => w.Id == id);
        if (wilaya is null)
        {
            return;
        }
        wilaya.IsActive = !wilaya.IsActive;
        await dbContext.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> DeleteWilayaAsync(int id)
    {
        Wilaya? wilaya = await dbContext.Wilayas
            .Include(w => w.Communes)
            .SingleOrDefaultAsync(w => w.Id == id);
        if (wilaya is null)
        {
            return (false, "الولاية غير موجودة.");
        }

        bool hasOrders = await dbContext.ShopOrders.AnyAsync(o => o.WilayaId == id);
        if (hasOrders)
        {
            return (false, "لا يمكن حذف الولاية لوجود طلبات مرتبطة بها. عطّلها بدلاً من ذلك لإخفائها من صفحة الطلب.");
        }

        dbContext.Communes.RemoveRange(wilaya.Communes);
        dbContext.Wilayas.Remove(wilaya);
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<CommuneSummary>> ListCommunesAsync(int wilayaId)
    {
        return await dbContext.Communes
            .AsNoTracking()
            .Where(c => c.WilayaId == wilayaId)
            .OrderBy(c => c.NameAr)
            .Select(c => new CommuneSummary
            {
                Id = c.Id,
                WilayaId = c.WilayaId,
                NameAr = c.NameAr,
                NameFr = c.NameFr,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<Commune?> FindCommuneAsync(int id)
    {
        return await dbContext.Communes.SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> CreateCommuneAsync(int wilayaId, CommuneFormInput input)
    {
        if (!await dbContext.Wilayas.AnyAsync(w => w.Id == wilayaId))
        {
            return false;
        }

        string nameAr = Normalize(input.NameAr);
        string nameFr = Normalize(input.NameFr);
        if (string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameFr))
        {
            return false;
        }

        if (await dbContext.Communes.AnyAsync(c => c.WilayaId == wilayaId && c.NameAr == nameAr))
        {
            return false;
        }

        dbContext.Communes.Add(new Commune
        {
            WilayaId = wilayaId,
            NameAr = nameAr,
            NameFr = nameFr,
            IsActive = input.IsActive
        });
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCommuneAsync(int id, CommuneFormInput input)
    {
        Commune? commune = await dbContext.Communes.SingleOrDefaultAsync(c => c.Id == id);
        if (commune is null)
        {
            return false;
        }

        string nameAr = Normalize(input.NameAr);
        string nameFr = Normalize(input.NameFr);
        if (string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameFr))
        {
            return false;
        }

        if (await dbContext.Communes.AnyAsync(c => c.WilayaId == commune.WilayaId && c.Id != id && c.NameAr == nameAr))
        {
            return false;
        }

        commune.NameAr = nameAr;
        commune.NameFr = nameFr;
        commune.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task ToggleCommuneAsync(int id)
    {
        Commune? commune = await dbContext.Communes.SingleOrDefaultAsync(c => c.Id == id);
        if (commune is null)
        {
            return;
        }
        commune.IsActive = !commune.IsActive;
        await dbContext.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> DeleteCommuneAsync(int id)
    {
        Commune? commune = await dbContext.Communes.SingleOrDefaultAsync(c => c.Id == id);
        if (commune is null)
        {
            return (false, "البلدية غير موجودة.");
        }

        bool hasOrders = await dbContext.ShopOrders.AnyAsync(o => o.CommuneId == id);
        if (hasOrders)
        {
            return (false, "لا يمكن حذف البلدية لوجود طلبات مرتبطة بها. عطّلها بدلاً من ذلك.");
        }

        dbContext.Communes.Remove(commune);
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<CommuneOption>> GetActiveCommunesAsync(int wilayaId)
    {
        return await dbContext.Communes
            .AsNoTracking()
            .Where(c => c.WilayaId == wilayaId && c.IsActive)
            .OrderBy(c => c.NameAr)
            .Select(c => new CommuneOption
            {
                Id = c.Id,
                NameAr = c.NameAr,
                NameFr = c.NameFr
            })
            .ToListAsync();
    }

    public async Task<WilayaOption?> GetActiveWilayaAsync(int id)
    {
        return await dbContext.Wilayas
            .AsNoTracking()
            .Where(w => w.Id == id && w.IsActive)
            .Select(w => new WilayaOption
            {
                Id = w.Id,
                Code = w.Code,
                NameAr = w.NameAr,
                NameFr = w.NameFr,
                ShippingFee = w.ShippingFee
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<WilayaOption>> GetActiveWilayasAsync()
    {
        return await dbContext.Wilayas
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Code)
            .Select(w => new WilayaOption
            {
                Id = w.Id,
                Code = w.Code,
                NameAr = w.NameAr,
                NameFr = w.NameFr,
                ShippingFee = w.ShippingFee
            })
            .ToListAsync();
    }

    private static string Normalize(string value) => value?.Trim() ?? string.Empty;
}

public sealed class WilayaSummary
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public bool IsActive { get; set; }
    public int CommuneCount { get; set; }
}

public sealed class CommuneSummary
{
    public int Id { get; set; }
    public int WilayaId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WilayaFormInput
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CommuneFormInput
{
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class WilayaOption
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }

    public string LocalizedName => PublicCulture.Name(NameAr, NameFr);
}

public sealed class CommuneOption
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;

    public string LocalizedName => PublicCulture.Name(NameAr, NameFr);
}

public sealed class DeliveryZonesIndexViewModel
{
    public IReadOnlyList<WilayaSummary> Wilayas { get; set; } = [];
    public IReadOnlyList<CommuneSummary> Communes { get; set; } = [];
    public int SelectedWilayaId { get; set; }
    public WilayaSummary? SelectedWilaya { get; set; }
}