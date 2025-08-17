using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class DistributionAlgorithmServiceTests
{
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<ILogger<DistributionAlgorithmService>> _mockLogger;
    private readonly DistributionAlgorithmService _service;

    public DistributionAlgorithmServiceTests()
    {
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockLogger = new Mock<ILogger<DistributionAlgorithmService>>();
        _service = new DistributionAlgorithmService(_mockSupplierRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateDistributionSuggestionAsync_WithValidOrder_ReturnsDistributionSuggestion()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder(ProductType.LMR, 100);
        var suppliers = CreateTestSuppliers(ProductType.LMR);
        
        SetupSupplierRepositoryMocks(suppliers);

        // Act
        var result = await _service.GenerateDistributionSuggestionAsync(customerOrder);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customerOrder.Id, result.CustomerOrderId);
        Assert.Equal(100, result.TotalQuantity);
        Assert.Equal(ProductType.LMR, result.ProductType);
        Assert.True(result.Allocations.Any());
        Assert.Equal(DistributionStrategy.Balanced, result.Strategy);
    }

    [Fact]
    public async Task GenerateDistributionSuggestionAsync_WithNoEligibleSuppliers_ReturnsEmptyAllocation()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder(ProductType.FFV, 50);
        
        _mockSupplierRepository.Setup(x => x.GetActiveSuppliersByProductTypeAsync(ProductType.FFV))
            .ReturnsAsync(new List<Supplier>());

        // Act
        var result = await _service.GenerateDistributionSuggestionAsync(customerOrder);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Allocations);
        Assert.False(result.IsFullyAllocated);
        Assert.Contains("No eligible suppliers", result.Notes);
    }

    [Fact]
    public async Task GetEligibleSuppliersAsync_WithValidSuppliers_ReturnsFilteredList()
    {
        // Arrange
        var suppliers = CreateTestSuppliers(ProductType.LMR);
        SetupSupplierRepositoryMocks(suppliers);

        // Act
        var result = await _service.GetEligibleSuppliersAsync(ProductType.LMR, 50);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2); // Should have at least 2 eligible suppliers
        Assert.All(result, s => Assert.True(s.OverallPerformanceScore >= 0.7m));
        Assert.All(result, s => Assert.True(s.AvailableCapacity >= 1));
    }

    [Fact]
    public async Task GetEligibleSuppliersAsync_FiltersOutPoorPerformers()
    {
        // Arrange
        var suppliers = CreateTestSuppliersWithPoorPerformance(ProductType.LMR);
        SetupSupplierRepositoryMocks(suppliers);

        // Act
        var result = await _service.GetEligibleSuppliersAsync(ProductType.LMR, 50);

        // Assert
        Assert.Empty(result); // All suppliers should be filtered out due to poor performance
    }

    [Theory]
    [InlineData(DistributionStrategy.EvenDistribution)]
    [InlineData(DistributionStrategy.PerformanceBased)]
    [InlineData(DistributionStrategy.CapacityBased)]
    [InlineData(DistributionStrategy.Balanced)]
    public void CalculateOptimalDistribution_WithDifferentStrategies_ReturnsValidAllocations(DistributionStrategy strategy)
    {
        // Arrange
        var suppliers = CreateTestSupplierAllocationInfo();
        var totalQuantity = 100;

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, totalQuantity, strategy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Any());
        Assert.True(result.Sum(a => a.AllocatedQuantity) <= totalQuantity);
        Assert.All(result, a => Assert.True(a.AllocatedQuantity >= 1));
        Assert.All(result, a => Assert.True(a.AllocationPercentage >= 0));
    }

    [Fact]
    public void CalculateOptimalDistribution_EvenDistribution_DistributesEvenly()
    {
        // Arrange
        var suppliers = CreateTestSupplierAllocationInfo(3, 50); // 3 suppliers with 50 capacity each
        var totalQuantity = 90;

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, totalQuantity, DistributionStrategy.EvenDistribution);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, a => Assert.InRange(a.AllocatedQuantity, 29, 31)); // Should be around 30 each
        Assert.Equal(90, result.Sum(a => a.AllocatedQuantity));
    }

    [Fact]
    public void CalculateOptimalDistribution_PerformanceBased_PrioritizesHighPerformers()
    {
        // Arrange
        var suppliers = new List<SupplierAllocationInfo>
        {
            CreateSupplierAllocationInfo(Guid.NewGuid(), "High Performer", 100, 0.95m, 4.5m),
            CreateSupplierAllocationInfo(Guid.NewGuid(), "Medium Performer", 100, 0.85m, 3.5m),
            CreateSupplierAllocationInfo(Guid.NewGuid(), "Low Performer", 100, 0.75m, 2.5m)
        };
        var totalQuantity = 90;

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, totalQuantity, DistributionStrategy.PerformanceBased);

        // Assert
        var highPerformerAllocation = result.First(a => a.SupplierName == "High Performer");
        var lowPerformerAllocation = result.First(a => a.SupplierName == "Low Performer");
        
        Assert.True(highPerformerAllocation.AllocatedQuantity > lowPerformerAllocation.AllocatedQuantity);
    }

    [Fact]
    public void CalculateOptimalDistribution_CapacityBased_PrioritizesHighCapacity()
    {
        // Arrange
        var suppliers = new List<SupplierAllocationInfo>
        {
            CreateSupplierAllocationInfo(Guid.NewGuid(), "High Capacity", 200, 0.85m, 3.5m),
            CreateSupplierAllocationInfo(Guid.NewGuid(), "Medium Capacity", 100, 0.85m, 3.5m),
            CreateSupplierAllocationInfo(Guid.NewGuid(), "Low Capacity", 50, 0.85m, 3.5m)
        };
        var totalQuantity = 150;

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, totalQuantity, DistributionStrategy.CapacityBased);

        // Assert
        Assert.NotEmpty(result);
        var highCapacityAllocation = result.FirstOrDefault(a => a.SupplierName == "High Capacity");
        var lowCapacityAllocation = result.FirstOrDefault(a => a.SupplierName == "Low Capacity");
        
        Assert.NotNull(highCapacityAllocation);
        if (lowCapacityAllocation != null)
        {
            Assert.True(highCapacityAllocation.AllocatedQuantity >= lowCapacityAllocation.AllocatedQuantity);
        }
    }

    [Fact]
    public async Task ValidateDistributionAsync_WithValidPlan_ReturnsValid()
    {
        // Arrange
        var suppliers = CreateTestSuppliers(ProductType.LMR);
        var distributionPlan = CreateTestDistributionPlan(suppliers);
        
        SetupSupplierRepositoryMocks(suppliers);

        // Act
        var result = await _service.ValidateDistributionAsync(distributionPlan);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateDistributionAsync_WithInsufficientCapacity_ReturnsInvalid()
    {
        // Arrange
        var suppliers = CreateTestSuppliers(ProductType.LMR);
        var distributionPlan = CreateTestDistributionPlan(suppliers, overAllocate: true);
        
        SetupSupplierRepositoryMocks(suppliers);

        // Act
        var result = await _service.ValidateDistributionAsync(distributionPlan);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        // Check if any error contains capacity-related message
        Assert.True(result.Errors.Any(e => e.Contains("Insufficient capacity") || e.Contains("capacity")));
    }

    [Fact]
    public async Task ValidateDistributionAsync_WithEmptyAllocations_ReturnsInvalid()
    {
        // Arrange
        var distributionPlan = new DistributionPlan
        {
            CustomerOrderId = Guid.NewGuid(),
            Allocations = new List<SupplierAllocation>()
        };

        // Act
        var result = await _service.ValidateDistributionAsync(distributionPlan);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must contain at least one allocation", result.Errors.First());
    }

    [Fact]
    public void CalculateOptimalDistribution_WithZeroQuantity_ReturnsEmptyList()
    {
        // Arrange
        var suppliers = CreateTestSupplierAllocationInfo();

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, 0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateOptimalDistribution_WithEmptySuppliers_ReturnsEmptyList()
    {
        // Arrange
        var suppliers = new List<SupplierAllocationInfo>();

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, 100);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateOptimalDistribution_WithInsufficientTotalCapacity_AllocatesMaximumPossible()
    {
        // Arrange
        var suppliers = CreateTestSupplierAllocationInfo(2, 30); // 2 suppliers with 30 capacity each = 60 total
        var totalQuantity = 100; // Requesting more than available

        // Act
        var result = _service.CalculateOptimalDistribution(suppliers, totalQuantity);

        // Assert
        Assert.True(result.Sum(a => a.AllocatedQuantity) <= 60); // Should not exceed total available capacity
        Assert.All(result, a => Assert.True(a.AllocatedQuantity <= a.AvailableCapacity));
    }

    // Helper methods
    private CustomerOrder CreateTestCustomerOrder(ProductType productType, int totalQuantity)
    {
        return new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "TEST-001",
            CustomerId = "DODAAC123",
            CustomerName = "Test Customer",
            ProductType = productType,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD-001",
                    Description = "Test Product",
                    Quantity = totalQuantity,
                    Unit = "EA"
                }
            }
        };
    }

    private List<Supplier> CreateTestSuppliers(ProductType productType)
    {
        return new List<Supplier>
        {
            new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Supplier A",
                ContactEmail = "suppliera@test.com",
                ContactPhone = "123-456-7890",
                Address = "123 Test St",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new SupplierCapability
                    {
                        Id = Guid.NewGuid(),
                        ProductType = productType,
                        MaxMonthlyCapacity = 100,
                        CurrentCommitments = 20,
                        QualityRating = 4.0m,
                        IsActive = true
                    }
                },
                Performance = new SupplierPerformanceMetrics
                {
                    Id = Guid.NewGuid(),
                    OnTimeDeliveryRate = 0.95m,
                    QualityScore = 4.2m,
                    TotalOrdersCompleted = 50,
                    TotalOrdersOnTime = 47,
                    TotalOrdersLate = 3,
                    LastUpdated = DateTime.UtcNow
                }
            },
            new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Supplier B",
                ContactEmail = "supplierb@test.com",
                ContactPhone = "123-456-7891",
                Address = "456 Test Ave",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new SupplierCapability
                    {
                        Id = Guid.NewGuid(),
                        ProductType = productType,
                        MaxMonthlyCapacity = 80,
                        CurrentCommitments = 10,
                        QualityRating = 3.8m,
                        IsActive = true
                    }
                },
                Performance = new SupplierPerformanceMetrics
                {
                    Id = Guid.NewGuid(),
                    OnTimeDeliveryRate = 0.88m,
                    QualityScore = 3.9m,
                    TotalOrdersCompleted = 30,
                    TotalOrdersOnTime = 26,
                    TotalOrdersLate = 4,
                    LastUpdated = DateTime.UtcNow
                }
            },
            new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Supplier C",
                ContactEmail = "supplierc@test.com",
                ContactPhone = "123-456-7892",
                Address = "789 Test Blvd",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new SupplierCapability
                    {
                        Id = Guid.NewGuid(),
                        ProductType = productType,
                        MaxMonthlyCapacity = 120,
                        CurrentCommitments = 30,
                        QualityRating = 4.5m,
                        IsActive = true
                    }
                },
                Performance = new SupplierPerformanceMetrics
                {
                    Id = Guid.NewGuid(),
                    OnTimeDeliveryRate = 0.92m,
                    QualityScore = 4.3m,
                    TotalOrdersCompleted = 40,
                    TotalOrdersOnTime = 37,
                    TotalOrdersLate = 3,
                    LastUpdated = DateTime.UtcNow
                }
            }
        };
    }

    private List<Supplier> CreateTestSuppliersWithPoorPerformance(ProductType productType)
    {
        return new List<Supplier>
        {
            new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Poor Supplier",
                ContactEmail = "poor@test.com",
                ContactPhone = "123-456-7890",
                Address = "123 Test St",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new SupplierCapability
                    {
                        Id = Guid.NewGuid(),
                        ProductType = productType,
                        MaxMonthlyCapacity = 100,
                        CurrentCommitments = 20,
                        QualityRating = 2.0m,
                        IsActive = true
                    }
                },
                Performance = new SupplierPerformanceMetrics
                {
                    Id = Guid.NewGuid(),
                    OnTimeDeliveryRate = 0.60m, // Below threshold
                    QualityScore = 2.0m, // Below threshold
                    TotalOrdersCompleted = 20,
                    TotalOrdersOnTime = 12,
                    TotalOrdersLate = 8,
                    LastUpdated = DateTime.UtcNow
                }
            }
        };
    }

    private List<SupplierAllocationInfo> CreateTestSupplierAllocationInfo(int count = 3, int capacity = 100)
    {
        var suppliers = new List<SupplierAllocationInfo>();
        
        for (int i = 0; i < count; i++)
        {
            suppliers.Add(CreateSupplierAllocationInfo(
                Guid.NewGuid(), 
                $"Supplier {i + 1}", 
                capacity, 
                0.85m + (i * 0.05m), // Varying performance
                3.5m + (i * 0.3m))); // Varying quality
        }
        
        return suppliers;
    }

    private SupplierAllocationInfo CreateSupplierAllocationInfo(Guid id, string name, int capacity, decimal onTimeRate, decimal qualityScore)
    {
        var performanceScore = (onTimeRate * 0.4m) + ((qualityScore / 5) * 0.4m) + 0.2m; // Simplified calculation
        
        return new SupplierAllocationInfo
        {
            SupplierId = id,
            SupplierName = name,
            AvailableCapacity = capacity,
            MaxMonthlyCapacity = capacity + 20,
            CurrentCommitments = 20,
            QualityRating = qualityScore,
            OnTimeDeliveryRate = onTimeRate,
            QualityScore = qualityScore,
            OverallPerformanceScore = performanceScore,
            IsPreferredSupplier = performanceScore >= 0.95m,
            IsReliableSupplier = performanceScore >= 0.85m,
            ProductType = ProductType.LMR,
            LastUpdated = DateTime.UtcNow
        };
    }

    private DistributionPlan CreateTestDistributionPlan(List<Supplier> suppliers, bool overAllocate = false)
    {
        var allocations = new List<SupplierAllocation>();
        
        foreach (var supplier in suppliers.Take(2))
        {
            var capability = supplier.Capabilities.First();
            var quantity = overAllocate ? capability.AvailableCapacity + 50 : Math.Min(30, capability.AvailableCapacity);
            
            allocations.Add(new SupplierAllocation
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                AllocatedQuantity = quantity,
                AllocationPercentage = 50m,
                AvailableCapacity = capability.AvailableCapacity,
                PerformanceScore = supplier.Performance?.OverallPerformanceScore ?? 0.8m,
                QualityRating = capability.QualityRating,
                OnTimeDeliveryRate = supplier.Performance?.OnTimeDeliveryRate ?? 0.85m
            });
        }

        return new DistributionPlan
        {
            CustomerOrderId = Guid.NewGuid(),
            Allocations = allocations,
            Strategy = DistributionStrategy.Balanced,
            CreatedBy = Guid.NewGuid()
        };
    }

    private void SetupSupplierRepositoryMocks(List<Supplier> suppliers)
    {
        _mockSupplierRepository.Setup(x => x.GetActiveSuppliersByProductTypeAsync(It.IsAny<ProductType>()))
            .ReturnsAsync(suppliers);

        foreach (var supplier in suppliers)
        {
            _mockSupplierRepository.Setup(x => x.GetSupplierWithCapabilitiesAsync(supplier.Id))
                .ReturnsAsync(supplier);
            
            _mockSupplierRepository.Setup(x => x.GetSupplierWithPerformanceAsync(supplier.Id))
                .ReturnsAsync(supplier);
        }
    }
}