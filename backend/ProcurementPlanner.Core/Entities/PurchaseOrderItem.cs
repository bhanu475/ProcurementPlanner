using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class PurchaseOrderItem : BaseEntity
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public Guid OrderItemId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Allocated quantity must be greater than 0")]
    public int AllocatedQuantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PackagingDetails { get; set; }

    [MaxLength(100)]
    public string? DeliveryMethod { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal? UnitPrice { get; set; }

    [MaxLength(1000)]
    public string? Specifications { get; set; }

    [MaxLength(500)]
    public string? SupplierNotes { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;

    // Calculated properties
    public decimal TotalPrice => UnitPrice.HasValue ? UnitPrice.Value * AllocatedQuantity : 0;

    public bool HasPackagingDetails => !string.IsNullOrWhiteSpace(PackagingDetails);

    public bool HasDeliveryEstimate => EstimatedDeliveryDate.HasValue;

    public bool IsDeliveryEstimateRealistic => EstimatedDeliveryDate.HasValue && 
                                             EstimatedDeliveryDate.Value <= PurchaseOrder?.RequiredDeliveryDate;

    public int DaysUntilEstimatedDelivery => EstimatedDeliveryDate.HasValue 
        ? (EstimatedDeliveryDate.Value.Date - DateTime.UtcNow.Date).Days 
        : 0;

    // Business logic methods
    public void SetPackagingDetails(string packagingDetails, string? deliveryMethod = null)
    {
        if (string.IsNullOrWhiteSpace(packagingDetails))
        {
            throw new ArgumentException("Packaging details cannot be empty", nameof(packagingDetails));
        }

        if (packagingDetails.Length > 500)
        {
            throw new ArgumentException("Packaging details cannot exceed 500 characters", nameof(packagingDetails));
        }

        PackagingDetails = packagingDetails;

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
        {
            if (deliveryMethod.Length > 100)
            {
                throw new ArgumentException("Delivery method cannot exceed 100 characters", nameof(deliveryMethod));
            }
            DeliveryMethod = deliveryMethod;
        }
    }

    public void SetEstimatedDeliveryDate(DateTime estimatedDate)
    {
        if (estimatedDate <= DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Estimated delivery date must be in the future", nameof(estimatedDate));
        }

        if (PurchaseOrder != null && estimatedDate > PurchaseOrder.RequiredDeliveryDate)
        {
            throw new ArgumentException($"Estimated delivery date cannot be later than required delivery date ({PurchaseOrder.RequiredDeliveryDate:yyyy-MM-dd})", nameof(estimatedDate));
        }

        EstimatedDeliveryDate = estimatedDate;
    }

    public void UpdateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        }

        UnitPrice = unitPrice;
    }

    public void ValidatePurchaseOrderItem()
    {
        if (PurchaseOrderId == Guid.Empty)
        {
            throw new ArgumentException("Purchase order ID is required", nameof(PurchaseOrderId));
        }

        if (OrderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item ID is required", nameof(OrderItemId));
        }

        if (string.IsNullOrWhiteSpace(ProductCode))
        {
            throw new ArgumentException("Product code is required", nameof(ProductCode));
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new ArgumentException("Description is required", nameof(Description));
        }

        if (AllocatedQuantity <= 0)
        {
            throw new ArgumentException("Allocated quantity must be greater than 0", nameof(AllocatedQuantity));
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            throw new ArgumentException("Unit is required", nameof(Unit));
        }

        if (UnitPrice.HasValue && UnitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative", nameof(UnitPrice));
        }
    }

    public void ValidateAllocation(int originalQuantity)
    {
        if (AllocatedQuantity > originalQuantity)
        {
            throw new InvalidOperationException($"Cannot allocate {AllocatedQuantity} units when original order quantity is {originalQuantity}");
        }
    }

    public void AddSupplierNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        if (notes.Length > 500)
        {
            throw new ArgumentException("Supplier notes cannot exceed 500 characters", nameof(notes));
        }

        if (string.IsNullOrWhiteSpace(SupplierNotes))
        {
            SupplierNotes = notes;
        }
        else
        {
            SupplierNotes += $"\n{DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
        }
    }

    public bool IsFullyAllocated(int originalQuantity)
    {
        return AllocatedQuantity == originalQuantity;
    }

    public decimal GetAllocationPercentage(int originalQuantity)
    {
        return originalQuantity > 0 ? (decimal)AllocatedQuantity / originalQuantity * 100 : 0;
    }
}