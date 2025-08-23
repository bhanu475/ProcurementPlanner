using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using StackExchange.Redis;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace ProcurementPlanner.Tests.Performance;

public class CachePerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ICacheService _cacheService;

    public CachePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Redis connection for testing (assumes Redis is running locally)
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            try
            {
                return ConnectionMultiplexer.Connect("localhost:6379");
            }
            catch
            {
                // If Redis is not available, skip these tests
                throw new SkipException("Redis is not available for performance testing");
            }
        });
        
        services.AddScoped<ICacheService, RedisCacheService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task CacheService_SetAndGet_PerformanceTest()
    {
        // Arrange
        const int iterations = 100; // Reduced for faster testing
        var testData = new OrderDashboardSummary
        {
            TotalOrders = 100,
            StatusCounts = new Dictionary<Core.Entities.OrderStatus, int>
            {
                { Core.Entities.OrderStatus.Submitted, 25 },
                { Core.Entities.OrderStatus.Delivered, 75 }
            },
            ProductTypeCounts = new Dictionary<Core.Entities.ProductType, int>
            {
                { Core.Entities.ProductType.LMR, 50 },
                { Core.Entities.ProductType.FFV, 50 }
            },
            OverdueOrders = 5,
            TotalValue = 10000m
        };

        // Act - Measure Set operations
        var setStopwatch = Stopwatch.StartNew();
        var setTasks = new List<Task>();
        
        for (int i = 0; i < iterations; i++)
        {
            var key = $"perf_test_set_{i}";
            setTasks.Add(_cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5)));
        }
        
        await Task.WhenAll(setTasks);
        setStopwatch.Stop();

        // Act - Measure Get operations
        var getStopwatch = Stopwatch.StartNew();
        var getTasks = new List<Task<OrderDashboardSummary?>>();
        
        for (int i = 0; i < iterations; i++)
        {
            var key = $"perf_test_set_{i}";
            getTasks.Add(_cacheService.GetAsync<OrderDashboardSummary>(key));
        }
        
        var results = await Task.WhenAll(getTasks);
        getStopwatch.Stop();

        // Assert
        Assert.All(results, result => Assert.NotNull(result));
        
        var setAvgMs = setStopwatch.ElapsedMilliseconds / (double)iterations;
        var getAvgMs = getStopwatch.ElapsedMilliseconds / (double)iterations;
        
        _output.WriteLine($"Set operations: {iterations} items in {setStopwatch.ElapsedMilliseconds}ms (avg: {setAvgMs:F2}ms per operation)");
        _output.WriteLine($"Get operations: {iterations} items in {getStopwatch.ElapsedMilliseconds}ms (avg: {getAvgMs:F2}ms per operation)");
        
        // Performance assertions (adjust thresholds based on your requirements)
        Assert.True(setAvgMs < 50, $"Set operation average time ({setAvgMs:F2}ms) should be less than 50ms");
        Assert.True(getAvgMs < 25, $"Get operation average time ({getAvgMs:F2}ms) should be less than 25ms");

        // Cleanup
        for (int i = 0; i < iterations; i++)
        {
            await _cacheService.RemoveAsync($"perf_test_set_{i}");
        }
    }

    [Fact]
    public async Task CacheService_BasicOperations_WorkCorrectly()
    {
        // Arrange
        var testData = new OrderDashboardSummary
        {
            TotalOrders = 50,
            StatusCounts = new Dictionary<Core.Entities.OrderStatus, int>
            {
                { Core.Entities.OrderStatus.Submitted, 10 },
                { Core.Entities.OrderStatus.Delivered, 40 }
            },
            ProductTypeCounts = new Dictionary<Core.Entities.ProductType, int>
            {
                { Core.Entities.ProductType.LMR, 25 },
                { Core.Entities.ProductType.FFV, 25 }
            },
            OverdueOrders = 2,
            TotalValue = 5000m
        };
        const string key = "basic_test_key";

        // Act & Assert - Set
        await _cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5));
        
        // Act & Assert - Exists
        var exists = await _cacheService.ExistsAsync(key);
        Assert.True(exists);
        
        // Act & Assert - Get
        var retrieved = await _cacheService.GetAsync<OrderDashboardSummary>(key);
        Assert.NotNull(retrieved);
        Assert.Equal(testData.TotalOrders, retrieved.TotalOrders);
        Assert.Equal(testData.TotalValue, retrieved.TotalValue);
        
        // Act & Assert - Remove
        await _cacheService.RemoveAsync(key);
        var existsAfterRemove = await _cacheService.ExistsAsync(key);
        Assert.False(existsAfterRemove);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}