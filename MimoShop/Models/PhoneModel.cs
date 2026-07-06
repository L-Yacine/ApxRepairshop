namespace MimoShop.Models;

public sealed class PhoneModel
{
    public int Id { get; set; }

    public int BrandId { get; set; }

    public Brand Brand { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
