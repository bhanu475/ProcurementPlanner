using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class Supplier : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    public SupplierStatus Status { get; set; } = SupplierStatus.Active;

    [Range(0, 100, ErrorMessage = "Performance score must be between 0 and 100")]
    public decimal PerformanceScore { get; set; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "Lead time must be non-negative")]
    public int LeadTimeDays { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "Capacity must be non-negative")]
    public int MonthlyCapacity { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "Current utilization must be non-negative")]
    public int CurrentUtilization { get; set; } = 0;

    public DateTime? LastPerformanceUpdate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public List<SupplierCapability> Capabilities { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();

    // Calculated properties
    public decimal UtilizationPercentage => MonthlyCapacity > 0 ? 
        Math.Min(100, (decimal)CurrentUtilization / MonthlyCapacity * 100) : 0;

    public int AvailableCapacity => Math.Max(0, MonthlyCapacity - CurrentUtilization);

    public bool IsOverCapacity => CurrentUtilization > MonthlyCapacity;

    // Business logic methods
    public bool CanHandleQuantity(int quantity)
    {
        return AvailableCapacity >= quantity;
    }

    public void UpdatePerformanceScore(decimal newScore)
    {
        if (newScore < 0 || newScore > 100)
        {
            throw new ArgumentException("Performance score must be between 0 and 100", nameof(newScore));
        }
        
        PerformanceScore = newScore;
        LastPerformanceUpdate = DateTime.UtcNow;
    }

    public void AllocateCapacity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity must be non-negative", nameof(quantity));
        }

        CurrentUtilization += quantity;
    }

    public void ReleaseCapacity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity must be non-negative", nameof(quantity));
        }

        CurrentUtilization = Math.Max(0, CurrentUtilization - quantity);
    }

    public bool HasCapabilityFor(ProductType productType)
    {
        return Capabilities.Any(c => c.ProductType == productType && c.IsActive);
    }
}

public enum SupplierStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    UnderReview = 4
}