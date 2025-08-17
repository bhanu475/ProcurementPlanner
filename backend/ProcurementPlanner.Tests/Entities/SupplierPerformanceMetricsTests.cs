using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class SupplierPerformanceMetricsTests
{
    [Fact]
    public void SupplierPerformanceMetrics_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();

        // Act
        var validationResults = ValidateModel(metrics);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SupplierPerformanceMetrics_WithInvalidOnTimeDeliveryRate_ShouldFailValidation(decimal rate)
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = rate;

        // Act
        var validationResults = ValidateModel(metrics);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("OnTimeDeliveryRate"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void SupplierPerformanceMetrics_WithInvalidQualityScore_ShouldFailValidation(decimal score)
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.QualityScore = score;

        // Act
        var validationResults = ValidateModel(metrics);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("QualityScore"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void SupplierPerformanceMetrics_WithNegativeTotalOrdersCompleted_ShouldFailValidation(int orders)
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = orders;

        // Act
        var validationResults = ValidateModel(metrics);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("TotalOrdersCompleted"));
    }

    [Fact]
    public void SupplierPerformanceMetrics_OverallPerformanceScore_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.9m; // 90%
        metrics.QualityScore = 4.0m; // 4.0/5 = 0.8
        metrics.CustomerSatisfactionRate = 0.85m; // 85%

        // Act
        var overallScore = metrics.OverallPerformanceScore;

        // Assert
        // Expected: (0.9 * 0.4) + (0.8 * 0.4) + (0.85 * 0.2) = 0.36 + 0.32 + 0.17 = 0.85
        Assert.Equal(0.85m, overallScore);
    }

    [Fact]
    public void SupplierPerformanceMetrics_OverallPerformanceScore_WithoutCustomerSatisfaction_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.9m; // 90%
        metrics.QualityScore = 4.0m; // 4.0/5 = 0.8
        metrics.CustomerSatisfactionRate = null;

        // Act
        var overallScore = metrics.OverallPerformanceScore;

        // Assert
        // Expected: (0.9 * 0.5) + (0.8 * 0.5) = 0.45 + 0.4 = 0.85
        Assert.Equal(0.85m, overallScore);
    }

    [Fact]
    public void SupplierPerformanceMetrics_IsReliableSupplier_WithGoodMetrics_ShouldReturnTrue()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.9m;
        metrics.QualityScore = 4.0m;

        // Act
        var isReliable = metrics.IsReliableSupplier;

        // Assert
        Assert.True(isReliable);
    }

    [Fact]
    public void SupplierPerformanceMetrics_IsReliableSupplier_WithPoorOnTimeDelivery_ShouldReturnFalse()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.8m; // Below 0.85 threshold
        metrics.QualityScore = 4.0m;

        // Act
        var isReliable = metrics.IsReliableSupplier;

        // Assert
        Assert.False(isReliable);
    }

    [Fact]
    public void SupplierPerformanceMetrics_IsPreferredSupplier_WithExcellentMetrics_ShouldReturnTrue()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.96m;
        metrics.QualityScore = 4.5m;

        // Act
        var isPreferred = metrics.IsPreferredSupplier;

        // Assert
        Assert.True(isPreferred);
    }

    [Fact]
    public void SupplierPerformanceMetrics_IsPreferredSupplier_WithGoodButNotExcellentMetrics_ShouldReturnFalse()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 0.9m; // Below 0.95 threshold
        metrics.QualityScore = 4.5m;

        // Act
        var isPreferred = metrics.IsPreferredSupplier;

        // Assert
        Assert.False(isPreferred);
    }

    [Fact]
    public void SupplierPerformanceMetrics_CancellationRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = 80;
        metrics.TotalOrdersCancelled = 20;

        // Act
        var cancellationRate = metrics.CancellationRate;

        // Assert
        Assert.Equal(0.2m, cancellationRate); // 20/(80+20) = 0.2
    }

    [Fact]
    public void SupplierPerformanceMetrics_CancellationRate_WithNoOrders_ShouldReturnZero()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = 0;
        metrics.TotalOrdersCancelled = 0;

        // Act
        var cancellationRate = metrics.CancellationRate;

        // Assert
        Assert.Equal(0, cancellationRate);
    }

    [Fact]
    public void SupplierPerformanceMetrics_UpdateOnTimeDeliveryMetrics_WithOnTimeDelivery_ShouldUpdateCorrectly()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = 10;
        metrics.TotalOrdersOnTime = 8;
        metrics.TotalOrdersLate = 2;

        // Act
        metrics.UpdateOnTimeDeliveryMetrics(true);

        // Assert
        Assert.Equal(11, metrics.TotalOrdersCompleted);
        Assert.Equal(9, metrics.TotalOrdersOnTime);
        Assert.Equal(2, metrics.TotalOrdersLate);
        Assert.Equal(9m / 11m, metrics.OnTimeDeliveryRate);
    }

    [Fact]
    public void SupplierPerformanceMetrics_UpdateOnTimeDeliveryMetrics_WithLateDelivery_ShouldUpdateCorrectly()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = 10;
        metrics.TotalOrdersOnTime = 8;
        metrics.TotalOrdersLate = 2;

        // Act
        metrics.UpdateOnTimeDeliveryMetrics(false);

        // Assert
        Assert.Equal(11, metrics.TotalOrdersCompleted);
        Assert.Equal(8, metrics.TotalOrdersOnTime);
        Assert.Equal(3, metrics.TotalOrdersLate);
        Assert.Equal(8m / 11m, metrics.OnTimeDeliveryRate);
    }

    [Fact]
    public void SupplierPerformanceMetrics_UpdateQualityScore_WithValidScore_ShouldUpdateWeightedAverage()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.QualityScore = 4.0m;
        metrics.TotalOrdersCompleted = 10;

        // Act
        metrics.UpdateQualityScore(5.0m);

        // Assert
        // Expected: ((4.0 * 10) + 5.0) / (10 + 1) = 45 / 11 ≈ 4.09
        Assert.Equal(Math.Round(45m / 11m, 3), Math.Round(metrics.QualityScore, 3));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void SupplierPerformanceMetrics_UpdateQualityScore_WithInvalidScore_ShouldThrowException(decimal score)
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => metrics.UpdateQualityScore(score));
    }

    [Fact]
    public void SupplierPerformanceMetrics_RecordCancelledOrder_ShouldIncrementCancelledCount()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        var initialCancelled = metrics.TotalOrdersCancelled;

        // Act
        metrics.RecordCancelledOrder();

        // Assert
        Assert.Equal(initialCancelled + 1, metrics.TotalOrdersCancelled);
    }

    [Fact]
    public void SupplierPerformanceMetrics_UpdateAverageDeliveryDays_WithValidDays_ShouldUpdateWeightedAverage()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.AverageDeliveryDays = 5.0m;
        metrics.TotalOrdersCompleted = 10;

        // Act
        metrics.UpdateAverageDeliveryDays(7);

        // Assert
        // Expected: ((5.0 * 10) + 7) / (10 + 1) = 57 / 11 ≈ 5.18
        Assert.Equal(Math.Round(57m / 11m, 2), Math.Round(metrics.AverageDeliveryDays.Value, 2));
    }

    [Fact]
    public void SupplierPerformanceMetrics_UpdateAverageDeliveryDays_WithNegativeDays_ShouldThrowException()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => metrics.UpdateAverageDeliveryDays(-1));
    }

    [Fact]
    public void SupplierPerformanceMetrics_ValidateMetrics_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();

        // Act & Assert
        metrics.ValidateMetrics(); // Should not throw
    }

    [Fact]
    public void SupplierPerformanceMetrics_ValidateMetrics_WithInvalidOnTimeDeliveryRate_ShouldThrowException()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.OnTimeDeliveryRate = 1.5m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => metrics.ValidateMetrics());
    }

    [Fact]
    public void SupplierPerformanceMetrics_ValidateMetrics_WithInconsistentOrderCounts_ShouldThrowException()
    {
        // Arrange
        var metrics = CreateValidSupplierPerformanceMetrics();
        metrics.TotalOrdersCompleted = 10;
        metrics.TotalOrdersOnTime = 6;
        metrics.TotalOrdersLate = 3; // 6 + 3 = 9, not 10

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => metrics.ValidateMetrics());
    }

    private static SupplierPerformanceMetrics CreateValidSupplierPerformanceMetrics()
    {
        return new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            OnTimeDeliveryRate = 0.85m,
            QualityScore = 4.0m,
            TotalOrdersCompleted = 100,
            TotalOrdersOnTime = 85,
            TotalOrdersLate = 15,
            TotalOrdersCancelled = 5,
            LastUpdated = DateTime.UtcNow,
            AverageDeliveryDays = 5.5m,
            CustomerSatisfactionRate = 0.9m,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}