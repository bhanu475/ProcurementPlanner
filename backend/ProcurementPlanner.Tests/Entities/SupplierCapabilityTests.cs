using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class SupplierCapabilityTests
{
    [Fact]
    public void SupplierCapability_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act
        var validationResults = ValidateModel(capability);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SupplierCapability_WithInvalidMaxMonthlyCapacity_ShouldFailValidation(int capacity)
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = capacity;

        // Act
        var validationResults = ValidateModel(capability);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("MaxMonthlyCapacity"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void SupplierCapability_WithNegativeCurrentCommitments_ShouldFailValidation(int commitments)
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.CurrentCommitments = commitments;

        // Act
        var validationResults = ValidateModel(capability);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("CurrentCommitments"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void SupplierCapability_WithInvalidQualityRating_ShouldFailValidation(decimal rating)
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.QualityRating = rating;

        // Act
        var validationResults = ValidateModel(capability);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("QualityRating"));
    }

    [Fact]
    public void SupplierCapability_AvailableCapacity_ShouldCalculateCorrectly()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 30;

        // Act
        var availableCapacity = capability.AvailableCapacity;

        // Assert
        Assert.Equal(70, availableCapacity);
    }

    [Fact]
    public void SupplierCapability_AvailableCapacity_WithOverCommitment_ShouldReturnZero()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 120;

        // Act
        var availableCapacity = capability.AvailableCapacity;

        // Assert
        Assert.Equal(0, availableCapacity);
    }

    [Fact]
    public void SupplierCapability_CapacityUtilizationRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 75;

        // Act
        var utilizationRate = capability.CapacityUtilizationRate;

        // Assert
        Assert.Equal(0.75m, utilizationRate);
    }

    [Fact]
    public void SupplierCapability_CapacityUtilizationRate_WithZeroCapacity_ShouldReturnZero()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 0;
        capability.CurrentCommitments = 0;

        // Act
        var utilizationRate = capability.CapacityUtilizationRate;

        // Assert
        Assert.Equal(0, utilizationRate);
    }

    [Fact]
    public void SupplierCapability_IsOverCommitted_WithExcessCommitments_ShouldReturnTrue()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 120;

        // Act
        var isOverCommitted = capability.IsOverCommitted;

        // Assert
        Assert.True(isOverCommitted);
    }

    [Fact]
    public void SupplierCapability_IsOverCommitted_WithNormalCommitments_ShouldReturnFalse()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 80;

        // Act
        var isOverCommitted = capability.IsOverCommitted;

        // Assert
        Assert.False(isOverCommitted);
    }

    [Fact]
    public void SupplierCapability_CanAccommodate_WithSufficientCapacity_ShouldReturnTrue()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 30;

        // Act
        var canAccommodate = capability.CanAccommodate(50);

        // Assert
        Assert.True(canAccommodate);
    }

    [Fact]
    public void SupplierCapability_CanAccommodate_WithInsufficientCapacity_ShouldReturnFalse()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 30;

        // Act
        var canAccommodate = capability.CanAccommodate(80);

        // Assert
        Assert.False(canAccommodate);
    }

    [Fact]
    public void SupplierCapability_AddCommitment_WithValidQuantity_ShouldIncreaseCommitments()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 30;

        // Act
        capability.AddCommitment(20);

        // Assert
        Assert.Equal(50, capability.CurrentCommitments);
    }

    [Fact]
    public void SupplierCapability_AddCommitment_WithZeroQuantity_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.AddCommitment(0));
    }

    [Fact]
    public void SupplierCapability_AddCommitment_WithNegativeQuantity_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.AddCommitment(-10));
    }

    [Fact]
    public void SupplierCapability_AddCommitment_ExceedingCapacity_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 30;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => capability.AddCommitment(80));
    }

    [Fact]
    public void SupplierCapability_RemoveCommitment_WithValidQuantity_ShouldDecreaseCommitments()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 100;
        capability.CurrentCommitments = 50;

        // Act
        capability.RemoveCommitment(20);

        // Assert
        Assert.Equal(30, capability.CurrentCommitments);
    }

    [Fact]
    public void SupplierCapability_RemoveCommitment_WithZeroQuantity_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.RemoveCommitment(0));
    }

    [Fact]
    public void SupplierCapability_RemoveCommitment_ExceedingCurrentCommitments_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.CurrentCommitments = 30;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => capability.RemoveCommitment(50));
    }

    [Fact]
    public void SupplierCapability_ValidateCapacity_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act & Assert
        capability.ValidateCapacity(); // Should not throw
    }

    [Fact]
    public void SupplierCapability_ValidateCapacity_WithInvalidMaxCapacity_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.MaxMonthlyCapacity = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.ValidateCapacity());
    }

    [Fact]
    public void SupplierCapability_ValidateCapacity_WithNegativeCommitments_ShouldThrowException()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();
        capability.CurrentCommitments = -10;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.ValidateCapacity());
    }

    [Fact]
    public void SupplierCapability_UpdateQualityRating_WithValidRating_ShouldUpdateRating()
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act
        capability.UpdateQualityRating(4.5m);

        // Assert
        Assert.Equal(4.5m, capability.QualityRating);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void SupplierCapability_UpdateQualityRating_WithInvalidRating_ShouldThrowException(decimal rating)
    {
        // Arrange
        var capability = CreateValidSupplierCapability();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capability.UpdateQualityRating(rating));
    }

    [Fact]
    public void SupplierCapability_IsActive_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        var capability = new SupplierCapability();

        // Assert
        Assert.True(capability.IsActive);
    }

    private static SupplierCapability CreateValidSupplierCapability()
    {
        return new SupplierCapability
        {
            Id = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            ProductType = ProductType.LMR,
            MaxMonthlyCapacity = 100,
            CurrentCommitments = 30,
            QualityRating = 4.0m,
            IsActive = true,
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