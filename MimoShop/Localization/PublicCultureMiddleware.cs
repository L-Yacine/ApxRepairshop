using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;

namespace MimoShop.Localization;

/// <summary>
/// Resolves the request culture for the public storefront, persists it in a
/// cookie, and canonicalizes URLs so every public page lives under a
/// /{culture}/ prefix. Staff and static-asset paths are left untouched.
/// </summary>
public sealed class PublicCultureMiddleware
{
    private static readonly HashSet<string> IgnoredFirstSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "staff",
        "css",
        "js",
        "lib",
        "images",
        "uploads",
        "fonts",
        "favicon.ico",
        "manifest.json",
        "robots.txt",
        "sitemap.xml"
    };

    private readonly RequestDelegate next;

    public PublicCultureMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "/";
        string first = FirstSegment(path);

        bool isStaff = string.Equals(first, "staff", StringComparison.OrdinalIgnoreCase);
        bool isIgnored = IgnoredFirstSegments.Contains(first);

        if (!isStaff && !isIgnored)
        {
            string culture = ResolveCulture(context, first);
            ApplyCulture(culture);

            if (context.Request.Method is "GET" or "HEAD")
            {
                PersistCookie(context, culture);

                if (!PublicCulture.IsSupported(first) && IsNavigation(context))
                {
                    context.Response.Redirect(BuildCanonicalUrl(context, culture), permanent: false);
                    return;
                }
            }
        }

        await next(context);
    }

    private static string ResolveCulture(HttpContext context, string first)
    {
        if (PublicCulture.IsSupported(first))
        {
            return first.ToLowerInvariant();
        }

        string? fromQuery = context.Request.Query["culture"].ToString();
        if (PublicCulture.IsSupported(fromQuery))
        {
            return fromQuery!.ToLowerInvariant();
        }

        string? cookie = context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
        if (!string.IsNullOrEmpty(cookie))
        {
            ProviderCultureResult? parsed = CookieRequestCultureProvider.ParseCookieValue(cookie);
            if (parsed is not null)
            {
                foreach (StringSegment ui in parsed.UICultures.Concat(parsed.Cultures))
                {
                    string two = ui.ToString().Split('-')[0].ToLowerInvariant();
                    if (PublicCulture.IsSupported(two))
                    {
                        return two;
                    }
                }
            }
        }

        string? accept = context.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(accept))
        {
            foreach (string part in accept.Split(','))
            {
                string two = part.Split(';')[0].Trim().Split('-')[0].ToLowerInvariant();
                if (PublicCulture.IsSupported(two))
                {
                    return two;
                }
            }
        }

        return PublicCulture.Default;
    }

    private static void ApplyCulture(string culture)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(PublicCulture.CultureName(culture));
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
    }

    private static void PersistCookie(HttpContext context, string culture)
    {
        string value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            value,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
    }

    private static bool IsNavigation(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Requested-With"))
        {
            return false;
        }

        string? accept = context.Request.Headers.Accept.ToString();
        return string.IsNullOrEmpty(accept)
            || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCanonicalUrl(HttpContext context, string culture)
    {
        string path = context.Request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && PublicCulture.IsSupported(segments[0]))
        {
            segments[0] = culture;
        }
        else
        {
            segments.Insert(0, culture);
        }

        string basePath = "/" + string.Join("/", segments);
        if (path.EndsWith('/') && basePath.Length > 1)
        {
            basePath += "/";
        }

        return basePath + context.Request.QueryString.Value;
    }

    private static string FirstSegment(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? string.Empty : segments[0];
    }
}
