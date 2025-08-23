using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Models;

/// <summary>
/// DTO for supplier order confirmation request
/// </summary>
public class SupplierOrderConfirmationDto
{
    [MaxLength(1000)]
    public string? SupplierNotes { get; set; }

    public List<SupplierItemUpdateDto> ItemUpdates { get; set; } = new();

    public bool AcceptOrder { get; set; } = true;
}

/// <summary>
/// DTO for updating individual purchase order items
/// </summary>
public class SupplierItemUpdateDto
{
    [Required]
    public Guid PurchaseOrderItemId { get; set; }

    [MaxLength(500)]
    public string? PackagingDetails { get; set; }

    [MaxLength(100)]
    public string? DeliveryMethod { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal? UnitPrice { get; set; }

    [MaxLength(500)]
    public string? SupplierNotes { get; set; }
}

/// <summary>
/// DTO for purchase order rejection
/// </summary>
public class PurchaseOrderRejectionDto
{
    [Required]
    [MaxLength(500)]
    public string RejectionReason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for supplier dashboard summary response
/// </summary>
public class SupplierDashboardDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int PendingOrdersCount { get; set; }
    public int ConfirmedOrdersCount { get; set; }
    public int InProductionOrdersCount { get; set; }
    public int OverdueOrdersCount { get; set; }
    public decimal TotalPendingValue { get; set; }
    public decimal TotalConfirmedValue { get; set; }
    public List<PurchaseOrderSummaryDto> RecentOrders { get; set; } = new();
    public List<PurchaseOrderSummaryDto> UpcomingDeliveries { get; set; } = new();
    public SupplierPerformanceDto Performance { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// DTO for supplier performance metrics
/// </summary>
public class SupplierPerformanceDto
{
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public int OrdersCompletedThisMonth { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime LastDelivery { get; set; }
}

/// <summary>
/// DTO for purchase order summary
/// </summary>
public class PurchaseOrderSummaryDto
{
    public Guid Id { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; }
    public DateTime RequiredDeliveryDate { get; set; }
    public decimal? TotalValue { get; set; }
    public int TotalQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDelivery { get; set; }
}

/// <summary>
/// DTO for detailed purchase order view
/// </summary>
public class PurchaseOrderDetailDto
{
    public Guid Id { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public Guid CustomerOrderId { get; set; }
    public string CustomerOrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTime RequiredDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? SupplierNotes { get; set; }
    public string? RejectionReason { get; set; }
    public decimal? TotalValue { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDelivery { get; set; }
    public List<PurchaseOrderItemDetailDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for purchase order item details
/// </summary>
public class PurchaseOrderItemDetailDto
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AllocatedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? PackagingDetails { get; set; }
    public string? DeliveryMethod { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Specifications { get; set; }
    public string? SupplierNotes { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// DTO for supplier order history filter
/// </summary>
public class SupplierOrderHistoryFilterDto
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
/// DTO for delivery date validation result
/// </summary>
public class DeliveryDateValidationDto
{
    public bool IsValid { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new();
    public List<ValidationWarningDto> Warnings { get; set; } = new();
    public DateTime CustomerRequiredDate { get; set; }
}

/// <summary>
/// DTO for validation error
/// </summary>
public class ValidationErrorDto
{
    public Guid ItemId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for validation warning
/// </summary>
public class ValidationWarningDto
{
    public Guid ItemId { get; set; }
    public string Message { get; set; } = string.Empty;
}