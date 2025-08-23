using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Infrastructure.Data;

/// <summary>
/// Optimized version of ApplicationDbContext with additional indexes and query optimizations
/// </summary>
public partial class ApplicationDbContext
{
    /// <summary>
    /// Configure additional indexes for performance optimization
    /// </summary>
    private void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // CustomerOrder performance indexes
        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            // Composite indexes for common query patterns
            entity.HasIndex(e => new { e.Status, e.RequestedDeliveryDate })
                  .HasDatabaseName("IX_CustomerOrders_Status_DeliveryDate");
            
            entity.HasIndex(e => new { e.ProductType, e.Status })
                  .HasDatabaseName("IX_CustomerOrders_ProductType_Status");
            
            entity.HasIndex(e => new { e.CustomerId, e.Status })
                  .HasDatabaseName("IX_CustomerOrders_CustomerId_Status");
            
            entity.HasIndex(e => new { e.RequestedDeliveryDate, e.Status })
                  .HasDatabaseName("IX_CustomerOrders_DeliveryDate_Status");
            
            entity.HasIndex(e => new { e.CreatedAt, e.Status })
                  .HasDatabaseName("IX_CustomerOrders_CreatedAt_Status");
            
            // Index for overdue orders query
            entity.HasIndex(e => new { e.RequestedDeliveryDate, e.Status, e.ProductType })
                  .HasDatabaseName("IX_CustomerOrders_OverdueQuery");
            
            // Index for dashboard queries
            entity.HasIndex(e => new { e.ProductType, e.RequestedDeliveryDate, e.Status })
                  .HasDatabaseName("IX_CustomerOrders_Dashboard");
        });

        // OrderItem performance indexes
        modelBuilder.Entity<OrderItem>(entity =>
        {
            // Composite index for order items with product code
            entity.HasIndex(e => new { e.OrderId, e.ProductCode })
                  .HasDatabaseName("IX_OrderItems_OrderId_ProductCode");
            
            // Index for price calculations
            entity.HasIndex(e => new { e.OrderId, e.UnitPrice })
                  .HasDatabaseName("IX_OrderItems_OrderId_UnitPrice");
        });

        // PurchaseOrder performance indexes
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            // Composite indexes for common queries
            entity.HasIndex(e => new { e.SupplierId, e.Status })
                  .HasDatabaseName("IX_PurchaseOrders_SupplierId_Status");
            
            entity.HasIndex(e => new { e.CustomerOrderId, e.Status })
                  .HasDatabaseName("IX_PurchaseOrders_CustomerOrderId_Status");
            
            entity.HasIndex(e => new { e.RequiredDeliveryDate, e.Status })
                  .HasDatabaseName("IX_PurchaseOrders_DeliveryDate_Status");
            
            entity.HasIndex(e => new { e.CreatedBy, e.CreatedAt })
                  .HasDatabaseName("IX_PurchaseOrders_CreatedBy_CreatedAt");
        });

        // SupplierCapability performance indexes
        modelBuilder.Entity<SupplierCapability>(entity =>
        {
            // Index for capacity queries
            entity.HasIndex(e => new { e.ProductType, e.IsActive, e.MaxMonthlyCapacity })
                  .HasDatabaseName("IX_SupplierCapabilities_ProductType_Active_Capacity");
            
            entity.HasIndex(e => new { e.SupplierId, e.IsActive })
                  .HasDatabaseName("IX_SupplierCapabilities_SupplierId_Active");
        });

        // SupplierPerformanceMetrics performance indexes
        modelBuilder.Entity<SupplierPerformanceMetrics>(entity =>
        {
            // Index for performance queries
            entity.HasIndex(e => new { e.OnTimeDeliveryRate, e.QualityScore })
                  .HasDatabaseName("IX_SupplierPerformance_OnTime_Quality");
            
            entity.HasIndex(e => new { e.LastUpdated, e.SupplierId })
                  .HasDatabaseName("IX_SupplierPerformance_LastUpdated_SupplierId");
        });

        // AuditLog performance indexes
        modelBuilder.Entity<AuditLog>(entity =>
        {
            // Composite indexes for audit queries
            entity.HasIndex(e => new { e.Timestamp, e.Action })
                  .HasDatabaseName("IX_AuditLogs_Timestamp_Action");
            
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp })
                  .HasDatabaseName("IX_AuditLogs_Entity_Timestamp");
            
            entity.HasIndex(e => new { e.UserId, e.Action, e.Timestamp })
                  .HasDatabaseName("IX_AuditLogs_User_Action_Timestamp");
        });

        // NotificationLog performance indexes
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            // Index for notification processing
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
                  .HasDatabaseName("IX_NotificationLogs_Status_Priority_Created");
            
            entity.HasIndex(e => new { e.Recipient, e.Type, e.CreatedAt })
                  .HasDatabaseName("IX_NotificationLogs_Recipient_Type_Created");
        });
    }
}