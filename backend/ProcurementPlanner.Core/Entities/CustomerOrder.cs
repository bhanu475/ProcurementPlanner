using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class CustomerOrder : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty; // DODAAC

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    public DateTime RequestedDeliveryDate { get; set; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    [Required]
    public Guid CreatedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public List<OrderItem> Items { get; set; } = new();

    // Business logic methods
    public bool CanTransitionTo(OrderStatus newStatus)
    {
        return newStatus switch
        {
            OrderStatus.Submitted => false, // Cannot go back to submitted
            OrderStatus.UnderReview => Status == OrderStatus.Submitted,
            OrderStatus.PlanningInProgress => Status == OrderStatus.UnderReview,
            OrderStatus.PurchaseOrdersCreated => Status == OrderStatus.PlanningInProgress,
            OrderStatus.AwaitingSupplierConfirmation => Status == OrderStatus.PurchaseOrdersCreated,
            OrderStatus.InProduction => Status == OrderStatus.AwaitingSupplierConfirmation,
            OrderStatus.ReadyForDelivery => Status == OrderStatus.InProduction,
            OrderStatus.Delivered => Status == OrderStatus.ReadyForDelivery,
            OrderStatus.Cancelled => Status != OrderStatus.Delivered, // Can cancel unless delivered
            _ => false
        };
    }

    public void TransitionTo(OrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");
        }
        Status = newStatus;
    }

    public int TotalQuantity => Items.Sum(item => item.Quantity);

    public bool IsOverdue => Status != OrderStatus.Delivered && 
                           Status != OrderStatus.Cancelled && 
                           RequestedDeliveryDate < DateTime.UtcNow.Date;
}

public enum ProductType
{
    LMR = 1,
    FFV = 2
}

public enum OrderStatus
{
    Submitted = 1,
    UnderReview = 2,
    PlanningInProgress = 3,
    PurchaseOrdersCreated = 4,
    AwaitingSupplierConfirmation = 5,
    InProduction = 6,
    ReadyForDelivery = 7,
    Delivered = 8,
    Cancelled = 9
}