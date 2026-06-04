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

    public DbSet<InventoryPart> InventoryParts => Set<InventoryPart>();

    public DbSet<RepairPartUsage> RepairPartUsages => Set<RepairPartUsage>();

    public DbSet<InventoryStockMovement> InventoryStockMovements => Set<InventoryStockMovement>();

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

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

            entity.HasData(
                StaffMember.CreateSeed(1, "owner", "owner123", "صاحب المحل", StaffRoles.Owner),
                StaffMember.CreateSeed(2, "worker1", "worker123", "العامل 1", StaffRoles.Worker),
                StaffMember.CreateSeed(3, "worker2", "worker123", "العامل 2", StaffRoles.Worker));
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

        modelBuilder.Entity<InventoryPart>(entity =>
        {
            entity.HasIndex(part => new { part.Brand, part.Model, part.PartType, part.Variant }).IsUnique();
            entity.Property(part => part.Brand).HasMaxLength(60).IsRequired();
            entity.Property(part => part.Model).HasMaxLength(80).IsRequired();
            entity.Property(part => part.PartType).HasMaxLength(80).IsRequired();
            entity.Property(part => part.Variant).HasMaxLength(80).IsRequired();
            entity.Property(part => part.UnitCostPrice).HasPrecision(18, 2);
            entity.Property(part => part.UnitSalePrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<RepairPartUsage>(entity =>
        {
            entity.Property(usage => usage.Brand).HasMaxLength(60).IsRequired();
            entity.Property(usage => usage.Model).HasMaxLength(80).IsRequired();
            entity.Property(usage => usage.PartType).HasMaxLength(80).IsRequired();
            entity.Property(usage => usage.Variant).HasMaxLength(80).IsRequired();
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
            entity.Property(movement => movement.MovementType).HasMaxLength(40).IsRequired();
            entity.Property(movement => movement.CreatedByUsername).HasMaxLength(40).IsRequired();

            entity.HasOne(movement => movement.InventoryPart)
                .WithMany()
                .HasForeignKey(movement => movement.InventoryPartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(movement => movement.RepairTicket)
                .WithMany()
                .HasForeignKey(movement => movement.RepairTicketId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(movement => movement.RepairPartUsage)
                .WithMany()
                .HasForeignKey(movement => movement.RepairPartUsageId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
