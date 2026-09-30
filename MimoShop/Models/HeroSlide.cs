namespace MimoShop.Models;

public sealed class HeroSlide
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string CtaText { get; set; } = string.Empty;

    public string CtaUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
