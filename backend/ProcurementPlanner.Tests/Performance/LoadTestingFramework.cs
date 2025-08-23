using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace ProcurementPlanner.Tests.Performance;

/// <summary>
/// Framework for conducting load and performance tests on the procurement system
/// </summary>
public class LoadTestingFramework : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;
    private ApplicationDbContext _context;
    private readonly ConcurrentBag<PerformanceMetric> _metrics;

    public LoadTestingFramework(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _metrics = new ConcurrentBag<PerformanceMetric>();
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        await SeedPerformanceTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context?.Dispose();
    }

    [Fact]
    public async Task HighVolumeOrderCreation_ShouldMaintainPerformance()
    {
        // Arrange
        const int concurrentUsers = 50;
        const int ordersPerUser = 10;
        const int maxAcceptableResponseTimeMs = 2000;

        var clients = Enumerable.Range(0, concurrentUsers)
            .Select(_ => _factory.CreateClient())
            .ToList();

        // Act
        var tasks = clients.Select(async (client, userIndex) =>
        {
            var userMetrics = new List<PerformanceMetric>();
            
            for (int orderIndex = 0; orderIndex < ordersPerUser; orderIndex++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    var createOrderDto = new CreateOrderDto
                    {
                        CustomerId = $"LOAD{userIndex:D3}",
                        CustomerName = $"Load Test Customer {userIndex}",
                        ProductType = orderIndex % 2 == 0 ? ProductType.LMR : ProductType.FFV,
                        RequestedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)),
                        Items = new List<CreateOrderItemDto>
                        {
                            new()
                            {
                                ProductCode = $"LOAD{orderIndex:D3}",
                                Description = $"Load Test Product {orderIndex}",
                                Quantity = Random.Shared.Next(1, 100),
                                Unit = "EA",
                                UnitPrice = (decimal)(Random.Shared.NextDouble() * 100)
                            }
                        }
                    };

                    var response = await client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
                    stopwatch.Stop();

                    var metric = new PerformanceMetric
                    {
                        Operation = "CreateOrder",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Success = response.IsSuccessStatusCode,
                        StatusCode = response.StatusCode,
                        Timestamp = DateTime.UtcNow,
                        UserId = userIndex,
                        RequestSize = JsonSerializer.Serialize(createOrderDto).Length
                    };

                    userMetrics.Add(metric);
                    _metrics.Add(metric);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var metric = new PerformanceMetric
                    {
                        Operation = "CreateOrder",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Success = false,
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow,
                        UserId = userIndex
                    };
                    
                    userMetrics.Add(metric);
                    _metrics.Add(metric);
                }
            }

            return userMetrics;
        });

        var allResults = await Task.WhenAll(tasks);

        // Cleanup clients
        foreach (var client in clients)
        {
            client.Dispose();
        }

        // Assert
        var allMetrics = allResults.SelectMany(r => r).ToList();
        var successfulRequests = allMetrics.Where(m => m.Success).ToList();
        var failedRequests = allMetrics.Where(m => !m.Success).ToList();

        // Performance assertions
        var averageResponseTime = successfulRequests.Average(m => m.ResponseTimeMs);
        var maxResponseTime = successfulRequests.Max(m => m.ResponseTimeMs);
        var successRate = (double)successfulRequests.Count / allMetrics.Count * 100;

        // Log performance metrics
        Console.WriteLine($"Load Test Results:");
        Console.WriteLine($"Total Requests: {allMetrics.Count}");
        Console.WriteLine($"Successful Requests: {successfulRequests.Count}");
        Console.WriteLine($"Failed Requests: {failedRequests.Count}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"Max Response Time: {maxResponseTime}ms");
        Console.WriteLine($"95th Percentile: {GetPercentile(successfulRequests.Select(m => m.ResponseTimeMs), 95):F2}ms");

        // Assertions
        successRate.Should().BeGreaterThan(95, "Success rate should be above 95%");
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
            $"Average response time should be less than {maxAcceptableResponseTimeMs}ms");
        maxResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs * 2, 
            "Max response time should be reasonable");
    }

    [Fact]
    public async Task ConcurrentDashboardAccess_ShouldHandleHighLoad()
    {
        // Arrange
        const int concurrentUsers = 100;
        const int requestsPerUser = 5;
        const int maxAcceptableResponseTimeMs = 1000;

        var clients = Enumerable.Range(0, concurrentUsers)
            .Select(_ => _factory.CreateClient())
            .ToList();

        // Act
        var tasks = clients.Select(async (client, userIndex) =>
        {
            var userMetrics = new List<PerformanceMetric>();
            
            for (int requestIndex = 0; requestIndex < requestsPerUser; requestIndex++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    var response = await client.GetAsync("/api/order/dashboard");
                    stopwatch.Stop();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    var metric = new PerformanceMetric
                    {
                        Operation = "GetDashboard",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Success = response.IsSuccessStatusCode,
                        StatusCode = response.StatusCode,
                        Timestamp = DateTime.UtcNow,
                        UserId = userIndex,
                        ResponseSize = responseContent.Length
                    };

                    userMetrics.Add(metric);
                    _metrics.Add(metric);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var metric = new PerformanceMetric
                    {
                        Operation = "GetDashboard",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Success = false,
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow,
                        UserId = userIndex
                    };
                    
                    userMetrics.Add(metric);
                    _metrics.Add(metric);
                }
            }

            return userMetrics;
        });

        var allResults = await Task.WhenAll(tasks);

        // Cleanup clients
        foreach (var client in clients)
        {
            client.Dispose();
        }

        // Assert
        var allMetrics = allResults.SelectMany(r => r).ToList();
        var successfulRequests = allMetrics.Where(m => m.Success).ToList();

        var averageResponseTime = successfulRequests.Average(m => m.ResponseTimeMs);
        var successRate = (double)successfulRequests.Count / allMetrics.Count * 100;

        Console.WriteLine($"Dashboard Load Test Results:");
        Console.WriteLine($"Total Requests: {allMetrics.Count}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");

        successRate.Should().BeGreaterThan(98, "Dashboard should handle concurrent access with high success rate");
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
            "Dashboard should respond quickly under load");
    }

    [Fact]
    public async Task DatabaseConnectionPooling_ShouldHandleConcurrentQueries()
    {
        // Arrange
        const int concurrentQueries = 200;
        const int maxAcceptableResponseTimeMs = 3000;

        var clients = Enumerable.Range(0, concurrentQueries)
            .Select(_ => _factory.CreateClient())
            .ToList();

        // Act
        var tasks = clients.Select(async (client, index) =>
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Mix different types of database operations
                var operation = index % 4 switch
                {
                    0 => "/api/order",
                    1 => "/api/supplier",
                    2 => "/api/order/dashboard",
                    _ => "/api/audit"
                };

                var response = await client.GetAsync(operation);
                stopwatch.Stop();

                return new PerformanceMetric
                {
                    Operation = $"DatabaseQuery_{operation}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode,
                    Timestamp = DateTime.UtcNow,
                    UserId = index
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new PerformanceMetric
                {
                    Operation = "DatabaseQuery",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    UserId = index
                };
            }
        });

        var results = await Task.WhenAll(tasks);

        // Cleanup clients
        foreach (var client in clients)
        {
            client.Dispose();
        }

        // Assert
        var successfulRequests = results.Where(r => r.Success).ToList();
        var averageResponseTime = successfulRequests.Average(r => r.ResponseTimeMs);
        var successRate = (double)successfulRequests.Count / results.Length * 100;

        Console.WriteLine($"Database Connection Pool Test Results:");
        Console.WriteLine($"Total Queries: {results.Length}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");

        successRate.Should().BeGreaterThan(95, "Database should handle concurrent connections");
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
            "Database queries should complete within acceptable time");
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable_UnderLoad()
    {
        // Arrange
        const int iterations = 100;
        const long maxMemoryIncreaseMB = 100;

        var initialMemory = GC.GetTotalMemory(true);
        var client = _factory.CreateClient();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var createOrderDto = new CreateOrderDto
            {
                CustomerId = $"MEM{i:D3}",
                CustomerName = $"Memory Test Customer {i}",
                ProductType = ProductType.LMR,
                RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
                Items = new List<CreateOrderItemDto>
                {
                    new()
                    {
                        ProductCode = $"MEM{i:D3}",
                        Description = $"Memory Test Product {i}",
                        Quantity = 10,
                        Unit = "EA",
                        UnitPrice = 5.00m
                    }
                }
            };

            await client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);

            // Force garbage collection every 10 iterations
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        client.Dispose();

        // Assert
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024 * 1024);

        Console.WriteLine($"Memory Usage Test Results:");
        Console.WriteLine($"Initial Memory: {initialMemory / (1024 * 1024)}MB");
        Console.WriteLine($"Final Memory: {finalMemory / (1024 * 1024)}MB");
        Console.WriteLine($"Memory Increase: {memoryIncreaseMB}MB");

        memoryIncreaseMB.Should().BeLessThan(maxMemoryIncreaseMB, 
            "Memory usage should not increase significantly under load");
    }

    [Fact]
    public async Task ApiThroughput_ShouldMeetMinimumRequirements()
    {
        // Arrange
        const int testDurationSeconds = 30;
        const int minRequestsPerSecond = 50;
        
        var client = _factory.CreateClient();
        var requestCount = 0;
        var successCount = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        
        while (stopwatch.Elapsed.TotalSeconds < testDurationSeconds)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await client.GetAsync("/api/order/dashboard");
                    Interlocked.Increment(ref requestCount);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref requestCount);
                }
            });
            
            tasks.Add(task);
            
            // Limit concurrent tasks to prevent overwhelming the system
            if (tasks.Count >= 100)
            {
                await Task.WhenAny(tasks);
                tasks.RemoveAll(t => t.IsCompleted);
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        client.Dispose();

        // Assert
        var actualDuration = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = requestCount / actualDuration;
        var successRate = (double)successCount / requestCount * 100;

        Console.WriteLine($"Throughput Test Results:");
        Console.WriteLine($"Test Duration: {actualDuration:F2}s");
        Console.WriteLine($"Total Requests: {requestCount}");
        Console.WriteLine($"Successful Requests: {successCount}");
        Console.WriteLine($"Requests per Second: {requestsPerSecond:F2}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");

        requestsPerSecond.Should().BeGreaterThan(minRequestsPerSecond, 
            $"API should handle at least {minRequestsPerSecond} requests per second");
        successRate.Should().BeGreaterThan(95, "Success rate should remain high under load");
    }

    [Fact]
    public async Task CacheEffectiveness_ShouldImprovePerformance()
    {
        // Arrange
        const int warmupRequests = 10;
        const int testRequests = 50;
        
        var client = _factory.CreateClient();

        // Act - Warmup cache
        for (int i = 0; i < warmupRequests; i++)
        {
            await client.GetAsync("/api/order/dashboard");
        }

        // Measure cached performance
        var cachedMetrics = new List<long>();
        for (int i = 0; i < testRequests; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync("/api/order/dashboard");
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                cachedMetrics.Add(stopwatch.ElapsedMilliseconds);
            }
        }

        client.Dispose();

        // Assert
        var averageCachedTime = cachedMetrics.Average();
        var maxCachedTime = cachedMetrics.Max();

        Console.WriteLine($"Cache Performance Test Results:");
        Console.WriteLine($"Average Cached Response Time: {averageCachedTime:F2}ms");
        Console.WriteLine($"Max Cached Response Time: {maxCachedTime}ms");

        averageCachedTime.Should().BeLessThan(500, "Cached responses should be fast");
        maxCachedTime.Should().BeLessThan(1000, "Even worst-case cached responses should be reasonable");
    }

    // Helper methods
    private async Task SeedPerformanceTestDataAsync()
    {
        // Create test suppliers for performance testing
        var suppliers = Enumerable.Range(1, 20).Select(i => new Supplier
        {
            Id = Guid.NewGuid(),
            Name = $"Performance Test Supplier {i}",
            ContactEmail = $"perf{i}@test.com",
            ContactPhone = $"555-{i:D4}",
            Address = $"{i} Performance St, Test City, TC 12345",
            IsActive = true,
            Capabilities = new List<SupplierCapability>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductType = ProductType.LMR,
                    MaxMonthlyCapacity = Random.Shared.Next(100, 1000),
                    CurrentCommitments = Random.Shared.Next(10, 100),
                    QualityRating = (decimal)(Random.Shared.NextDouble() * 2 + 3)
                }
            }
        }).ToList();

        await _context.Suppliers.AddRangeAsync(suppliers);
        await _context.SaveChangesAsync();
    }

    private static double GetPercentile(IEnumerable<long> values, int percentile)
    {
        var sortedValues = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

/// <summary>
/// Represents a performance metric captured during load testing
/// </summary>
public class PerformanceMetric
{
    public string Operation { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public System.Net.HttpStatusCode? StatusCode { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
    public int UserId { get; set; }
    public int RequestSize { get; set; }
    public int ResponseSize { get; set; }
}