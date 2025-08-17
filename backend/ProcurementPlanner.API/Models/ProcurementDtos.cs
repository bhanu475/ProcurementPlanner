using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.API.Models;

public class CreatePurchaseOrdersRequest
{
    [Required]
    public Guid CustomerOrderId { get; set; }

    [Required]
    public DistributionPlanDto DistributionPlan { get; set; } = new();

    public string? Notes { get; set; }

    public bool AutoSendToSuppliers { get; set; } = true;
}

public class DistributionPlanDto
{
    [Required]
    public List<SupplierAllocationDto> Allocations { get; set; } = new();

    public DistributionStrategy Strategy { get; set; } = DistributionStrategy.Balanced;

    public string? Notes { get; set; }
}

public class SupplierAllocationDto
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Allocated quantity must be greater than 0")]
    public int AllocatedQuantity { get; set; }

    public string? AllocationReason { get; set; }
}

public class SupplierConfirmationRequest
{
    public string? SupplierNotes { get; set; }

    public List<PurchaseOrderItemConfirmationDto> ItemConfirmations { get; set; } = new();
}

public class PurchaseOrderItemConfirmationDto
{
    [Required]
    public Guid PurchaseOrderItemId { get; set; }

    public string? PackagingDetails { get; set; }

    public string? DeliveryMethod { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal? UnitPrice { get; set; }

    public string? SupplierNotes { get; set; }
}

public class PurchaseOrderItemUpdateRequest
{
    public string? PackagingDetails { get; set; }

    public string? DeliveryMethod { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal? UnitPrice { get; set; }

    public string? SupplierNotes { get; set; }

    public string? Specifications { get; set; }
}

public class RejectPurchaseOrderRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Rejection reason is required")]
    public string RejectionReason { get; set; } = string.Empty;
}

public class DistributionSuggestionResponse
{
    public Guid CustomerOrderId { get; set; }
    public int TotalQuantity { get; set; }
    public ProductType ProductType { get; set; }
    public List<SupplierAllocationResponse> Allocations { get; set; } = new();
    public DistributionStrategy Strategy { get; set; }
    public decimal TotalCapacityUtilization { get; set; }
    public string? Notes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsFullyAllocated { get; set; }
    public int UnallocatedQuantity { get; set; }
}

public class SupplierAllocationResponse
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int AllocatedQuantity { get; set; }
    public decimal AllocationPercentage { get; set; }
    public int AvailableCapacity { get; set; }
    public decimal PerformanceScore { get; set; }
    public decimal QualityRating { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public string? AllocationReason { get; set; }
}

public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public Guid CustomerOrderId { get; set; }
    public string CustomerOrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; }
    public DateTime RequiredDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? SupplierNotes { get; set; }
    public string? RejectionReason { get; set; }
    public decimal? TotalValue { get; set; }
    public int TotalQuantity { get; set; }
    public bool IsOverdue { get; set; }
    public List<PurchaseOrderItemResponse> Items { get; set; } = new();
}

public class PurchaseOrderItemResponse
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AllocatedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? PackagingDetails { get; set; }
    public string? DeliveryMethod { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string? Specifications { get; set; }
    public string? SupplierNotes { get; set; }
}

public class DistributionValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<SupplierCapacityValidationResponse> SupplierValidations { get; set; } = new();
}

public class SupplierCapacityValidationResponse
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableCapacity { get; set; }
    public bool HasSufficientCapacity { get; set; }
    public int CapacityShortfall { get; set; }
    public string? ValidationMessage { get; set; }
}