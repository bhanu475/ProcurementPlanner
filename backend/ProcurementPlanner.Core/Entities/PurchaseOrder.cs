using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class PurchaseOrder : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string PurchaseOrderNumber { get; set; } = string.Empty;

    [Required]
    public Guid CustomerOrderId { get; set; }

    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Created;

    [Required]
    public DateTime RequiredDeliveryDate { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    [MaxLength(1000)]
    public string? SupplierNotes { get; set; }

    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    [MaxLength(100)]
    public string? RejectionReason { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Total value must be non-negative")]
    public decimal? TotalValue { get; set; }

    // Navigation properties
    public CustomerOrder CustomerOrder { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public List<PurchaseOrderItem> Items { get; set; } = new();

    // Calculated properties
    public int TotalQuantity => Items.Sum(item => item.AllocatedQuantity);

    public bool IsOverdue => Status != PurchaseOrderStatus.Delivered && 
                           Status != PurchaseOrderStatus.Cancelled && 
                           RequiredDeliveryDate < DateTime.UtcNow.Date;

    public bool IsAwaitingSupplierResponse => Status == PurchaseOrderStatus.SentToSupplier;

    public bool IsConfirmed => Status == PurchaseOrderStatus.Confirmed;

    public bool IsRejected => Status == PurchaseOrderStatus.Rejected;

    public int DaysUntilDelivery => (RequiredDeliveryDate.Date - DateTime.UtcNow.Date).Days;

    // Business logic methods
    public bool CanTransitionTo(PurchaseOrderStatus newStatus)
    {
        return newStatus switch
        {
            PurchaseOrderStatus.Created => false, // Cannot go back to created
            PurchaseOrderStatus.SentToSupplier => Status == PurchaseOrderStatus.Created,
            PurchaseOrderStatus.Confirmed => Status == PurchaseOrderStatus.SentToSupplier,
            PurchaseOrderStatus.Rejected => Status == PurchaseOrderStatus.SentToSupplier,
            PurchaseOrderStatus.InProduction => Status == PurchaseOrderStatus.Confirmed,
            PurchaseOrderStatus.ReadyForShipment => Status == PurchaseOrderStatus.InProduction,
            PurchaseOrderStatus.Shipped => Status == PurchaseOrderStatus.ReadyForShipment,
            PurchaseOrderStatus.Delivered => Status == PurchaseOrderStatus.Shipped,
            PurchaseOrderStatus.Cancelled => Status != PurchaseOrderStatus.Delivered && Status != PurchaseOrderStatus.Cancelled,
            _ => false
        };
    }

    public void TransitionTo(PurchaseOrderStatus newStatus, string? notes = null)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");
        }

        var previousStatus = Status;
        Status = newStatus;

        // Set timestamps for specific status changes
        switch (newStatus)
        {
            case PurchaseOrderStatus.Confirmed:
                ConfirmedAt = DateTime.UtcNow;
                break;
            case PurchaseOrderStatus.Rejected:
                RejectedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(notes))
                {
                    RejectionReason = notes;
                }
                break;
            case PurchaseOrderStatus.Shipped:
                ShippedAt = DateTime.UtcNow;
                break;
            case PurchaseOrderStatus.Delivered:
                DeliveredAt = DateTime.UtcNow;
                break;
        }

        if (!string.IsNullOrEmpty(notes))
        {
            SupplierNotes = notes;
        }
    }

    public void ConfirmOrder(string? supplierNotes = null)
    {
        TransitionTo(PurchaseOrderStatus.Confirmed, supplierNotes);
    }

    public void RejectOrder(string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("Rejection reason is required", nameof(rejectionReason));
        }

        TransitionTo(PurchaseOrderStatus.Rejected, rejectionReason);
    }

    public void ValidatePurchaseOrder()
    {
        if (string.IsNullOrWhiteSpace(PurchaseOrderNumber))
        {
            throw new ArgumentException("Purchase order number is required", nameof(PurchaseOrderNumber));
        }

        if (CustomerOrderId == Guid.Empty)
        {
            throw new ArgumentException("Customer order ID is required", nameof(CustomerOrderId));
        }

        if (SupplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier ID is required", nameof(SupplierId));
        }

        if (RequiredDeliveryDate <= DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Required delivery date must be in the future", nameof(RequiredDeliveryDate));
        }

        if (!Items.Any())
        {
            throw new InvalidOperationException("Purchase order must have at least one item");
        }
    }

    public void CalculateTotalValue()
    {
        TotalValue = Items.Sum(item => item.TotalPrice);
    }

    public bool HasItemForProduct(string productCode)
    {
        return Items.Any(item => item.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase));
    }

    public PurchaseOrderItem? GetItemByProductCode(string productCode)
    {
        return Items.FirstOrDefault(item => item.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase));
    }
}

public enum PurchaseOrderStatus
{
    Created = 1,
    SentToSupplier = 2,
    Confirmed = 3,
    Rejected = 4,
    InProduction = 5,
    ReadyForShipment = 6,
    Shipped = 7,
    Delivered = 8,
    Cancelled = 9
}