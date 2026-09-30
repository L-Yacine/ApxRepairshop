using System.Globalization;

namespace MimoShop.Localization;

public static class PublicCulture
{
    public const string French = "fr";
    public const string Arabic = "ar";
    public const string English = "en";
    public const string Default = French;

    public static readonly IReadOnlyList<string> Supported = [French, Arabic, English];

    public static bool IsArabic =>
        CultureInfo.CurrentUICulture.Name.StartsWith(Arabic, StringComparison.OrdinalIgnoreCase);

    public static string TwoLetter
    {
        get
        {
            string two = CultureInfo.CurrentUICulture.Name.Split('-')[0].ToLowerInvariant();
            return Supported.Contains(two, StringComparer.OrdinalIgnoreCase) ? two : Default;
        }
    }

    public static bool IsRtl => IsArabic;

    public static string CultureName(string twoLetter) => twoLetter switch
    {
        Arabic => "ar-DZ",
        English => "en-US",
        _ => "fr-FR"
    };

    /// <summary>
    /// Returns the Arabic display name for Arabic UI culture, otherwise the
    /// Latin name (used as the French/English fallback per the catalog model).
    /// </summary>
    public static string Name(string? displayNameAr, string? latin)
    {
        string ar = displayNameAr ?? string.Empty;
        string other = latin ?? string.Empty;
        if (IsArabic)
        {
            return string.IsNullOrWhiteSpace(ar) ? other : ar;
        }
        return string.IsNullOrWhiteSpace(other) ? ar : other;
    }

    public static bool IsSupported(string? twoLetter) =>
        twoLetter is not null && Supported.Contains(twoLetter, StringComparer.OrdinalIgnoreCase);
}
