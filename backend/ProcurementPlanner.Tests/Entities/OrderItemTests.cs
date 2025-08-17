using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class OrderItemTests
{
    [Fact]
    public void OrderItem_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            ProductCode = "PROD-001",
            Description = "Test Product",
            Quantity = 10,
            Unit = "EA",
            Specifications = "Test specifications",
            UnitPrice = 25.50m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void OrderItem_WithInvalidProductCode_ShouldFailValidation(string productCode)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.ProductCode = productCode;

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ProductCode"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void OrderItem_WithInvalidDescription_ShouldFailValidation(string description)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Description = description;

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Description"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void OrderItem_WithInvalidQuantity_ShouldFailValidation(int quantity)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Quantity = quantity;

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Quantity"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void OrderItem_WithInvalidUnit_ShouldFailValidation(string unit)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Unit = unit;

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Unit"));
    }

    [Fact]
    public void OrderItem_WithTooLongProductCode_ShouldFailValidation()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.ProductCode = new string('A', 51); // Exceeds 50 character limit

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ProductCode"));
    }

    [Fact]
    public void OrderItem_WithTooLongDescription_ShouldFailValidation()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Description = new string('A', 201); // Exceeds 200 character limit

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Description"));
    }

    [Fact]
    public void OrderItem_WithTooLongUnit_ShouldFailValidation()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Unit = new string('A', 21); // Exceeds 20 character limit

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Unit"));
    }

    [Fact]
    public void OrderItem_WithTooLongSpecifications_ShouldFailValidation()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Specifications = new string('A', 1001); // Exceeds 1000 character limit

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Specifications"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10.50)]
    public void OrderItem_WithNegativeUnitPrice_ShouldFailValidation(decimal unitPrice)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.UnitPrice = unitPrice;

        // Act
        var validationResults = ValidateModel(orderItem);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("UnitPrice"));
    }

    [Fact]
    public void OrderItem_TotalPrice_WithUnitPrice_ShouldCalculateCorrectly()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Quantity = 10;
        orderItem.UnitPrice = 25.50m;

        // Act
        var totalPrice = orderItem.TotalPrice;

        // Assert
        Assert.Equal(255.00m, totalPrice);
    }

    [Fact]
    public void OrderItem_TotalPrice_WithoutUnitPrice_ShouldReturnZero()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Quantity = 10;
        orderItem.UnitPrice = null;

        // Act
        var totalPrice = orderItem.TotalPrice;

        // Assert
        Assert.Equal(0m, totalPrice);
    }

    [Fact]
    public void OrderItem_ValidateQuantity_WithValidQuantity_ShouldNotThrow()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Quantity = 10;

        // Act & Assert
        orderItem.ValidateQuantity(); // Should not throw
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void OrderItem_ValidateQuantity_WithInvalidQuantity_ShouldThrowException(int quantity)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Quantity = quantity;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => orderItem.ValidateQuantity());
    }

    [Fact]
    public void OrderItem_ValidateProductCode_WithValidProductCode_ShouldNotThrow()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.ProductCode = "PROD-001";

        // Act & Assert
        orderItem.ValidateProductCode(); // Should not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OrderItem_ValidateProductCode_WithInvalidProductCode_ShouldThrowException(string productCode)
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.ProductCode = productCode;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => orderItem.ValidateProductCode());
    }

    [Fact]
    public void OrderItem_ValidateSpecifications_WithValidSpecifications_ShouldNotThrow()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Specifications = "Valid specifications";

        // Act & Assert
        orderItem.ValidateSpecifications(); // Should not throw
    }

    [Fact]
    public void OrderItem_ValidateSpecifications_WithNullSpecifications_ShouldNotThrow()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Specifications = null;

        // Act & Assert
        orderItem.ValidateSpecifications(); // Should not throw
    }

    [Fact]
    public void OrderItem_ValidateSpecifications_WithEmptySpecifications_ShouldNotThrow()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Specifications = "";

        // Act & Assert
        orderItem.ValidateSpecifications(); // Should not throw
    }

    [Fact]
    public void OrderItem_ValidateSpecifications_WithTooLongSpecifications_ShouldThrowException()
    {
        // Arrange
        var orderItem = CreateValidOrderItem();
        orderItem.Specifications = new string('A', 1001); // Exceeds 1000 character limit

        // Act & Assert
        Assert.Throws<ArgumentException>(() => orderItem.ValidateSpecifications());
    }

    private static OrderItem CreateValidOrderItem()
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            ProductCode = "PROD-001",
            Description = "Test Product",
            Quantity = 10,
            Unit = "EA",
            Specifications = "Test specifications",
            UnitPrice = 25.50m,
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