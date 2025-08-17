using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class SupplierCapability : BaseEntity
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Capacity must be non-negative")]
    public int MonthlyCapacity { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Lead time must be non-negative")]
    public int LeadTimeDays { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative")]
    public decimal? EstimatedUnitCost { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Certifications { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;

    // Business logic methods
    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool CanMeetDeadline(DateTime requestedDate)
    {
        var earliestDelivery = DateTime.UtcNow.AddDays(LeadTimeDays);
        return requestedDate >= earliestDelivery;
    }

    public DateTime GetEarliestDeliveryDate()
    {
        return DateTime.UtcNow.AddDays(LeadTimeDays);
    }
}