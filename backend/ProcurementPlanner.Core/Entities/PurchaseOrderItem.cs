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
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }

    [MaxLength(1000)]
    public string? Specifications { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;

    // Calculated properties
    public decimal TotalPrice => UnitPrice * Quantity;

    // Business logic methods
    public void ValidateQuantity()
    {
        if (Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0", nameof(Quantity));
        }
    }

    public void ValidateUnitPrice()
    {
        if (UnitPrice < 0)
        {
            throw new ArgumentException("Unit price must be non-negative", nameof(UnitPrice));
        }
    }

    public void UpdateFromOrderItem(OrderItem orderItem)
    {
        if (orderItem == null)
        {
            throw new ArgumentNullException(nameof(orderItem));
        }

        OrderItemId = orderItem.Id;
        ProductCode = orderItem.ProductCode;
        Description = orderItem.Description;
        Quantity = orderItem.Quantity;
        Unit = orderItem.Unit;
        Specifications = orderItem.Specifications;
        
        // Unit price needs to be set separately as it may differ from order item
    }

    public bool MatchesOrderItem(OrderItem orderItem)
    {
        if (orderItem == null) return false;

        return OrderItemId == orderItem.Id &&
               ProductCode == orderItem.ProductCode &&
               Quantity == orderItem.Quantity;
    }
}