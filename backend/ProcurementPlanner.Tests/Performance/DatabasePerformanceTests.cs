using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Repositories;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace ProcurementPlanner.Tests.Performance;

public class DatabasePerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly OptimizedCustomerOrderRepository _repository;

    public DatabasePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddMemoryCache();
        
        // Configure options
        services.Configure<PaginationOptions>(options =>
        {
            options.DefaultPageSize = 20;
            options.MaxPageSize = 100;
            options.EnableTotalCountOptimization = true;
        });
        
        services.Configure<QueryCacheOptions>(options =>
        {
            options.DefaultCacheTimeout = TimeSpan.FromMinutes(5);
            options.EnableQueryResultCaching = true;
        });
        
        services.Configure<DatabaseMonitoringOptions>(options =>
        {
            options.EnableSlowQueryLogging = true;
            options.SlowQueryThresholdMs = 100;
        });
        
        // Use in-memory database for testing
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<IDatabaseMonitoringService, DatabaseMonitoringService>();
        services.AddScoped<OptimizedCustomerOrderRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _repository = _serviceProvider.GetRequiredService<OptimizedCustomerOrderRepository>();
        
        // Seed test data
        SeedTestData().Wait();
    }

    [Fact]
    public async Task GetOrdersAsync_WithFilters_PerformanceTest()
    {
        // Arrange
        var filter = new OrderFilterRequest
        {
            Page = 1,
            PageSize = 20,
            Status = OrderStatus.Submitted,
            ProductType = ProductType.LMR,
            DeliveryDateFrom = DateTime.UtcNow.Date,
            DeliveryDateTo = DateTime.UtcNow.Date.AddDays(30)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _repository.GetOrdersAsync(filter);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count <= filter.PageSize);
        
        _output.WriteLine($"GetOrdersAsync completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved {result.Items.Count} orders out of {result.TotalCount} total");
        
        // Performance assertion - should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_PerformanceTest()
    {
        // Arrange
        var filter = new DashboardFilterRequest
        {
            ProductType = ProductType.LMR,
            DeliveryDateFrom = DateTime.UtcNow.Date.AddDays(-30),
            DeliveryDateTo = DateTime.UtcNow.Date.AddDays(30)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _repository.GetDashboardSummaryAsync(filter);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalOrders >= 0);
        Assert.NotNull(result.StatusCounts);
        Assert.NotNull(result.ProductTypeCounts);
        Assert.NotNull(result.OrdersByDeliveryDate);
        Assert.NotNull(result.TopCustomers);
        
        _output.WriteLine($"GetDashboardSummaryAsync completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Dashboard summary: {result.TotalOrders} total orders, {result.OverdueOrders} overdue");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Dashboard query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetOrdersByDeliveryDateRangeAsync_PerformanceTest()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _repository.GetOrdersByDeliveryDateRangeAsync(startDate, endDate);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.All(result, order => 
        {
            Assert.True(order.RequestedDeliveryDate >= startDate);
            Assert.True(order.RequestedDeliveryDate <= endDate);
        });
        
        _output.WriteLine($"GetOrdersByDeliveryDateRangeAsync completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved {result.Count} orders in date range");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Date range query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetOverdueOrdersAsync_PerformanceTest()
    {
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _repository.GetOverdueOrdersAsync();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        var today = DateTime.UtcNow.Date;
        Assert.All(result, order => 
        {
            Assert.True(order.RequestedDeliveryDate < today);
            Assert.True(order.Status != OrderStatus.Delivered);
            Assert.True(order.Status != OrderStatus.Cancelled);
        });
        
        _output.WriteLine($"GetOverdueOrdersAsync completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved {result.Count} overdue orders");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Overdue orders query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task OrderNumberExistsAsync_PerformanceTest()
    {
        // Arrange
        const int iterations = 100;
        var orderNumbers = Enumerable.Range(1, iterations)
            .Select(i => $"ORD-{i:D6}")
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = orderNumbers.Select(orderNumber => _repository.OrderNumberExistsAsync(orderNumber));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(iterations, results.Length);
        
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        _output.WriteLine($"OrderNumberExistsAsync: {iterations} checks in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTime:F2}ms per check)");
        
        // Performance assertion
        Assert.True(avgTime < 10, $"Average order number check took too long: {avgTime:F2}ms");
    }

    [Fact]
    public async Task ConcurrentQueries_PerformanceTest()
    {
        // Arrange
        const int concurrentQueries = 10;
        var filter = new OrderFilterRequest
        {
            Page = 1,
            PageSize = 10,
            Status = OrderStatus.Submitted
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(1, concurrentQueries)
            .Select(_ => _repository.GetOrdersAsync(filter))
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(concurrentQueries, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        
        var avgTime = stopwatch.ElapsedMilliseconds / (double)concurrentQueries;
        _output.WriteLine($"Concurrent queries: {concurrentQueries} queries in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTime:F2}ms per query)");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Concurrent queries took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CompiledQueries_PerformanceTest()
    {
        // Arrange
        const int iterations = 50;
        var orderIds = Enumerable.Range(1, iterations)
            .Select(_ => Guid.NewGuid())
            .ToList();

        // Act - Test compiled query performance
        var stopwatch = Stopwatch.StartNew();
        var tasks = orderIds.Select(id => _repository.GetByIdAsync(id));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(iterations, results.Length);
        
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        _output.WriteLine($"Compiled queries: {iterations} queries in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTime:F2}ms per query)");
        
        // Performance assertion - compiled queries should be faster
        Assert.True(avgTime < 20, $"Average compiled query took too long: {avgTime:F2}ms");
    }

    [Fact]
    public async Task PaginationCaching_PerformanceTest()
    {
        // Arrange
        var filter = new OrderFilterRequest
        {
            Page = 1,
            PageSize = 10,
            Status = OrderStatus.Submitted
        };

        // Act - First call (should cache total count)
        var stopwatch1 = Stopwatch.StartNew();
        var result1 = await _repository.GetOrdersAsync(filter);
        stopwatch1.Stop();

        // Act - Second call (should use cached total count)
        var stopwatch2 = Stopwatch.StartNew();
        var result2 = await _repository.GetOrdersAsync(filter);
        stopwatch2.Stop();

        // Assert
        Assert.Equal(result1.TotalCount, result2.TotalCount);
        
        _output.WriteLine($"First call: {stopwatch1.ElapsedMilliseconds}ms, Second call: {stopwatch2.ElapsedMilliseconds}ms");
        
        // Second call should be faster due to caching (allowing some variance)
        Assert.True(stopwatch2.ElapsedMilliseconds <= stopwatch1.ElapsedMilliseconds + 50, 
            "Second call should benefit from caching");
    }

    [Fact]
    public async Task LargeDataset_PaginationPerformance()
    {
        // Arrange - Create additional test data
        await SeedLargeDataset(1000);
        
        var filter = new OrderFilterRequest
        {
            Page = 1,
            PageSize = 50,
            ProductType = ProductType.LMR
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _repository.GetOrdersAsync(filter);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count <= filter.PageSize);
        
        _output.WriteLine($"Large dataset pagination: {result.Items.Count} items from {result.TotalCount} total in {stopwatch.ElapsedMilliseconds}ms");
        
        // Performance assertion for large dataset
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Large dataset pagination took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task IndexOptimization_FilteredQueries()
    {
        // Arrange - Test queries that should benefit from our indexes
        var testCases = new[]
        {
            new OrderFilterRequest { Status = OrderStatus.Submitted, ProductType = ProductType.LMR },
            new OrderFilterRequest { DeliveryDateFrom = DateTime.UtcNow.Date, DeliveryDateTo = DateTime.UtcNow.Date.AddDays(7) },
            new OrderFilterRequest { CustomerId = "CUST001", Status = OrderStatus.UnderReview },
            new OrderFilterRequest { IsOverdue = true }
        };

        // Act & Assert
        foreach (var filter in testCases)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _repository.GetOrdersAsync(filter);
            stopwatch.Stop();

            _output.WriteLine($"Filtered query ({filter.Status}, {filter.ProductType}, {filter.CustomerId}, {filter.IsOverdue}): {stopwatch.ElapsedMilliseconds}ms");
            
            // Each filtered query should complete quickly due to indexes
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"Filtered query took too long: {stopwatch.ElapsedMilliseconds}ms for filter {filter}");
        }
    }

    [Fact]
    public async Task DatabaseConnectionPooling_StressTest()
    {
        // Arrange
        const int concurrentConnections = 20;
        const int queriesPerConnection = 5;
        
        var tasks = new List<Task>();
        
        // Act - Simulate high concurrent load
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < concurrentConnections; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < queriesPerConnection; j++)
                {
                    var filter = new OrderFilterRequest
                    {
                        Page = j + 1,
                        PageSize = 5,
                        Status = (OrderStatus)(j % 4 + 1)
                    };
                    
                    await _repository.GetOrdersAsync(filter);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var totalQueries = concurrentConnections * queriesPerConnection;
        var avgTimePerQuery = stopwatch.ElapsedMilliseconds / (double)totalQueries;
        
        _output.WriteLine($"Connection pooling stress test: {totalQueries} queries in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimePerQuery:F2}ms per query)");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Connection pooling stress test took too long: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(avgTimePerQuery < 100, $"Average query time too high under load: {avgTimePerQuery:F2}ms");
    }

    private async Task SeedTestData()
    {
        // Create test customers and orders
        var customers = new[]
        {
            ("CUST001", "Customer One"),
            ("CUST002", "Customer Two"),
            ("CUST003", "Customer Three")
        };

        var random = new Random(42); // Fixed seed for consistent test data
        var orders = new List<CustomerOrder>();

        for (int i = 1; i <= 100; i++)
        {
            var customer = customers[i % customers.Length];
            var order = new CustomerOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{i:D6}",
                CustomerId = customer.Item1,
                CustomerName = customer.Item2,
                ProductType = i % 2 == 0 ? ProductType.LMR : ProductType.FFV,
                Status = (OrderStatus)(i % 4 + 1), // Cycle through statuses
                RequestedDeliveryDate = DateTime.UtcNow.Date.AddDays(random.Next(-10, 30)),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 60)),

                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductCode = $"PROD-{i:D3}",
                        Description = $"Product {i}",
                        Quantity = random.Next(1, 50),
                        Unit = "EA",
                        UnitPrice = random.Next(10, 500),
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            orders.Add(order);
        }

        _context.CustomerOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    private async Task SeedLargeDataset(int orderCount)
    {
        var customers = new[]
        {
            ("CUST001", "Customer One"),
            ("CUST002", "Customer Two"),
            ("CUST003", "Customer Three"),
            ("CUST004", "Customer Four"),
            ("CUST005", "Customer Five")
        };

        var random = new Random(123); // Fixed seed for consistent test data
        var orders = new List<CustomerOrder>();

        for (int i = 101; i <= 100 + orderCount; i++)
        {
            var customer = customers[i % customers.Length];
            var order = new CustomerOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{i:D6}",
                CustomerId = customer.Item1,
                CustomerName = customer.Item2,
                ProductType = i % 2 == 0 ? ProductType.LMR : ProductType.FFV,
                Status = (OrderStatus)(i % 8 + 1), // Cycle through all statuses
                RequestedDeliveryDate = DateTime.UtcNow.Date.AddDays(random.Next(-30, 60)),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 90)),

                Items = Enumerable.Range(1, random.Next(1, 5)).Select(j => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = $"PROD-{i:D3}-{j}",
                    Description = $"Product {i}-{j}",
                    Quantity = random.Next(1, 100),
                    Unit = "EA",
                    UnitPrice = random.Next(5, 1000),
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            orders.Add(order);
        }

        _context.CustomerOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}