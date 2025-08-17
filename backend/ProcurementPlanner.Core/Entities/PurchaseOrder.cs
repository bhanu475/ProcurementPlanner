using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class PurchaseOrder : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string PurchaseOrderNumber { get; set; } = string.Empty;

    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public Guid CustomerOrderId { get; set; }

    [Required]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [Required]
    public DateTime RequestedDeliveryDate { get; set; }

    public DateTime? ConfirmedDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Total amount must be non-negative")]
    public decimal TotalAmount { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? SupplierNotes { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public CustomerOrder CustomerOrder { get; set; } = null!;
    public List<PurchaseOrderItem> Items { get; set; } = new();

    // Calculated properties
    public bool IsOverdue => Status != PurchaseOrderStatus.Delivered && 
                           Status != PurchaseOrderStatus.Cancelled &&
                           (ConfirmedDeliveryDate ?? RequestedDeliveryDate) < DateTime.UtcNow.Date;

    public int DaysUntilDelivery
    {
        get
        {
            var targetDate = ConfirmedDeliveryDate ?? RequestedDeliveryDate;
            return (int)(targetDate.Date - DateTime.UtcNow.Date).TotalDays;
        }
    }

    public decimal CalculatedTotalAmount => Items.Sum(item => item.TotalPrice);

    // Business logic methods
    public bool CanTransitionTo(PurchaseOrderStatus newStatus)
    {
        return newStatus switch
        {
            PurchaseOrderStatus.Draft => false, // Cannot go back to draft
            PurchaseOrderStatus.Sent => Status == PurchaseOrderStatus.Draft,
            PurchaseOrderStatus.Acknowledged => Status == PurchaseOrderStatus.Sent,
            PurchaseOrderStatus.InProduction => Status == PurchaseOrderStatus.Acknowledged,
            PurchaseOrderStatus.ReadyForShipment => Status == PurchaseOrderStatus.InProduction,
            PurchaseOrderStatus.Shipped => Status == PurchaseOrderStatus.ReadyForShipment,
            PurchaseOrderStatus.Delivered => Status == PurchaseOrderStatus.Shipped,
            PurchaseOrderStatus.Cancelled => Status != PurchaseOrderStatus.Delivered,
            _ => false
        };
    }

    public void TransitionTo(PurchaseOrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");
        }

        Status = newStatus;

        // Set delivery date when delivered
        if (newStatus == PurchaseOrderStatus.Delivered && ActualDeliveryDate == null)
        {
            ActualDeliveryDate = DateTime.UtcNow;
        }
    }

    public void ConfirmDeliveryDate(DateTime confirmedDate)
    {
        if (confirmedDate < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Confirmed delivery date cannot be in the past", nameof(confirmedDate));
        }

        ConfirmedDeliveryDate = confirmedDate;
    }

    public void UpdateTotalAmount()
    {
        TotalAmount = CalculatedTotalAmount;
    }

    public void AddItem(PurchaseOrderItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        item.PurchaseOrderId = Id;
        Items.Add(item);
        UpdateTotalAmount();
    }

    public void RemoveItem(PurchaseOrderItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        Items.Remove(item);
        UpdateTotalAmount();
    }
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    Acknowledged = 3,
    InProduction = 4,
    ReadyForShipment = 5,
    Shipped = 6,
    Delivered = 7,
    Cancelled = 8
}