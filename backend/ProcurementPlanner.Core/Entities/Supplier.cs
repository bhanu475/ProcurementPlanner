using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class Supplier : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? ContactPersonName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public List<SupplierCapability> Capabilities { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    public SupplierPerformanceMetrics? Performance { get; set; }

    // Business logic methods
    public int GetAvailableCapacity(ProductType productType)
    {
        var capability = Capabilities.FirstOrDefault(c => c.ProductType == productType);
        return capability?.AvailableCapacity ?? 0;
    }

    public bool CanHandleProductType(ProductType productType)
    {
        return Capabilities.Any(c => c.ProductType == productType && c.IsActive);
    }

    public bool HasCapacityFor(ProductType productType, int requiredQuantity)
    {
        var availableCapacity = GetAvailableCapacity(productType);
        return availableCapacity >= requiredQuantity;
    }

    public void ValidateContactInformation()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Supplier name is required", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(ContactEmail))
        {
            throw new ArgumentException("Contact email is required", nameof(ContactEmail));
        }

        if (string.IsNullOrWhiteSpace(ContactPhone))
        {
            throw new ArgumentException("Contact phone is required", nameof(ContactPhone));
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            throw new ArgumentException("Address is required", nameof(Address));
        }
    }

    public decimal GetQualityRating(ProductType productType)
    {
        var capability = Capabilities.FirstOrDefault(c => c.ProductType == productType);
        return capability?.QualityRating ?? 0;
    }
}