namespace MimoShop.Models;

public sealed class PartVariant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayNameAr { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
