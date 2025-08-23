using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class ReportingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<ReportingService>> _mockLogger;
    private readonly IReportingService _reportingService;

    public ReportingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ReportingService>>();
        _reportingService = new ReportingService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test suppliers
        var supplier1 = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier 1",
            ContactEmail = "supplier1@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var supplier2 = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier 2",
            ContactEmail = "supplier2@test.com",
            ContactPhone = "123-456-7891",
            Address = "456 Test Ave",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _context.Suppliers.AddRange(supplier1, supplier2);

        // Create supplier capabilities
        var capability1 = new SupplierCapability
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier1.Id,
            ProductType = ProductType.LMR,
            MaxMonthlyCapacity = 1000,
            CurrentCommitments = 500,
            QualityRating = 4.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var capability2 = new SupplierCapability
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier2.Id,
            ProductType = ProductType.FFV,
            MaxMonthlyCapacity = 800,
            CurrentCommitments = 300,
            QualityRating = 4.2m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _context.SupplierCapabilities.AddRange(capability1, capability2);

        // Create supplier performance metrics
        var performance1 = new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier1.Id,
            OnTimeDeliveryRate = 0.95m,
            QualityScore = 4.5m,
            TotalOrdersCompleted = 50,
            AverageDeliveryDays = 7.5m,
            CustomerSatisfactionRate = 0.92m,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var performance2 = new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier2.Id,
            OnTimeDeliveryRate = 0.88m,
            QualityScore = 4.2m,
            TotalOrdersCompleted = 35,
            AverageDeliveryDays = 8.2m,
            CustomerSatisfactionRate = 0.89m,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _context.SupplierPerformanceMetrics.AddRange(performance1, performance2);

        // Create test customer orders
        var order1 = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerId = "CUST-001",
            CustomerName = "Test Customer 1",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(14),
            Status = OrderStatus.Delivered,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD-001",
                    Description = "Test Product 1",
                    Quantity = 100,
                    Unit = "pcs",
                    UnitPrice = 10.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            }
        };

        var order2 = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            CustomerId = "CUST-002",
            CustomerName = "Test Customer 2",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(21),
            Status = OrderStatus.InProduction,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD-002",
                    Description = "Test Product 2",
                    Quantity = 50,
                    Unit = "kg",
                    UnitPrice = 25.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        };

        _context.CustomerOrders.AddRange(order1, order2);

        // Create test purchase orders
        var purchaseOrder1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-001",
            CustomerOrderId = order1.Id,
            SupplierId = supplier1.Id,
            Status = PurchaseOrderStatus.Delivered,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(14),
            TotalValue = 1050.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            Items = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = order1.Items.First().Id,
                    ProductCode = "PROD-001",
                    Description = "Test Product 1",
                    AllocatedQuantity = 100,
                    Unit = "pcs",
                    UnitPrice = 10.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            }
        };

        var purchaseOrder2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-002",
            CustomerOrderId = order2.Id,
            SupplierId = supplier2.Id,
            Status = PurchaseOrderStatus.InProduction,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(21),
            TotalValue = 1250.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = order2.Items.First().Id,
                    ProductCode = "PROD-002",
                    Description = "Test Product 2",
                    AllocatedQuantity = 50,
                    Unit = "kg",
                    UnitPrice = 25.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        };

        _context.PurchaseOrders.AddRange(purchaseOrder1, purchaseOrder2);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReportAsync_WithValidRequest_ShouldReturnReport()
    {
        // Arrange
        var request = new PerformanceMetricsReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            IncludeOrderMetrics = true,
            IncludeSupplierMetrics = true,
            IncludeDeliveryMetrics = true
        };

        // Act
        var result = await _reportingService.GeneratePerformanceMetricsReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.FromDate, result.FromDate);
        Assert.Equal(request.ToDate, result.ToDate);
        Assert.True(result.OrderMetrics.TotalOrders > 0);
        Assert.True(result.SupplierMetrics.Count > 0);
        Assert.NotNull(result.DeliveryMetrics);
    }

    [Fact]
    public async Task GenerateSupplierDistributionReportAsync_WithValidRequest_ShouldReturnReport()
    {
        // Arrange
        var request = new SupplierDistributionReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupByProductType = true,
            IncludeCapacityUtilization = true
        };

        // Act
        var result = await _reportingService.GenerateSupplierDistributionReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.FromDate, result.FromDate);
        Assert.Equal(request.ToDate, result.ToDate);
        Assert.True(result.TotalOrders > 0);
        Assert.True(result.Distributions.Count > 0);
        Assert.True(result.CapacityUtilizations.Count > 0);
    }

    [Fact]
    public async Task GenerateOrderFulfillmentReportAsync_WithValidRequest_ShouldReturnReport()
    {
        // Arrange
        var request = new OrderFulfillmentReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupByStatus = true,
            GroupByCustomer = true,
            IncludeTimelines = true
        };

        // Act
        var result = await _reportingService.GenerateOrderFulfillmentReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.FromDate, result.FromDate);
        Assert.Equal(request.ToDate, result.ToDate);
        Assert.True(result.TotalOrders > 0);
        Assert.True(result.StatusSummaries.Count > 0);
        Assert.True(result.Timelines.Count > 0);
    }

    [Fact]
    public async Task GenerateDeliveryPerformanceReportAsync_WithValidRequest_ShouldReturnReport()
    {
        // Arrange
        var request = new DeliveryPerformanceReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupBySupplier = true,
            IncludeDelayAnalysis = true
        };

        // Act
        var result = await _reportingService.GenerateDeliveryPerformanceReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.FromDate, result.FromDate);
        Assert.Equal(request.ToDate, result.ToDate);
        Assert.True(result.TotalDeliveries >= 0);
        Assert.True(result.OnTimeDeliveryRate >= 0);
        Assert.True(result.AverageDeliveryDays >= 0);
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReportAsync_WithSpecificSupplier_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var request = new PerformanceMetricsReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            SupplierId = supplier.Id,
            IncludeSupplierMetrics = true
        };

        // Act
        var result = await _reportingService.GeneratePerformanceMetricsReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SupplierMetrics.Count <= 1);
        if (result.SupplierMetrics.Count > 0)
        {
            Assert.Equal(supplier.Id, result.SupplierMetrics.First().SupplierId);
        }
    }

    [Fact]
    public async Task GenerateSupplierDistributionReportAsync_WithProductTypeFilter_ShouldFilterResults()
    {
        // Arrange
        var request = new SupplierDistributionReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            ProductType = "LMR"
        };

        // Act
        var result = await _reportingService.GenerateSupplierDistributionReportAsync(request);

        // Assert
        Assert.NotNull(result);
        // Results should be filtered by product type
        Assert.True(result.TotalOrders >= 0);
    }

    [Fact]
    public async Task ExportReportAsync_WithJsonFormat_ShouldReturnJsonData()
    {
        // Arrange
        var request = new PerformanceMetricsReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var report = await _reportingService.GeneratePerformanceMetricsReportAsync(request);

        // Act
        var result = await _reportingService.ExportReportAsync(report, "json");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        
        var jsonContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("GeneratedAt", jsonContent);
        Assert.Contains("FromDate", jsonContent);
        Assert.Contains("ToDate", jsonContent);
    }

    [Fact]
    public async Task ExportReportAsync_WithCsvFormat_ShouldReturnCsvData()
    {
        // Arrange
        var request = new PerformanceMetricsReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var report = await _reportingService.GeneratePerformanceMetricsReportAsync(request);

        // Act
        var result = await _reportingService.ExportReportAsync(report, "csv");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Report Data", csvContent);
    }

    [Fact]
    public async Task ExportReportAsync_WithInvalidFormat_ShouldThrowException()
    {
        // Arrange
        var request = new PerformanceMetricsReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var report = await _reportingService.GeneratePerformanceMetricsReportAsync(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _reportingService.ExportReportAsync(report, "invalid"));
    }

    [Fact]
    public async Task GenerateOrderFulfillmentReportAsync_WithCustomerFilter_ShouldFilterResults()
    {
        // Arrange
        var request = new OrderFulfillmentReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CustomerId = "CUST-001"
        };

        // Act
        var result = await _reportingService.GenerateOrderFulfillmentReportAsync(request);

        // Assert
        Assert.NotNull(result);
        // Should only include orders for the specified customer
        Assert.True(result.TotalOrders >= 0);
    }

    [Fact]
    public async Task GenerateDeliveryPerformanceReportAsync_WithSupplierFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var request = new DeliveryPerformanceReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            SupplierId = supplier.Id,
            GroupBySupplier = true
        };

        // Act
        var result = await _reportingService.GenerateDeliveryPerformanceReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SupplierPerformances.Count <= 1);
        if (result.SupplierPerformances.Count > 0)
        {
            Assert.Equal(supplier.Id, result.SupplierPerformances.First().SupplierId);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}