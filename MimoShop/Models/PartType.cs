namespace MimoShop.Models;

public sealed class PartType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
