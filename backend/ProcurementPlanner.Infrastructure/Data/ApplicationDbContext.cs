using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Infrastructure.Data;

public class ApplicationDbContext : DbContext
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
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
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