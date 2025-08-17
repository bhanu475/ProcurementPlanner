using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class SupplierTests
{
    [Fact]
    public void Supplier_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var supplier = CreateValidSupplier();

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Supplier_WithInvalidName_ShouldFailValidation(string name)
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Name = name;

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    public void Supplier_WithInvalidContactEmail_ShouldFailValidation(string email)
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.ContactEmail = email;

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ContactEmail"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Supplier_WithInvalidContactPhone_ShouldFailValidation(string phone)
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.ContactPhone = phone;

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ContactPhone"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Supplier_WithInvalidAddress_ShouldFailValidation(string address)
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Address = address;

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Address"));
    }

    [Fact]
    public void Supplier_WithTooLongName_ShouldFailValidation()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Name = new string('A', 201); // Exceeds 200 character limit

        // Act
        var validationResults = ValidateModel(supplier);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Supplier_GetAvailableCapacity_WithMatchingProductType_ShouldReturnCorrectCapacity()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                MaxMonthlyCapacity = 100,
                CurrentCommitments = 30,
                IsActive = true
            }
        };

        // Act
        var availableCapacity = supplier.GetAvailableCapacity(ProductType.LMR);

        // Assert
        Assert.Equal(70, availableCapacity);
    }

    [Fact]
    public void Supplier_GetAvailableCapacity_WithNoMatchingProductType_ShouldReturnZero()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                MaxMonthlyCapacity = 100,
                CurrentCommitments = 30,
                IsActive = true
            }
        };

        // Act
        var availableCapacity = supplier.GetAvailableCapacity(ProductType.FFV);

        // Assert
        Assert.Equal(0, availableCapacity);
    }

    [Fact]
    public void Supplier_CanHandleProductType_WithActiveCapability_ShouldReturnTrue()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                IsActive = true
            }
        };

        // Act
        var canHandle = supplier.CanHandleProductType(ProductType.LMR);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void Supplier_CanHandleProductType_WithInactiveCapability_ShouldReturnFalse()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                IsActive = false
            }
        };

        // Act
        var canHandle = supplier.CanHandleProductType(ProductType.LMR);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void Supplier_HasCapacityFor_WithSufficientCapacity_ShouldReturnTrue()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                MaxMonthlyCapacity = 100,
                CurrentCommitments = 30,
                IsActive = true
            }
        };

        // Act
        var hasCapacity = supplier.HasCapacityFor(ProductType.LMR, 50);

        // Assert
        Assert.True(hasCapacity);
    }

    [Fact]
    public void Supplier_HasCapacityFor_WithInsufficientCapacity_ShouldReturnFalse()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                MaxMonthlyCapacity = 100,
                CurrentCommitments = 30,
                IsActive = true
            }
        };

        // Act
        var hasCapacity = supplier.HasCapacityFor(ProductType.LMR, 80);

        // Assert
        Assert.False(hasCapacity);
    }

    [Fact]
    public void Supplier_ValidateContactInformation_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var supplier = CreateValidSupplier();

        // Act & Assert
        supplier.ValidateContactInformation(); // Should not throw
    }

    [Fact]
    public void Supplier_ValidateContactInformation_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Name = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => supplier.ValidateContactInformation());
    }

    [Fact]
    public void Supplier_ValidateContactInformation_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.ContactEmail = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => supplier.ValidateContactInformation());
    }

    [Fact]
    public void Supplier_GetQualityRating_WithMatchingProductType_ShouldReturnCorrectRating()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                QualityRating = 4.5m
            }
        };

        // Act
        var qualityRating = supplier.GetQualityRating(ProductType.LMR);

        // Assert
        Assert.Equal(4.5m, qualityRating);
    }

    [Fact]
    public void Supplier_GetQualityRating_WithNoMatchingProductType_ShouldReturnZero()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Capabilities = new List<SupplierCapability>
        {
            new()
            {
                ProductType = ProductType.LMR,
                QualityRating = 4.5m
            }
        };

        // Act
        var qualityRating = supplier.GetQualityRating(ProductType.FFV);

        // Assert
        Assert.Equal(0, qualityRating);
    }

    [Fact]
    public void Supplier_IsActive_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        var supplier = new Supplier();

        // Assert
        Assert.True(supplier.IsActive);
    }

    private static Supplier CreateValidSupplier()
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            ContactEmail = "test@supplier.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test Street, Test City, TC 12345",
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