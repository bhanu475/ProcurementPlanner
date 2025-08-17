using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class SupplierPerformanceMetrics : BaseEntity
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    [Range(0, 1, ErrorMessage = "On-time delivery rate must be between 0 and 1")]
    public decimal OnTimeDeliveryRate { get; set; }

    [Required]
    [Range(0, 5, ErrorMessage = "Quality score must be between 0 and 5")]
    public decimal QualityScore { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Total orders completed cannot be negative")]
    public int TotalOrdersCompleted { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Total orders on time cannot be negative")]
    public int TotalOrdersOnTime { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Total orders late cannot be negative")]
    public int TotalOrdersLate { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Total orders cancelled cannot be negative")]
    public int TotalOrdersCancelled { get; set; }

    [Required]
    public DateTime LastUpdated { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Average delivery days cannot be negative")]
    public decimal? AverageDeliveryDays { get; set; }

    [Range(0, 1, ErrorMessage = "Customer satisfaction rate must be between 0 and 1")]
    public decimal? CustomerSatisfactionRate { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;

    // Calculated properties
    public decimal OverallPerformanceScore => CalculateOverallScore();

    public bool IsReliableSupplier => OnTimeDeliveryRate >= 0.85m && QualityScore >= 3.5m;

    public bool IsPreferredSupplier => OnTimeDeliveryRate >= 0.95m && QualityScore >= 4.0m;

    public decimal CancellationRate => TotalOrdersCompleted > 0 
        ? (decimal)TotalOrdersCancelled / (TotalOrdersCompleted + TotalOrdersCancelled) 
        : 0;

    // Business logic methods
    public void UpdateOnTimeDeliveryMetrics(bool wasOnTime)
    {
        TotalOrdersCompleted++;
        
        if (wasOnTime)
        {
            TotalOrdersOnTime++;
        }
        else
        {
            TotalOrdersLate++;
        }

        RecalculateOnTimeDeliveryRate();
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateQualityScore(decimal newScore)
    {
        if (newScore < 0 || newScore > 5)
        {
            throw new ArgumentException("Quality score must be between 0 and 5", nameof(newScore));
        }

        // Weighted average with existing score
        if (TotalOrdersCompleted > 0)
        {
            QualityScore = ((QualityScore * TotalOrdersCompleted) + newScore) / (TotalOrdersCompleted + 1);
        }
        else
        {
            QualityScore = newScore;
        }

        LastUpdated = DateTime.UtcNow;
    }

    public void RecordCancelledOrder()
    {
        TotalOrdersCancelled++;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateAverageDeliveryDays(int deliveryDays)
    {
        if (deliveryDays < 0)
        {
            throw new ArgumentException("Delivery days cannot be negative", nameof(deliveryDays));
        }

        if (AverageDeliveryDays.HasValue && TotalOrdersCompleted > 0)
        {
            AverageDeliveryDays = ((AverageDeliveryDays.Value * TotalOrdersCompleted) + deliveryDays) / (TotalOrdersCompleted + 1);
        }
        else
        {
            AverageDeliveryDays = deliveryDays;
        }

        LastUpdated = DateTime.UtcNow;
    }

    private void RecalculateOnTimeDeliveryRate()
    {
        var totalDeliveries = TotalOrdersOnTime + TotalOrdersLate;
        OnTimeDeliveryRate = totalDeliveries > 0 ? (decimal)TotalOrdersOnTime / totalDeliveries : 0;
    }

    private decimal CalculateOverallScore()
    {
        // Weighted score: 40% on-time delivery, 40% quality, 20% customer satisfaction
        var score = (OnTimeDeliveryRate * 0.4m) + ((QualityScore / 5) * 0.4m);
        
        if (CustomerSatisfactionRate.HasValue)
        {
            score += (CustomerSatisfactionRate.Value * 0.2m);
        }
        else
        {
            // If no customer satisfaction data, redistribute weight
            score = (OnTimeDeliveryRate * 0.5m) + ((QualityScore / 5) * 0.5m);
        }

        return Math.Round(score, 3);
    }

    public void ValidateMetrics()
    {
        if (OnTimeDeliveryRate < 0 || OnTimeDeliveryRate > 1)
        {
            throw new ArgumentException("On-time delivery rate must be between 0 and 1", nameof(OnTimeDeliveryRate));
        }

        if (QualityScore < 0 || QualityScore > 5)
        {
            throw new ArgumentException("Quality score must be between 0 and 5", nameof(QualityScore));
        }

        if (TotalOrdersOnTime + TotalOrdersLate != TotalOrdersCompleted)
        {
            throw new InvalidOperationException("Total orders on time plus late must equal total orders completed");
        }

        if (CustomerSatisfactionRate.HasValue && (CustomerSatisfactionRate < 0 || CustomerSatisfactionRate > 1))
        {
            throw new ArgumentException("Customer satisfaction rate must be between 0 and 1", nameof(CustomerSatisfactionRate));
        }
    }
}