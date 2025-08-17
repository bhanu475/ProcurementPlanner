using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Models;

public class DistributionSuggestion
{
    public Guid CustomerOrderId { get; set; }
    public int TotalQuantity { get; set; }
    public ProductType ProductType { get; set; }
    public List<SupplierAllocation> Allocations { get; set; } = new();
    public DistributionStrategy Strategy { get; set; }
    public decimal TotalCapacityUtilization { get; set; }
    public string? Notes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public bool IsFullyAllocated => Allocations.Sum(a => a.AllocatedQuantity) == TotalQuantity;
    public int UnallocatedQuantity => Math.Max(0, TotalQuantity - Allocations.Sum(a => a.AllocatedQuantity));
}

public class SupplierAllocation
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

public class SupplierAllocationInfo
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int AvailableCapacity { get; set; }
    public int MaxMonthlyCapacity { get; set; }
    public int CurrentCommitments { get; set; }
    public decimal QualityRating { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public decimal OverallPerformanceScore { get; set; }
    public bool IsPreferredSupplier { get; set; }
    public bool IsReliableSupplier { get; set; }
    public ProductType ProductType { get; set; }
    public DateTime LastUpdated { get; set; }

    public decimal CapacityUtilizationRate => MaxMonthlyCapacity > 0 
        ? (decimal)CurrentCommitments / MaxMonthlyCapacity 
        : 0;
}

public class DistributionPlan
{
    public Guid CustomerOrderId { get; set; }
    public List<SupplierAllocation> Allocations { get; set; } = new();
    public DistributionStrategy Strategy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    public int TotalAllocatedQuantity => Allocations.Sum(a => a.AllocatedQuantity);
}

public class DistributionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<SupplierCapacityValidation> SupplierValidations { get; set; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

public class SupplierCapacityValidation
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableCapacity { get; set; }
    public bool HasSufficientCapacity { get; set; }
    public int CapacityShortfall { get; set; }
    public string? ValidationMessage { get; set; }
}

public enum DistributionStrategy
{
    /// <summary>
    /// Distribute evenly across all eligible suppliers
    /// </summary>
    EvenDistribution,
    
    /// <summary>
    /// Prioritize suppliers with best performance metrics
    /// </summary>
    PerformanceBased,
    
    /// <summary>
    /// Balance between capacity utilization and performance
    /// </summary>
    Balanced,
    
    /// <summary>
    /// Prioritize suppliers with highest available capacity
    /// </summary>
    CapacityBased,
    
    /// <summary>
    /// Custom distribution based on specific business rules
    /// </summary>
    Custom
}

public class SupplierConfirmation
{
    public string? SupplierNotes { get; set; }
    public List<PurchaseOrderItemConfirmation> ItemConfirmations { get; set; } = new();
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    public Guid ConfirmedBy { get; set; }
}

public class PurchaseOrderItemConfirmation
{
    public Guid PurchaseOrderItemId { get; set; }
    public string? PackagingDetails { get; set; }
    public string? DeliveryMethod { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? SupplierNotes { get; set; }
}

public class PurchaseOrderItemUpdate
{
    public string? PackagingDetails { get; set; }
    public string? DeliveryMethod { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? SupplierNotes { get; set; }
    public string? Specifications { get; set; }
}

public class ProcurementAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerOrderId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? AdditionalData { get; set; }
}

public class PurchaseOrderCreationRequest
{
    public Guid CustomerOrderId { get; set; }
    public DistributionPlan DistributionPlan { get; set; } = new();
    public Guid CreatedBy { get; set; }
    public string? Notes { get; set; }
    public bool AutoSendToSuppliers { get; set; } = true;
}

public class PurchaseOrderCreationResult
{
    public List<PurchaseOrder> CreatedPurchaseOrders { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsSuccessful => !Errors.Any();
    public int TotalOrdersCreated => CreatedPurchaseOrders.Count;
    public int TotalQuantityAllocated => CreatedPurchaseOrders.Sum(po => po.TotalQuantity);
}