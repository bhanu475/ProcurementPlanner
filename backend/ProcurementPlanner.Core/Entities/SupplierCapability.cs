using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class SupplierCapability : BaseEntity
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max monthly capacity must be greater than 0")]
    public int MaxMonthlyCapacity { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Current commitments cannot be negative")]
    public int CurrentCommitments { get; set; }

    [Required]
    [Range(0, 5, ErrorMessage = "Quality rating must be between 0 and 5")]
    public decimal QualityRating { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;

    // Calculated properties
    public int AvailableCapacity => Math.Max(0, MaxMonthlyCapacity - CurrentCommitments);

    public decimal CapacityUtilizationRate => MaxMonthlyCapacity > 0 
        ? (decimal)CurrentCommitments / MaxMonthlyCapacity 
        : 0;

    public bool IsOverCommitted => CurrentCommitments > MaxMonthlyCapacity;

    // Business logic methods
    public bool CanAccommodate(int additionalQuantity)
    {
        return AvailableCapacity >= additionalQuantity;
    }

    public void AddCommitment(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        }

        if (!CanAccommodate(quantity))
        {
            throw new InvalidOperationException($"Cannot accommodate {quantity} units. Available capacity: {AvailableCapacity}");
        }

        CurrentCommitments += quantity;
    }

    public void RemoveCommitment(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        }

        if (quantity > CurrentCommitments)
        {
            throw new InvalidOperationException($"Cannot remove {quantity} units. Current commitments: {CurrentCommitments}");
        }

        CurrentCommitments -= quantity;
    }

    public void ValidateCapacity()
    {
        if (MaxMonthlyCapacity <= 0)
        {
            throw new ArgumentException("Max monthly capacity must be greater than 0", nameof(MaxMonthlyCapacity));
        }

        if (CurrentCommitments < 0)
        {
            throw new ArgumentException("Current commitments cannot be negative", nameof(CurrentCommitments));
        }

        if (QualityRating < 0 || QualityRating > 5)
        {
            throw new ArgumentException("Quality rating must be between 0 and 5", nameof(QualityRating));
        }
    }

    public void UpdateQualityRating(decimal newRating)
    {
        if (newRating < 0 || newRating > 5)
        {
            throw new ArgumentException("Quality rating must be between 0 and 5", nameof(newRating));
        }

        QualityRating = newRating;
    }
}