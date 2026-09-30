using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
using MimoShop.Models;

namespace MimoShop.Services;

public sealed class HeroSlideService
{
    private readonly MimoShopDbContext dbContext;

    public HeroSlideService(MimoShopDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<HeroSlide>> GetActiveSlidesAsync()
    {
        return await dbContext.HeroSlides
            .AsNoTracking()
            .Where(slide => slide.IsActive)
            .OrderBy(slide => slide.SortOrder)
            .ThenBy(slide => slide.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<HeroSlide>> ListAllAsync()
    {
        return await dbContext.HeroSlides
            .AsNoTracking()
            .OrderBy(slide => slide.SortOrder)
            .ThenBy(slide => slide.Id)
            .ToListAsync();
    }

    public async Task<HeroSlide?> FindAsync(int id)
    {
        return await dbContext.HeroSlides
            .AsNoTracking()
            .SingleOrDefaultAsync(slide => slide.Id == id);
    }

    public async Task CreateAsync(HeroSlide slide)
    {
        dbContext.HeroSlides.Add(slide);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(HeroSlide slide)
    {
        HeroSlide? existing = await dbContext.HeroSlides.SingleOrDefaultAsync(s => s.Id == slide.Id);
        if (existing is null)
        {
            return false;
        }

        existing.Title = slide.Title;
        existing.Subtitle = slide.Subtitle;
        existing.ImageUrl = slide.ImageUrl;
        existing.CtaText = slide.CtaText;
        existing.CtaUrl = slide.CtaUrl;
        existing.SortOrder = slide.SortOrder;
        existing.IsActive = slide.IsActive;

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleAsync(int id)
    {
        HeroSlide? existing = await dbContext.HeroSlides.SingleOrDefaultAsync(s => s.Id == id);
        if (existing is null)
        {
            return false;
        }

        existing.IsActive = !existing.IsActive;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        HeroSlide? existing = await dbContext.HeroSlides.SingleOrDefaultAsync(s => s.Id == id);
        if (existing is null)
        {
            return false;
        }

        dbContext.HeroSlides.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
