using Microsoft.EntityFrameworkCore;
using MimoShop.Models;
using MimoShop.Services;

namespace MimoShop.Data;

public sealed class MimoShopDbContext : DbContext
{
    public MimoShopDbContext(DbContextOptions<MimoShopDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();

    public DbSet<RepairStatusHistory> RepairStatusHistories => Set<RepairStatusHistory>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<PhoneModel> PhoneModels => Set<PhoneModel>();

    public DbSet<PartType> PartTypes => Set<PartType>();

    public DbSet<PartVariant> PartVariants => Set<PartVariant>();

    public DbSet<InventoryPart> InventoryParts => Set<InventoryPart>();

    public DbSet<RepairPartUsage> RepairPartUsages => Set<RepairPartUsage>();

    public DbSet<InventoryStockMovement> InventoryStockMovements => Set<InventoryStockMovement>();

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    public DbSet<ShopSetting> ShopSettings => Set<ShopSetting>();

    public DbSet<Wilaya> Wilayas => Set<Wilaya>();

    public DbSet<Commune> Communes => Set<Commune>();

    public DbSet<ShopOrder> ShopOrders => Set<ShopOrder>();

    public DbSet<ShopOrderLine> ShopOrderLines => Set<ShopOrderLine>();

    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(customer => customer.Phone).IsUnique();
            entity.Property(customer => customer.Name).HasMaxLength(120).IsRequired();
            entity.Property(customer => customer.Phone).HasMaxLength(40).IsRequired();
            entity.Property(customer => customer.WhatsApp).HasMaxLength(40);
            entity.Property(customer => customer.Telegram).HasMaxLength(80);
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasIndex(staff => staff.Username).IsUnique();
            entity.Property(staff => staff.Username).HasMaxLength(40).IsRequired();
            entity.Property(staff => staff.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(staff => staff.Role).HasMaxLength(30).IsRequired();
            entity.Property(staff => staff.Salt).HasMaxLength(120).IsRequired();
            entity.Property(staff => staff.PasswordHash).HasMaxLength(128).IsRequired();
            entity.Property(staff => staff.IsActive).HasDefaultValue(true);

            entity.HasData(
                StaffMember.CreateSeed(1, "owner", "owner123", "مدير النظام", StaffRoles.SuperAdmin),
                StaffMember.CreateSeed(2, "worker1", "worker123", "العامل 1", StaffRoles.Worker),
                StaffMember.CreateSeed(3, "worker2", "worker123", "العامل 2", StaffRoles.Worker));
        });

        modelBuilder.Entity<ShopSetting>(entity =>
        {
            entity.HasKey(setting => setting.Id);
            entity.Property(setting => setting.Name).HasMaxLength(120).IsRequired();
            entity.Property(setting => setting.LatinName).HasMaxLength(120).IsRequired();
            entity.Property(setting => setting.Phone).HasMaxLength(40).IsRequired();
            entity.Property(setting => setting.WhatsApp).HasMaxLength(40);
            entity.Property(setting => setting.Address).HasMaxLength(200).IsRequired();
            entity.Property(setting => setting.TelegramHandle).HasMaxLength(80);
            entity.Property(setting => setting.OpeningHours).HasMaxLength(200);
            entity.Property(setting => setting.LogoUrl).HasMaxLength(500);

            entity.HasData(new ShopSetting
            {
                Id = 1,
                Name = "ميمو شوب",
                LatinName = "MimoShop",
                Phone = "000 00 00 00",
                WhatsApp = "",
                Address = "الجزائر",
                TelegramHandle = "@mimoshop_bot",
                OpeningHours = "السبت - الخميس 09:00 - 18:00",
                LogoUrl = null
            });
        });

        modelBuilder.Entity<RepairTicket>(entity =>
        {
            entity.HasIndex(ticket => ticket.JobCode).IsUnique();
            entity.Property(ticket => ticket.JobCode).HasMaxLength(20).IsRequired();
            entity.Property(ticket => ticket.DeviceBrand).HasMaxLength(60).IsRequired();
            entity.Property(ticket => ticket.DeviceModel).HasMaxLength(80).IsRequired();
            entity.Property(ticket => ticket.ProblemDescription).HasMaxLength(1000).IsRequired();
            entity.Property(ticket => ticket.AssignedWorkerUsername).HasMaxLength(40).IsRequired();
            entity.Property(ticket => ticket.AssignedWorkerName).HasMaxLength(120).IsRequired();
            entity.Property(ticket => ticket.EstimatedPrice).HasPrecision(18, 2);
            entity.Property(ticket => ticket.AmountPaid).HasPrecision(18, 2);
            entity.Property(ticket => ticket.Notes).HasMaxLength(1000);
            entity.Property(ticket => ticket.Status).HasMaxLength(40).IsRequired();

            entity.HasOne(ticket => ticket.Customer)
                .WithMany(customer => customer.RepairTickets)
                .HasForeignKey(ticket => ticket.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairStatusHistory>(entity =>
        {
            entity.Property(history => history.Status).HasMaxLength(40).IsRequired();
            entity.Property(history => history.ChangedByUsername).HasMaxLength(40).IsRequired();

            entity.HasOne(history => history.RepairTicket)
                .WithMany(ticket => ticket.StatusHistory)
                .HasForeignKey(history => history.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasIndex(brand => brand.Name).IsUnique();
            entity.Property(brand => brand.Name).HasMaxLength(60).IsRequired();
            entity.Property(brand => brand.DisplayNameAr).HasMaxLength(120).IsRequired();
            entity.Property(brand => brand.ImageUrl).HasMaxLength(500);
            entity.Property(brand => brand.ThumbnailUrl).HasMaxLength(500);

            entity.HasData(
                new Brand { Id = 1, Name = "Samsung", DisplayNameAr = "سامسونج", SortOrder = 10, IsActive = true },
                new Brand { Id = 2, Name = "Apple", DisplayNameAr = "أبل", SortOrder = 20, IsActive = true },
                new Brand { Id = 3, Name = "Xiaomi", DisplayNameAr = "شاومي", SortOrder = 30, IsActive = true },
                new Brand { Id = 4, Name = "Huawei", DisplayNameAr = "هواوي", SortOrder = 40, IsActive = true },
                new Brand { Id = 5, Name = "Oppo", DisplayNameAr = "أوبو", SortOrder = 50, IsActive = true },
                new Brand { Id = 6, Name = "Realme", DisplayNameAr = "ريلمي", SortOrder = 60, IsActive = true },
                new Brand { Id = 7, Name = "Infinix", DisplayNameAr = "إنفينيكس", SortOrder = 70, IsActive = true },
                new Brand { Id = 8, Name = "Tecno", DisplayNameAr = "تكنو", SortOrder = 80, IsActive = true },
                new Brand { Id = 9, Name = "Nokia", DisplayNameAr = "نوكيا", SortOrder = 90, IsActive = true },
                new Brand { Id = 10, Name = "OnePlus", DisplayNameAr = "ون بلس", SortOrder = 100, IsActive = true });
        });

        modelBuilder.Entity<PhoneModel>(entity =>
        {
            entity.HasIndex(model => new { model.BrandId, model.Name }).IsUnique();
            entity.Property(model => model.Name).HasMaxLength(80).IsRequired();
            entity.Property(model => model.DisplayNameAr).HasMaxLength(120).IsRequired();
            entity.Property(model => model.ImageUrl).HasMaxLength(500);
            entity.Property(model => model.ThumbnailUrl).HasMaxLength(500);

            entity.HasOne(model => model.Brand)
                .WithMany(brand => brand.PhoneModels)
                .HasForeignKey(model => model.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PartType>(entity =>
        {
            entity.HasIndex(type => type.Name).IsUnique();
            entity.Property(type => type.Name).HasMaxLength(80).IsRequired();
            entity.Property(type => type.DisplayNameAr).HasMaxLength(120).IsRequired();
            entity.Property(type => type.ImageUrl).HasMaxLength(500);
            entity.Property(type => type.ThumbnailUrl).HasMaxLength(500);

            entity.HasData(
                new PartType { Id = 1, Name = "Screen", DisplayNameAr = "شاشة", SortOrder = 10, IsActive = true },
                new PartType { Id = 2, Name = "Battery", DisplayNameAr = "بطارية", SortOrder = 20, IsActive = true },
                new PartType { Id = 3, Name = "Back cover", DisplayNameAr = "غطاء خلفي", SortOrder = 30, IsActive = true },
                new PartType { Id = 4, Name = "Charging port", DisplayNameAr = "منفذ شحن", SortOrder = 40, IsActive = true },
                new PartType { Id = 5, Name = "Camera", DisplayNameAr = "كاميرا", SortOrder = 50, IsActive = true },
                new PartType { Id = 6, Name = "Speaker", DisplayNameAr = "سماعة", SortOrder = 60, IsActive = true },
                new PartType { Id = 7, Name = "Microphone", DisplayNameAr = "مايكروفون", SortOrder = 70, IsActive = true });
        });

        modelBuilder.Entity<PartVariant>(entity =>
        {
            entity.HasIndex(variant => variant.Name).IsUnique();
            entity.Property(variant => variant.Name).HasMaxLength(80).IsRequired();
            entity.Property(variant => variant.DisplayNameAr).HasMaxLength(120).IsRequired();

            entity.HasData(
                new PartVariant { Id = 1, Name = "Original OEM", DisplayNameAr = "أصلي", SortOrder = 10, IsActive = true },
                new PartVariant { Id = 2, Name = "Compatible", DisplayNameAr = "متوافق", SortOrder = 20, IsActive = true },
                new PartVariant { Id = 3, Name = "Refurbished", DisplayNameAr = "مجدد", SortOrder = 30, IsActive = true });
        });

        modelBuilder.Entity<InventoryPart>(entity =>
        {
            entity.HasIndex(part => new { part.BrandId, part.PhoneModelId, part.PartTypeId, part.PartVariantId }).IsUnique();
            entity.Property(part => part.ImageUrl).HasMaxLength(500);
            entity.Property(part => part.ThumbnailUrl).HasMaxLength(500);
            entity.Property(part => part.UnitCostPrice).HasPrecision(18, 2);
            entity.Property(part => part.UnitSalePrice).HasPrecision(18, 2);

            entity.HasOne(part => part.Brand)
                .WithMany()
                .HasForeignKey(part => part.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(part => part.PhoneModel)
                .WithMany()
                .HasForeignKey(part => part.PhoneModelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(part => part.PartType)
                .WithMany()
                .HasForeignKey(part => part.PartTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(part => part.PartVariant)
                .WithMany()
                .HasForeignKey(part => part.PartVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairPartUsage>(entity =>
        {
            entity.Property(usage => usage.BrandName).HasMaxLength(60).IsRequired();
            entity.Property(usage => usage.PhoneModelName).HasMaxLength(80).IsRequired();
            entity.Property(usage => usage.PartTypeName).HasMaxLength(80).IsRequired();
            entity.Property(usage => usage.PartVariantName).HasMaxLength(80).IsRequired();
            entity.Property(usage => usage.UnitCostPrice).HasPrecision(18, 2);
            entity.Property(usage => usage.UnitSalePrice).HasPrecision(18, 2);
            entity.Property(usage => usage.CreatedByUsername).HasMaxLength(40).IsRequired();

            entity.HasOne(usage => usage.RepairTicket)
                .WithMany(ticket => ticket.PartUsages)
                .HasForeignKey(usage => usage.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(usage => usage.InventoryPart)
                .WithMany(part => part.RepairPartUsages)
                .HasForeignKey(usage => usage.InventoryPartId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryStockMovement>(entity =>
{
    entity.Property(movement => movement.MovementType)
        .HasMaxLength(40)
        .IsRequired();

    entity.Property(movement => movement.CreatedByUsername)
        .HasMaxLength(40)
        .IsRequired();

    // InventoryPart should never be automatically deleted if stock
    // movement history exists. Stock movements are audit records.
    entity.HasOne(movement => movement.InventoryPart)
        .WithMany()
        .HasForeignKey(movement => movement.InventoryPartId)
        .OnDelete(DeleteBehavior.Restrict);

    // IMPORTANT:
    // Changed from SetNull -> NoAction
    //
    // SQL Server detected multiple cascade paths:
    //
    // RepairTicket
    //   -> InventoryStockMovement
    //   -> RepairPartUsage -> InventoryStockMovement
    //
    // SetNull is considered a cascading action by SQL Server.
    // NoAction removes that cascade path.
    //
    // This also preserves stock movement history even if someone
    // attempts to delete a repair ticket.
    entity.HasOne(movement => movement.RepairTicket)
        .WithMany()
        .HasForeignKey(movement => movement.RepairTicketId)
        .OnDelete(DeleteBehavior.NoAction);

    // IMPORTANT:
    // Changed from SetNull -> NoAction
    //
    // Stock movements should remain immutable historical records.
    // We don't want EF or SQL Server automatically modifying them.
    //
    // This also removes the second cascade path that SQL Server
    // complains about.
    entity.HasOne(movement => movement.RepairPartUsage)
        .WithMany()
        .HasForeignKey(movement => movement.RepairPartUsageId)
        .OnDelete(DeleteBehavior.NoAction);
});

        modelBuilder.Entity<Wilaya>(entity =>
        {
            entity.HasIndex(wilaya => wilaya.Code).IsUnique();
            entity.HasIndex(wilaya => wilaya.NameAr).IsUnique();
            entity.HasIndex(wilaya => wilaya.NameFr).IsUnique();
            entity.Property(wilaya => wilaya.Code).HasMaxLength(2).IsRequired();
            entity.Property(wilaya => wilaya.NameAr).HasMaxLength(100).IsRequired();
            entity.Property(wilaya => wilaya.NameFr).HasMaxLength(100).IsRequired();
            entity.Property(wilaya => wilaya.ShippingFee).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(wilaya => wilaya.IsActive).HasDefaultValue(true);

            entity.HasData(WilayaCommuneSeed.Wilayas().ToArray());
        });

        modelBuilder.Entity<Commune>(entity =>
        {
            entity.HasIndex(commune => new { commune.WilayaId, commune.NameAr }).IsUnique();
            entity.Property(commune => commune.NameAr).HasMaxLength(100).IsRequired();
            entity.Property(commune => commune.NameFr).HasMaxLength(100).IsRequired();
            entity.Property(commune => commune.IsActive).HasDefaultValue(true);

            entity.HasOne(commune => commune.Wilaya)
                .WithMany(wilaya => wilaya.Communes)
                .HasForeignKey(commune => commune.WilayaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShopOrder>(entity =>
        {
            entity.HasIndex(order => order.OrderCode).IsUnique();
            entity.Property(order => order.OrderCode).HasMaxLength(12).IsRequired();
            entity.Property(order => order.CustomerName).HasMaxLength(120).IsRequired();
            entity.Property(order => order.CustomerPhone).HasMaxLength(40).IsRequired();
            entity.Property(order => order.CustomerWhatsApp).HasMaxLength(40);
            entity.Property(order => order.WilayaName).HasMaxLength(100).IsRequired();
            entity.Property(order => order.CommuneName).HasMaxLength(100).IsRequired();
            entity.Property(order => order.CommuneNameFr).HasMaxLength(100).IsRequired();
            entity.Property(order => order.Address).HasMaxLength(300).IsRequired();
            entity.Property(order => order.Notes).HasMaxLength(500);
            entity.Property(order => order.Status).HasMaxLength(30).HasDefaultValue(ShopOrderStatuses.New).IsRequired();
            entity.Property(order => order.Subtotal).HasPrecision(18, 2).IsRequired();
            entity.Property(order => order.ShippingFee).HasPrecision(18, 2).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(order => order.ReturnReason).HasMaxLength(300);
            entity.Property(order => order.CancelledReason).HasMaxLength(300);

            // Wilaya/Commune set null on order would be destructive to snapshots;
            // Restrict prevents accidental data loss if a zone is deleted.
            // The DeliveryZoneService blocks deletion when orders reference it.
            entity.HasOne(order => order.Wilaya)
                .WithMany()
                .HasForeignKey(order => order.WilayaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(order => order.Commune)
                .WithMany()
                .HasForeignKey(order => order.CommuneId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShopOrderLine>(entity =>
        {
            entity.Property(line => line.BrandName).HasMaxLength(60).IsRequired();
            entity.Property(line => line.ModelName).HasMaxLength(80).IsRequired();
            entity.Property(line => line.PartTypeName).HasMaxLength(80).IsRequired();
            entity.Property(line => line.VariantName).HasMaxLength(80).IsRequired();
            entity.Property(line => line.PartDisplayName).HasMaxLength(300).IsRequired();
            entity.Property(line => line.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(line => line.ImageUrl).HasMaxLength(500);

            entity.HasOne(line => line.ShopOrder)
                .WithMany(order => order.Lines)
                .HasForeignKey(line => line.ShopOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(line => line.InventoryPart)
                .WithMany()
                .HasForeignKey(line => line.InventoryPartId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<HeroSlide>(entity =>
        {
            entity.Property(slide => slide.Title).HasMaxLength(160).IsRequired();
            entity.Property(slide => slide.Subtitle).HasMaxLength(300);
            entity.Property(slide => slide.ImageUrl).HasMaxLength(500);
            entity.Property(slide => slide.CtaText).HasMaxLength(80);
            entity.Property(slide => slide.CtaUrl).HasMaxLength(500);
            entity.Property(slide => slide.IsActive).HasDefaultValue(true);
        });
    }
}
