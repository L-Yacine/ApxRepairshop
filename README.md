# ApxRepairshop

Repair shop management back-office with a public storefront. ASP.NET Core MVC (.NET 10) + EF Core (SQL Server).

- Public storefront (no login): `/`, `/Catalog/*`, `/Cart`, `/Checkout`, `/Search`, `/RepairStatus`
- Staff back-office (login required): `/Staff/<Controller>/<Action>`

## Prerequisites

- .NET 10 SDK
- SQL Server (connection string `DefaultConnection`)

## Run

```bash
dotnet restore MimoShop.slnx
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

New database migration:

```bash
dotnet ef migrations add <MigrationName> --project MimoShop --startup-project MimoShop
```

## Notes

- `MimoShop/appsettings.json` is environment config and is not committed. Create it from your local `appsettings.Development.json` sample.
- `MimoShop/wwwroot/uploads/` holds runtime images and is not committed.
