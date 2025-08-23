using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Models;

/// <summary>
/// Model for supplier order confirmation with packaging and delivery details
/// </summary>
public class SupplierOrderConfirmation
{
    public string? SupplierNotes { get; set; }
    public List<SupplierItemUpdate> ItemUpdates { get; set; } = new();
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    public bool AcceptOrder { get; set; } = true;
}

/// <summary>
/// Model for updating individual purchase order items with supplier details
/// </summary>
public class SupplierItemUpdate
{
    public Guid PurchaseOrderItemId { get; set; }
    public string? PackagingDetails { get; set; }
    public string? DeliveryMethod { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? SupplierNotes { get; set; }
}

/// <summary>
/// Result of delivery date validation
/// </summary>
public class DeliveryDateValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<DeliveryDateValidationError> Errors { get; set; } = new();
    public List<DeliveryDateValidationWarning> Warnings { get; set; } = new();
    public DateTime CustomerRequiredDate { get; set; }

    public void AddError(Guid itemId, string message)
    {
        Errors.Add(new DeliveryDateValidationError { ItemId = itemId, Message = message });
        IsValid = false;
    }

    public void AddWarning(Guid itemId, string message)
    {
        Warnings.Add(new DeliveryDateValidationWarning { ItemId = itemId, Message = message });
    }
}

/// <summary>
/// Delivery date validation error
/// </summary>
public class DeliveryDateValidationError
{
    public Guid ItemId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Delivery date validation warning
/// </summary>
public class DeliveryDateValidationWarning
{
    public Guid ItemId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Supplier dashboard summary with key metrics
/// </summary>
public class SupplierDashboardSummary
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int PendingOrdersCount { get; set; }
    public int ConfirmedOrdersCount { get; set; }
    public int InProductionOrdersCount { get; set; }
    public int OverdueOrdersCount { get; set; }
    public decimal TotalPendingValue { get; set; }
    public decimal TotalConfirmedValue { get; set; }
    public List<PurchaseOrder> RecentOrders { get; set; } = new();
    public List<PurchaseOrder> UpcomingDeliveries { get; set; } = new();
    public SupplierPerformanceSnapshot Performance { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Snapshot of supplier performance metrics
/// </summary>
public class SupplierPerformanceSnapshot
{
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public int OrdersCompletedThisMonth { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime LastDelivery { get; set; }
}

/// <summary>
/// Filter options for supplier order history
/// </summary>
public class SupplierOrderHistoryFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
    public ProductType? ProductType { get; set; }
    public string? CustomerName { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}



/// <summary>
/// Supplier notification model
/// </summary>
public class SupplierNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    NewPurchaseOrder,
    OrderCancelled,
    OrderModified,
    DeliveryReminder,
    OverdueOrder
}

/// <summary>
/// Notification status
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Cancelled
}

/// <summary>
/// Supplier order confirmation result
/// </summary>
public class SupplierOrderConfirmationResult
{
    public bool IsSuccessful { get; set; }
    public PurchaseOrder? UpdatedOrder { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DeliveryDateValidationResult? ValidationResult { get; set; }

    public void AddError(string error)
    {
        Errors.Add(error);
        IsSuccessful = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}