using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MimoShop.Data;
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
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    ;


app.Run();
