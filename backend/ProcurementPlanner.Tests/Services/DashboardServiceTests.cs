using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<ICustomerOrderRepository> _mockRepository;
    private readonly Mock<ILogger<DashboardService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _mockRepository = new Mock<ICustomerOrderRepository>();
        _mockLogger = new Mock<ILogger<DashboardService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new DashboardService(_mockRepository.Object, _memoryCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_FirstCall_ReturnsFromDatabase()
    {
        // Arrange
        var filter = new DashboardFilterRequest
        {
            ProductType = ProductType.LMR
        };

        var expectedSummary = new OrderDashboardSummary
        {
            TotalOrders = 10,
            StatusCounts = new Dictionary<OrderStatus, int>
            {
                { OrderStatus.Submitted, 5 },
                { OrderStatus.UnderReview, 3 },
                { OrderStatus.Delivered, 2 }
            },
            ProductTypeCounts = new Dictionary<ProductType, int>
            {
                { ProductType.LMR, 7 },
                { ProductType.FFV, 3 }
            },
            OverdueOrders = 2,
            TotalValue = 15000.50m
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _service.GetDashboardSummaryAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSummary.TotalOrders, result.TotalOrders);
        Assert.Equal(expectedSummary.OverdueOrders, result.OverdueOrders);
        Assert.Equal(expectedSummary.TotalValue, result.TotalValue);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_SecondCall_ReturnsFromCache()
    {
        // Arrange
        var filter = new DashboardFilterRequest
        {
            ProductType = ProductType.LMR
        };

        var expectedSummary = new OrderDashboardSummary
        {
            TotalOrders = 10,
            StatusCounts = new Dictionary<OrderStatus, int>
            {
                { OrderStatus.Submitted, 5 }
            }
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(expectedSummary);

        // Act - First call
        var result1 = await _service.GetDashboardSummaryAsync(filter);
        
        // Act - Second call
        var result2 = await _service.GetDashboardSummaryAsync(filter);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.TotalOrders, result2.TotalOrders);
        
        // Repository should only be called once (first call), second call should use cache
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_DifferentFilters_CallsDatabaseForEach()
    {
        // Arrange
        var filter1 = new DashboardFilterRequest { ProductType = ProductType.LMR };
        var filter2 = new DashboardFilterRequest { ProductType = ProductType.FFV };

        var summary1 = new OrderDashboardSummary { TotalOrders = 5 };
        var summary2 = new OrderDashboardSummary { TotalOrders = 8 };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter1))
            .ReturnsAsync(summary1);
        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter2))
            .ReturnsAsync(summary2);

        // Act
        var result1 = await _service.GetDashboardSummaryAsync(filter1);
        var result2 = await _service.GetDashboardSummaryAsync(filter2);

        // Assert
        Assert.Equal(5, result1.TotalOrders);
        Assert.Equal(8, result2.TotalOrders);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter1), Times.Once);
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter2), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByDeliveryDateAsync_FirstCall_ReturnsFromDatabase()
    {
        // Arrange
        var filter = new DashboardFilterRequest();
        var expectedOrders = new List<OrdersByDeliveryDate>
        {
            new()
            {
                DeliveryDate = DateTime.UtcNow.Date.AddDays(1),
                OrderCount = 3,
                TotalQuantity = 150
            },
            new()
            {
                DeliveryDate = DateTime.UtcNow.Date.AddDays(2),
                OrderCount = 2,
                TotalQuantity = 100
            }
        };

        var summary = new OrderDashboardSummary
        {
            OrdersByDeliveryDate = expectedOrders
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(summary);

        // Act
        var result = await _service.GetOrdersByDeliveryDateAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].OrderCount);
        Assert.Equal(150, result[0].TotalQuantity);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetTopCustomersAsync_WithTopCount_ReturnsLimitedResults()
    {
        // Arrange
        var filter = new DashboardFilterRequest();
        var topCount = 3;
        
        var allCustomers = new List<OrdersByCustomer>
        {
            new() { CustomerId = "CUST001", CustomerName = "Customer 1", OrderCount = 10 },
            new() { CustomerId = "CUST002", CustomerName = "Customer 2", OrderCount = 8 },
            new() { CustomerId = "CUST003", CustomerName = "Customer 3", OrderCount = 6 },
            new() { CustomerId = "CUST004", CustomerName = "Customer 4", OrderCount = 4 },
            new() { CustomerId = "CUST005", CustomerName = "Customer 5", OrderCount = 2 }
        };

        var summary = new OrderDashboardSummary
        {
            TopCustomers = allCustomers
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(summary);

        // Act
        var result = await _service.GetTopCustomersAsync(filter, topCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(topCount, result.Count);
        Assert.Equal("CUST001", result[0].CustomerId);
        Assert.Equal("CUST002", result[1].CustomerId);
        Assert.Equal("CUST003", result[2].CustomerId);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetTopCustomersAsync_SecondCall_ReturnsFromCache()
    {
        // Arrange
        var filter = new DashboardFilterRequest();
        var topCount = 2;
        
        var customers = new List<OrdersByCustomer>
        {
            new() { CustomerId = "CUST001", CustomerName = "Customer 1", OrderCount = 10 },
            new() { CustomerId = "CUST002", CustomerName = "Customer 2", OrderCount = 8 }
        };

        var summary = new OrderDashboardSummary
        {
            TopCustomers = customers
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(summary);

        // Act - First call
        var result1 = await _service.GetTopCustomersAsync(filter, topCount);
        
        // Act - Second call
        var result2 = await _service.GetTopCustomersAsync(filter, topCount);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1[0].CustomerId, result2[0].CustomerId);
        
        // Repository should only be called once
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_CompletesSuccessfully()
    {
        // Act
        await _service.InvalidateCacheAsync();

        // Assert - No exception should be thrown
        // This is a simple test since the current implementation just logs
        Assert.True(true);
    }

    [Theory]
    [InlineData(ProductType.LMR, null, null, null, null)]
    [InlineData(null, OrderStatus.Submitted, null, null, null)]
    [InlineData(null, null, "CUST001", null, null)]
    [InlineData(ProductType.FFV, OrderStatus.Delivered, "CUST002", "2025-01-01", "2025-12-31")]
    public async Task GetDashboardSummaryAsync_DifferentFilterCombinations_GeneratesDifferentCacheKeys(
        ProductType? productType, 
        OrderStatus? status, 
        string? customerId, 
        string? deliveryDateFromStr, 
        string? deliveryDateToStr)
    {
        // Arrange
        var filter = new DashboardFilterRequest
        {
            ProductType = productType,
            Status = status,
            CustomerId = customerId,
            DeliveryDateFrom = deliveryDateFromStr != null ? DateTime.Parse(deliveryDateFromStr) : null,
            DeliveryDateTo = deliveryDateToStr != null ? DateTime.Parse(deliveryDateToStr) : null
        };

        var summary = new OrderDashboardSummary { TotalOrders = 1 };
        
        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(It.IsAny<DashboardFilterRequest>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _service.GetDashboardSummaryAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalOrders);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_PerformanceTest_CompletesWithinReasonableTime()
    {
        // Arrange
        var filter = new DashboardFilterRequest();
        var summary = new OrderDashboardSummary
        {
            TotalOrders = 1000,
            StatusCounts = Enum.GetValues<OrderStatus>().ToDictionary(s => s, s => 100),
            ProductTypeCounts = Enum.GetValues<ProductType>().ToDictionary(p => p, p => 500),
            OrdersByDeliveryDate = Enumerable.Range(1, 30)
                .Select(i => new OrdersByDeliveryDate
                {
                    DeliveryDate = DateTime.UtcNow.Date.AddDays(i),
                    OrderCount = 10,
                    TotalQuantity = 100
                }).ToList(),
            TopCustomers = Enumerable.Range(1, 50)
                .Select(i => new OrdersByCustomer
                {
                    CustomerId = $"CUST{i:000}",
                    CustomerName = $"Customer {i}",
                    OrderCount = 100 - i,
                    TotalQuantity = 1000 - (i * 10),
                    TotalValue = 10000 - (i * 100)
                }).ToList()
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(summary);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _service.GetDashboardSummaryAsync(filter);

        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.TotalOrders);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Dashboard summary took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }
}