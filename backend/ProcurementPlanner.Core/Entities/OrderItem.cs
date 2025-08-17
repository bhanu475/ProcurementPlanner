using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class OrderItem : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

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

    [MaxLength(1000)]
    public string? Specifications { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal? UnitPrice { get; set; }

    // Navigation properties
    public CustomerOrder Order { get; set; } = null!;

    // Calculated properties
    public decimal TotalPrice => UnitPrice.HasValue ? UnitPrice.Value * Quantity : 0;

    // Business logic methods
    public void ValidateQuantity()
    {
        if (Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0", nameof(Quantity));
        }
    }

    public void ValidateProductCode()
    {
        if (string.IsNullOrWhiteSpace(ProductCode))
        {
            throw new ArgumentException("Product code is required", nameof(ProductCode));
        }
    }

    public void ValidateSpecifications()
    {
        if (!string.IsNullOrEmpty(Specifications) && Specifications.Length > 1000)
        {
            throw new ArgumentException("Specifications cannot exceed 1000 characters", nameof(Specifications));
        }
    }
}