using Microsoft.AspNetCore.Routing;

namespace MimoShop.Localization;

public sealed class CultureRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out object? value) || value is null)
        {
            return false;
        }

        return value is string culture && PublicCulture.IsSupported(culture);
    }
}
