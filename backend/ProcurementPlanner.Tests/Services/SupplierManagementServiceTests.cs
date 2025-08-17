using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Services;

namespace ProcurementPlanner.Tests.Services;

public class SupplierManagementServiceTests
{
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<ILogger<SupplierManagementService>> _mockLogger;
    private readonly SupplierManagementService _service;

    public SupplierManagementServiceTests()
    {
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockLogger = new Mock<ILogger<SupplierManagementService>>();
        _service = new SupplierManagementService(_mockSupplierRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAvailableSuppliersAsync_ReturnsOrderedSuppliersByPerformance()
    {
        // Arrange
        var productType = ProductType.LMR;
        var requiredCapacity = 100;
        
        var supplier1 = CreateTestSupplier("Supplier 1", productType, 150, 50);
        supplier1.Performance = CreateTestPerformance(supplier1.Id, 0.95m, 4.5m);
        
        var supplier2 = CreateTestSupplier("Supplier 2", productType, 200, 100);
        supplier2.Performance = CreateTestPerformance(supplier2.Id, 0.85m, 4.0m);
        
        var suppliers = new List<Supplier> { supplier2, supplier1 }; // Intentionally out of order

        _mockSupplierRepository
            .Setup(r => r.GetSuppliersByCapacityAsync(productType, requiredCapacity))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _service.GetAvailableSuppliersAsync(productType, requiredCapacity);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Supplier 1", result[0].Name); // Should be first due to higher performance
        Assert.Equal("Supplier 2", result[1].Name);
    }

    [Fact]
    public async Task GetAvailableSuppliersAsync_FiltersInactiveSuppliers()
    {
        // Arrange
        var productType = ProductType.LMR;
        var requiredCapacity = 100;
        
        var activeSupplier = CreateTestSupplier("Active Supplier", productType, 150, 50);
        var inactiveSupplier = CreateTestSupplier("Inactive Supplier", productType, 200, 100);
        inactiveSupplier.IsActive = false;
        
        var suppliers = new List<Supplier> { activeSupplier, inactiveSupplier };

        _mockSupplierRepository
            .Setup(r => r.GetSuppliersByCapacityAsync(productType, requiredCapacity))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _service.GetAvailableSuppliersAsync(productType, requiredCapacity);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Supplier", result[0].Name);
    }

    [Fact]
    public async Task CreateSupplierAsync_ValidatesSupplierData()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            ContactEmail = "test@supplier.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            Capabilities = new List<SupplierCapability>
            {
                new SupplierCapability
                {
                    ProductType = ProductType.LMR,
                    MaxMonthlyCapacity = 1000,
                    CurrentCommitments = 500,
                    QualityRating = 4.0m
                }
            }
        };

        _mockSupplierRepository
            .Setup(r => r.AddAsync(It.IsAny<Supplier>()))
            .ReturnsAsync(supplier);

        // Act
        var result = await _service.CreateSupplierAsync(supplier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Supplier", result.Name);
        _mockSupplierRepository.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Once);
    }

    [Fact]
    public async Task CreateSupplierAsync_ThrowsExceptionForInvalidData()
    {
        // Arrange
        var supplier = new Supplier
        {
            Name = "", // Invalid - empty name
            ContactEmail = "test@supplier.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateSupplierAsync(supplier));
    }

    [Fact]
    public async Task UpdateSupplierCapacityAsync_UpdatesExistingCapability()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.LMR;
        var supplier = CreateTestSupplier("Test Supplier", productType, 1000, 500);
        supplier.Id = supplierId;

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithCapabilitiesAsync(supplierId))
            .ReturnsAsync(supplier);

        _mockSupplierRepository
            .Setup(r => r.UpdateCapabilityAsync(It.IsAny<SupplierCapability>()))
            .ReturnsAsync((SupplierCapability c) => c);

        // Act
        var result = await _service.UpdateSupplierCapacityAsync(supplierId, productType, 1500, 600);

        // Assert
        var capability = result.Capabilities.First(c => c.ProductType == productType);
        Assert.Equal(1500, capability.MaxMonthlyCapacity);
        Assert.Equal(600, capability.CurrentCommitments);
        Assert.Equal(900, capability.AvailableCapacity);
    }

    [Fact]
    public async Task UpdateSupplierCapacityAsync_CreatesNewCapabilityIfNotExists()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.FFV;
        var supplier = CreateTestSupplier("Test Supplier", ProductType.LMR, 1000, 500);
        supplier.Id = supplierId;

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithCapabilitiesAsync(supplierId))
            .ReturnsAsync(supplier);

        _mockSupplierRepository
            .Setup(r => r.UpdateCapabilityAsync(It.IsAny<SupplierCapability>()))
            .ReturnsAsync((SupplierCapability c) => c);

        // Act
        var result = await _service.UpdateSupplierCapacityAsync(supplierId, productType, 800, 200);

        // Assert
        Assert.Equal(2, result.Capabilities.Count);
        var newCapability = result.Capabilities.First(c => c.ProductType == productType);
        Assert.Equal(800, newCapability.MaxMonthlyCapacity);
        Assert.Equal(200, newCapability.CurrentCommitments);
        Assert.Equal(600, newCapability.AvailableCapacity);
    }

    [Fact]
    public async Task UpdateSupplierPerformanceAsync_CreatesNewMetricsIfNotExists()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var wasOnTime = true;
        var qualityScore = 4.5m;
        var deliveryDays = 5;

        _mockSupplierRepository
            .Setup(r => r.GetPerformanceMetricsAsync(supplierId))
            .ReturnsAsync((SupplierPerformanceMetrics?)null);

        _mockSupplierRepository
            .Setup(r => r.UpdatePerformanceMetricsAsync(It.IsAny<SupplierPerformanceMetrics>()))
            .ReturnsAsync((SupplierPerformanceMetrics m) => m);

        // Act
        await _service.UpdateSupplierPerformanceAsync(supplierId, wasOnTime, qualityScore, deliveryDays);

        // Assert
        _mockSupplierRepository.Verify(r => r.UpdatePerformanceMetricsAsync(
            It.Is<SupplierPerformanceMetrics>(m => 
                m.SupplierId == supplierId &&
                m.QualityScore == qualityScore &&
                m.AverageDeliveryDays == deliveryDays)), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierPerformanceAsync_UpdatesExistingMetrics()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var existingMetrics = CreateTestPerformance(supplierId, 0.8m, 3.5m);
        existingMetrics.TotalOrdersCompleted = 10;
        existingMetrics.TotalOrdersOnTime = 8;
        existingMetrics.TotalOrdersLate = 2;

        _mockSupplierRepository
            .Setup(r => r.GetPerformanceMetricsAsync(supplierId))
            .ReturnsAsync(existingMetrics);

        _mockSupplierRepository
            .Setup(r => r.UpdatePerformanceMetricsAsync(It.IsAny<SupplierPerformanceMetrics>()))
            .ReturnsAsync((SupplierPerformanceMetrics m) => m);

        // Act
        await _service.UpdateSupplierPerformanceAsync(supplierId, true, 4.0m, 3);

        // Assert
        _mockSupplierRepository.Verify(r => r.UpdatePerformanceMetricsAsync(
            It.Is<SupplierPerformanceMetrics>(m => 
                m.TotalOrdersCompleted == 11 &&
                m.TotalOrdersOnTime == 9)), Times.Once);
    }

    [Fact]
    public async Task ValidateSupplierEligibilityAsync_ReturnsTrueForEligibleSupplier()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.LMR;
        var requiredQuantity = 100;
        
        var supplier = CreateTestSupplier("Test Supplier", productType, 500, 200);
        supplier.Id = supplierId;
        supplier.Performance = CreateTestPerformance(supplierId, 0.9m, 4.0m);

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithPerformanceAsync(supplierId))
            .ReturnsAsync(supplier);

        // Act
        var result = await _service.ValidateSupplierEligibilityAsync(supplierId, productType, requiredQuantity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSupplierEligibilityAsync_ReturnsFalseForInactiveSupplier()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.LMR;
        var requiredQuantity = 100;
        
        var supplier = CreateTestSupplier("Test Supplier", productType, 500, 200);
        supplier.Id = supplierId;
        supplier.IsActive = false;

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithPerformanceAsync(supplierId))
            .ReturnsAsync(supplier);

        // Act
        var result = await _service.ValidateSupplierEligibilityAsync(supplierId, productType, requiredQuantity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSupplierEligibilityAsync_ReturnsFalseForInsufficientCapacity()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.LMR;
        var requiredQuantity = 500; // More than available capacity
        
        var supplier = CreateTestSupplier("Test Supplier", productType, 500, 400); // Only 100 available
        supplier.Id = supplierId;

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithPerformanceAsync(supplierId))
            .ReturnsAsync(supplier);

        // Act
        var result = await _service.ValidateSupplierEligibilityAsync(supplierId, productType, requiredQuantity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSupplierEligibilityAsync_ReturnsFalseForPoorPerformance()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productType = ProductType.LMR;
        var requiredQuantity = 100;
        
        var supplier = CreateTestSupplier("Test Supplier", productType, 500, 200);
        supplier.Id = supplierId;
        supplier.Performance = CreateTestPerformance(supplierId, 0.5m, 2.0m); // Poor performance

        _mockSupplierRepository
            .Setup(r => r.GetSupplierWithPerformanceAsync(supplierId))
            .ReturnsAsync(supplier);

        // Act
        var result = await _service.ValidateSupplierEligibilityAsync(supplierId, productType, requiredQuantity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetSuppliersByPerformanceAsync_ReturnsOrderedSuppliers()
    {
        // Arrange
        var productType = ProductType.LMR;
        var minOnTimeRate = 0.8m;
        var minQualityScore = 3.0m;

        var supplier1 = CreateTestSupplier("Supplier 1", productType, 500, 200);
        supplier1.Performance = CreateTestPerformance(supplier1.Id, 0.95m, 4.5m);
        
        var supplier2 = CreateTestSupplier("Supplier 2", productType, 600, 300);
        supplier2.Performance = CreateTestPerformance(supplier2.Id, 0.85m, 4.0m);

        var suppliers = new List<Supplier> { supplier2, supplier1 };

        _mockSupplierRepository
            .Setup(r => r.GetSuppliersByPerformanceThresholdAsync(productType, minOnTimeRate, minQualityScore))
            .ReturnsAsync(suppliers);

        // Act
        var result = await _service.GetSuppliersByPerformanceAsync(productType, minOnTimeRate, minQualityScore);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Supplier 1", result[0].Name); // Higher performance score
        Assert.Equal("Supplier 2", result[1].Name);
    }

    [Fact]
    public async Task DeactivateSupplierAsync_SetsIsActiveToFalse()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = CreateTestSupplier("Test Supplier", ProductType.LMR, 500, 200);
        supplier.Id = supplierId;
        supplier.IsActive = true;

        _mockSupplierRepository
            .Setup(r => r.GetByIdAsync(supplierId))
            .ReturnsAsync(supplier);

        _mockSupplierRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Supplier>()))
            .ReturnsAsync((Supplier s) => s);

        // Act
        await _service.DeactivateSupplierAsync(supplierId);

        // Assert
        Assert.False(supplier.IsActive);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(It.Is<Supplier>(s => !s.IsActive)), Times.Once);
    }

    [Fact]
    public async Task ActivateSupplierAsync_SetsIsActiveToTrue()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = CreateTestSupplier("Test Supplier", ProductType.LMR, 500, 200);
        supplier.Id = supplierId;
        supplier.IsActive = false;

        _mockSupplierRepository
            .Setup(r => r.GetByIdAsync(supplierId))
            .ReturnsAsync(supplier);

        _mockSupplierRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Supplier>()))
            .ReturnsAsync((Supplier s) => s);

        // Act
        await _service.ActivateSupplierAsync(supplierId);

        // Assert
        Assert.True(supplier.IsActive);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(It.Is<Supplier>(s => s.IsActive)), Times.Once);
    }

    [Fact]
    public async Task GetTotalAvailableCapacityAsync_ReturnsCorrectTotal()
    {
        // Arrange
        var productType = ProductType.LMR;
        var expectedCapacity = 1500;

        _mockSupplierRepository
            .Setup(r => r.GetTotalCapacityByProductTypeAsync(productType))
            .ReturnsAsync(expectedCapacity);

        // Act
        var result = await _service.GetTotalAvailableCapacityAsync(productType);

        // Assert
        Assert.Equal(expectedCapacity, result);
    }

    private static Supplier CreateTestSupplier(string name, ProductType productType, int maxCapacity, int currentCommitments)
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactEmail = $"{name.Replace(" ", "").ToLower()}@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            IsActive = true,
            Capabilities = new List<SupplierCapability>
            {
                new SupplierCapability
                {
                    Id = Guid.NewGuid(),
                    ProductType = productType,
                    MaxMonthlyCapacity = maxCapacity,
                    CurrentCommitments = currentCommitments,
                    QualityRating = 4.0m,
                    IsActive = true
                }
            }
        };
    }

    private static SupplierPerformanceMetrics CreateTestPerformance(Guid supplierId, decimal onTimeRate, decimal qualityScore)
    {
        return new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            OnTimeDeliveryRate = onTimeRate,
            QualityScore = qualityScore,
            TotalOrdersCompleted = 10,
            TotalOrdersOnTime = (int)(10 * onTimeRate),
            TotalOrdersLate = 10 - (int)(10 * onTimeRate),
            TotalOrdersCancelled = 0,
            LastUpdated = DateTime.UtcNow,
            AverageDeliveryDays = 5
        };
    }
}