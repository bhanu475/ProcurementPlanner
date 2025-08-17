using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class PurchaseOrderItemTests
{
    [Fact]
    public void PurchaseOrderItem_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PurchaseOrderItem_WithInvalidProductCode_ShouldFailValidation(string productCode)
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.ProductCode = productCode;

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ProductCode"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PurchaseOrderItem_WithInvalidDescription_ShouldFailValidation(string description)
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.Description = description;

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Description"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PurchaseOrderItem_WithInvalidAllocatedQuantity_ShouldFailValidation(int quantity)
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = quantity;

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("AllocatedQuantity"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PurchaseOrderItem_WithInvalidUnit_ShouldFailValidation(string unit)
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.Unit = unit;

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Unit"));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void PurchaseOrderItem_WithNegativeUnitPrice_ShouldFailValidation(decimal unitPrice)
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.UnitPrice = unitPrice;

        // Act
        var validationResults = ValidateModel(item);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("UnitPrice"));
    }

    [Fact]
    public void PurchaseOrderItem_TotalPrice_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 10;
        item.UnitPrice = 5.50m;

        // Act
        var totalPrice = item.TotalPrice;

        // Assert
        Assert.Equal(55.0m, totalPrice);
    }

    [Fact]
    public void PurchaseOrderItem_TotalPrice_WithNullUnitPrice_ShouldReturnZero()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 10;
        item.UnitPrice = null;

        // Act
        var totalPrice = item.TotalPrice;

        // Assert
        Assert.Equal(0, totalPrice);
    }

    [Fact]
    public void PurchaseOrderItem_HasPackagingDetails_WithPackagingDetails_ShouldReturnTrue()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.PackagingDetails = "Cardboard boxes, 10 units per box";

        // Act
        var hasDetails = item.HasPackagingDetails;

        // Assert
        Assert.True(hasDetails);
    }

    [Fact]
    public void PurchaseOrderItem_HasPackagingDetails_WithoutPackagingDetails_ShouldReturnFalse()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.PackagingDetails = null;

        // Act
        var hasDetails = item.HasPackagingDetails;

        // Assert
        Assert.False(hasDetails);
    }

    [Fact]
    public void PurchaseOrderItem_HasDeliveryEstimate_WithEstimatedDate_ShouldReturnTrue()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5);

        // Act
        var hasEstimate = item.HasDeliveryEstimate;

        // Assert
        Assert.True(hasEstimate);
    }

    [Fact]
    public void PurchaseOrderItem_HasDeliveryEstimate_WithoutEstimatedDate_ShouldReturnFalse()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.EstimatedDeliveryDate = null;

        // Act
        var hasEstimate = item.HasDeliveryEstimate;

        // Assert
        Assert.False(hasEstimate);
    }

    [Fact]
    public void PurchaseOrderItem_IsDeliveryEstimateRealistic_WithRealisticDate_ShouldReturnTrue()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var purchaseOrder = new PurchaseOrder { RequiredDeliveryDate = DateTime.UtcNow.AddDays(10) };
        item.PurchaseOrder = purchaseOrder;
        item.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(8);

        // Act
        var isRealistic = item.IsDeliveryEstimateRealistic;

        // Assert
        Assert.True(isRealistic);
    }

    [Fact]
    public void PurchaseOrderItem_IsDeliveryEstimateRealistic_WithUnrealisticDate_ShouldReturnFalse()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var purchaseOrder = new PurchaseOrder { RequiredDeliveryDate = DateTime.UtcNow.AddDays(5) };
        item.PurchaseOrder = purchaseOrder;
        item.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(8);

        // Act
        var isRealistic = item.IsDeliveryEstimateRealistic;

        // Assert
        Assert.False(isRealistic);
    }

    [Fact]
    public void PurchaseOrderItem_DaysUntilEstimatedDelivery_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7).Date;

        // Act
        var daysUntilDelivery = item.DaysUntilEstimatedDelivery;

        // Assert
        Assert.Equal(7, daysUntilDelivery);
    }

    [Fact]
    public void PurchaseOrderItem_DaysUntilEstimatedDelivery_WithoutEstimate_ShouldReturnZero()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.EstimatedDeliveryDate = null;

        // Act
        var daysUntilDelivery = item.DaysUntilEstimatedDelivery;

        // Assert
        Assert.Equal(0, daysUntilDelivery);
    }

    [Fact]
    public void PurchaseOrderItem_SetPackagingDetails_WithValidDetails_ShouldSetDetails()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act
        item.SetPackagingDetails("Plastic containers", "Ground shipping");

        // Assert
        Assert.Equal("Plastic containers", item.PackagingDetails);
        Assert.Equal("Ground shipping", item.DeliveryMethod);
    }

    [Fact]
    public void PurchaseOrderItem_SetPackagingDetails_WithEmptyDetails_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.SetPackagingDetails(""));
    }

    [Fact]
    public void PurchaseOrderItem_SetPackagingDetails_WithTooLongDetails_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var longDetails = new string('A', 501); // Exceeds 500 character limit

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.SetPackagingDetails(longDetails));
    }

    [Fact]
    public void PurchaseOrderItem_SetEstimatedDeliveryDate_WithValidDate_ShouldSetDate()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var estimatedDate = DateTime.UtcNow.AddDays(5);

        // Act
        item.SetEstimatedDeliveryDate(estimatedDate);

        // Assert
        Assert.Equal(estimatedDate, item.EstimatedDeliveryDate);
    }

    [Fact]
    public void PurchaseOrderItem_SetEstimatedDeliveryDate_WithPastDate_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.SetEstimatedDeliveryDate(pastDate));
    }

    [Fact]
    public void PurchaseOrderItem_SetEstimatedDeliveryDate_AfterRequiredDate_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var purchaseOrder = new PurchaseOrder { RequiredDeliveryDate = DateTime.UtcNow.AddDays(5) };
        item.PurchaseOrder = purchaseOrder;
        var lateDate = DateTime.UtcNow.AddDays(8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.SetEstimatedDeliveryDate(lateDate));
    }

    [Fact]
    public void PurchaseOrderItem_UpdateUnitPrice_WithValidPrice_ShouldUpdatePrice()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act
        item.UpdateUnitPrice(15.75m);

        // Assert
        Assert.Equal(15.75m, item.UnitPrice);
    }

    [Fact]
    public void PurchaseOrderItem_UpdateUnitPrice_WithNegativePrice_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.UpdateUnitPrice(-5.0m));
    }

    [Fact]
    public void PurchaseOrderItem_ValidatePurchaseOrderItem_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act & Assert
        item.ValidatePurchaseOrderItem(); // Should not throw
    }

    [Fact]
    public void PurchaseOrderItem_ValidatePurchaseOrderItem_WithEmptyProductCode_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.ProductCode = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.ValidatePurchaseOrderItem());
    }

    [Fact]
    public void PurchaseOrderItem_ValidateAllocation_WithValidAllocation_ShouldNotThrow()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 50;

        // Act & Assert
        item.ValidateAllocation(100); // Should not throw
    }

    [Fact]
    public void PurchaseOrderItem_ValidateAllocation_WithExcessiveAllocation_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 150;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => item.ValidateAllocation(100));
    }

    [Fact]
    public void PurchaseOrderItem_AddSupplierNotes_WithValidNotes_ShouldAddNotes()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();

        // Act
        item.AddSupplierNotes("Quality checked and approved");

        // Assert
        Assert.Equal("Quality checked and approved", item.SupplierNotes);
    }

    [Fact]
    public void PurchaseOrderItem_AddSupplierNotes_WithExistingNotes_ShouldAppendWithTimestamp()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.SupplierNotes = "Initial notes";

        // Act
        item.AddSupplierNotes("Additional notes");

        // Assert
        Assert.Contains("Initial notes", item.SupplierNotes);
        Assert.Contains("Additional notes", item.SupplierNotes);
        Assert.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"), item.SupplierNotes);
    }

    [Fact]
    public void PurchaseOrderItem_AddSupplierNotes_WithTooLongNotes_ShouldThrowException()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        var longNotes = new string('A', 501); // Exceeds 500 character limit

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.AddSupplierNotes(longNotes));
    }

    [Fact]
    public void PurchaseOrderItem_IsFullyAllocated_WithFullAllocation_ShouldReturnTrue()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 100;

        // Act
        var isFullyAllocated = item.IsFullyAllocated(100);

        // Assert
        Assert.True(isFullyAllocated);
    }

    [Fact]
    public void PurchaseOrderItem_IsFullyAllocated_WithPartialAllocation_ShouldReturnFalse()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 75;

        // Act
        var isFullyAllocated = item.IsFullyAllocated(100);

        // Assert
        Assert.False(isFullyAllocated);
    }

    [Fact]
    public void PurchaseOrderItem_GetAllocationPercentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 75;

        // Act
        var percentage = item.GetAllocationPercentage(100);

        // Assert
        Assert.Equal(75m, percentage);
    }

    [Fact]
    public void PurchaseOrderItem_GetAllocationPercentage_WithZeroOriginal_ShouldReturnZero()
    {
        // Arrange
        var item = CreateValidPurchaseOrderItem();
        item.AllocatedQuantity = 50;

        // Act
        var percentage = item.GetAllocationPercentage(0);

        // Assert
        Assert.Equal(0, percentage);
    }

    private static PurchaseOrderItem CreateValidPurchaseOrderItem()
    {
        return new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            OrderItemId = Guid.NewGuid(),
            ProductCode = "PROD-001",
            Description = "Test Product",
            AllocatedQuantity = 10,
            Unit = "EA",
            UnitPrice = 5.0m,
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