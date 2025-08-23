using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<CustomerOrder> CustomerOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierCapability> SupplierCapabilities { get; set; }
    public DbSet<SupplierPerformanceMetrics> SupplierPerformanceMetrics { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<OrderMilestone> OrderMilestones { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<CustomerNotificationPreferences> CustomerNotificationPreferences { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure performance indexes
        ConfigurePerformanceIndexes(modelBuilder);
        
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CustomerOrder entity
        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedDeliveryDate);
            entity.Property(e => e.ProductType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderItem entity
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductCode);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });

        // Configure Supplier entity
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ContactEmail);
            entity.HasIndex(e => e.IsActive);
            entity.HasMany(e => e.Capabilities)
                  .WithOne(c => c.Supplier)
                  .HasForeignKey(c => c.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.PurchaseOrders)
                  .WithOne(p => p.Supplier)
                  .HasForeignKey(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Performance)
                  .WithOne(p => p.Supplier)
                  .HasForeignKey<SupplierPerformanceMetrics>(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SupplierCapability entity
        modelBuilder.Entity<SupplierCapability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SupplierId, e.ProductType }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.ProductType).HasConversion<string>();
            entity.Property(e => e.QualityRating).HasPrecision(3, 2);
        });

        // Configure SupplierPerformanceMetrics entity
        modelBuilder.Entity<SupplierPerformanceMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SupplierId).IsUnique();
            entity.Property(e => e.OnTimeDeliveryRate).HasPrecision(5, 4);
            entity.Property(e => e.QualityScore).HasPrecision(3, 2);
            entity.Property(e => e.AverageDeliveryDays).HasPrecision(5, 2);
            entity.Property(e => e.CustomerSatisfactionRate).HasPrecision(5, 4);
        });

        // Configure PurchaseOrder entity
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PurchaseOrderNumber).IsUnique();
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.CustomerOrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequiredDeliveryDate);
            entity.HasIndex(e => e.CreatedBy);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.HasOne(e => e.CustomerOrder)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerOrderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Items)
                  .WithOne(i => i.PurchaseOrder)
                  .HasForeignKey(i => i.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PurchaseOrderItem entity
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.OrderItemId);
            entity.HasIndex(e => e.ProductCode);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.OrderItem)
                  .WithMany()
                  .HasForeignKey(e => e.OrderItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure OrderStatusHistory entity
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => e.ChangedBy);
            entity.Property(e => e.FromStatus).HasConversion<string>();
            entity.Property(e => e.ToStatus).HasConversion<string>();
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ChangedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure OrderMilestone entity
        modelBuilder.Entity<OrderMilestone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.TargetDate);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure NotificationTemplate entity
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Ignore(e => e.RequiredParameters);
        });

        // Configure NotificationLog entity
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Recipient);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Ignore(e => e.Metadata);
        });

        // Configure CustomerNotificationPreferences entity
        modelBuilder.Entity<CustomerNotificationPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId).IsUnique();
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Username);
            entity.HasIndex(e => e.UserRole);
            entity.HasIndex(e => e.Result);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.Property(e => e.Result).HasConversion<string>();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}