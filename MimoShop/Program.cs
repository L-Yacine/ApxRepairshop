using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MimoShop.Data;
using MimoShop.Localization;
using MimoShop.Services;
using MimoShop.Services.Telegram;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<MimoShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<StaffAccountService>();
builder.Services.AddScoped<RepairIntakeService>();
builder.Services.AddScoped<RepairWorkflowService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CatalogImageService>();
builder.Services.AddScoped<PublicCatalogQueryService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DeliveryZoneService>();
builder.Services.AddScoped<ShopOrderService>();
builder.Services.AddHttpClient(GsmArenaCatalogImageProvider.HttpClientName, client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<GsmArenaCatalogImageProvider>();
builder.Services.AddScoped<ICatalogImageProvider>(provider =>
    provider.GetRequiredService<GsmArenaCatalogImageProvider>());

builder.Services.AddHttpClient(IFixitCatalogImageProvider.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://www.ifixit.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<IFixitCatalogImageProvider>();

builder.Services.AddScoped<CatalogImageFetchService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ShopSettingsService>();
builder.Services.AddScoped<SeedImportService>();
builder.Services.AddScoped<HeroSlideService>();

builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));
builder.Services.AddScoped<PartsCatalogQueryService>();
builder.Services.AddScoped<RepairStatusQueryService>();
builder.Services.AddSingleton<RepairStatusInputState>();
builder.Services.AddSingleton<ChatMenuState>();
builder.Services.AddScoped<TelegramBotUpdateHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Staff/Account/Login";
        options.LogoutPath = "/Staff/Account/Logout";
        options.AccessDeniedPath = "/Staff/Account/AccessDenied";
        options.Cookie.Name = "MimoShop.Staff";
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

// Public storefront localization (French default, plus Arabic and English).
builder.Services.AddLocalization();
builder.Services.AddRouting(options =>
    options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint)));

// Session-based cart storage for the anonymous public storefront.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Staff/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Resolve/persist culture and canonicalize public URLs under /{culture}/.
app.UseMiddleware<PublicCultureMiddleware>();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Staff back-office lives under /Staff and is registered before the public
// root route so the literal "Staff" segment always wins. The public
// storefront sits at the site root: bare / resolves to the Home landing
// page; catalog browsing lives under /Catalog/*. Catalog drill-down uses
// multi-segment paths (Brand → Model → PartType → Variants).
app.MapAreaControllerRoute(
    name: "staff",
    areaName: "Staff",
    pattern: "Staff/{controller=Home}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "public-catalog-parttypes",
    areaName: "Public",
    pattern: "{culture:culture=fr}/Catalog/PartTypes/{brandId}/{modelId}",
    defaults: new { controller = "Catalog", action = "PartTypes" });

app.MapAreaControllerRoute(
    name: "public-catalog-variants",
    areaName: "Public",
    pattern: "{culture:culture=fr}/Catalog/Variants/{brandId}/{modelId}/{partTypeId}",
    defaults: new { controller = "Catalog", action = "Variants" });

app.MapAreaControllerRoute(
    name: "public",
    areaName: "Public",
    pattern: "{culture:culture=fr}/{controller=Home}/{action=Index}/{id?}");


app.Run();
