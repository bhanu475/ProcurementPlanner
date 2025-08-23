using Microsoft.AspNetCore.Mvc.Testing;
using ProcurementPlanner.API.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace ProcurementPlanner.Tests.Performance;

/// <summary>
/// Comprehensive API performance tests focusing on response times and throughput
/// </summary>
public class ApiPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Theory]
    [InlineData("/api/order", "GET")]
    [InlineData("/api/order/dashboard", "GET")]
    [InlineData("/api/supplier", "GET")]
    [InlineData("/api/audit", "GET")]
    [InlineData("/health", "GET")]
    public async Task ApiEndpoint_ResponseTime_ShouldMeetSLA(string endpoint, string method)
    {
        // Arrange
        const int testIterations = 20;
        const int maxAcceptableResponseTimeMs = 2000;
        const int targetResponseTimeMs = 500;
        
        var client = _factory.CreateClient();
        var responseTimes = new List<long>();

        // Act
        for (int i = 0; i < testIterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            HttpResponseMessage response = method.ToUpper() switch
            {
                "GET" => await client.GetAsync(endpoint),
                "POST" => await client.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json")),
                _ => throw new ArgumentException($"Unsupported method: {method}")
            };
            
            stopwatch.Stop();
            
            // Only record successful responses for performance measurement
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                responseTimes.Add(stopwatch.ElapsedMilliseconds);
            }
            
            response.Dispose();
        }

        client.Dispose();

        // Assert
        responseTimes.Should().NotBeEmpty($"Should have recorded response times for {endpoint}");
        
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();
        var p95ResponseTime = GetPercentile(responseTimes, 95);

        Console.WriteLine($"Performance Results for {method} {endpoint}:");
        Console.WriteLine($"  Average: {averageResponseTime:F2}ms");
        Console.WriteLine($"  Min: {minResponseTime}ms");
        Console.WriteLine($"  Max: {maxResponseTime}ms");
        Console.WriteLine($"  95th Percentile: {p95ResponseTime:F2}ms");

        // SLA assertions
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
            $"Average response time for {endpoint} should be under {maxAcceptableResponseTimeMs}ms");
        
        p95ResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs * 1.5, 
            $"95th percentile response time for {endpoint} should be reasonable");

        // Performance target (warning, not failure)
        if (averageResponseTime > targetResponseTimeMs)
        {
            Console.WriteLine($"WARNING: Average response time ({averageResponseTime:F2}ms) exceeds target ({targetResponseTimeMs}ms)");
        }
    }

    [Fact]
    public async Task OrderCreation_PerformanceUnderLoad_ShouldMaintainThroughput()
    {
        // Arrange
        const int concurrentUsers = 20;
        const int ordersPerUser = 5;
        const int maxAcceptableResponseTimeMs = 3000;
        const double minSuccessRate = 95.0;

        var clients = Enumerable.Range(0, concurrentUsers)
            .Select(_ => _factory.CreateClient())
            .ToList();

        var allMetrics = new ConcurrentBag<ApiPerformanceMetric>();

        // Act
        var userTasks = clients.Select(async (client, userIndex) =>
        {
            for (int orderIndex = 0; orderIndex < ordersPerUser; orderIndex++)
            {
                var stopwatch = Stopwatch.StartNew();
                var success = false;
                var statusCode = System.Net.HttpStatusCode.InternalServerError;
                var errorMessage = string.Empty;

                try
                {
                    var createOrderDto = new CreateOrderDto
                    {
                        CustomerId = $"PERF{userIndex:D3}",
                        CustomerName = $"Performance Test User {userIndex}",
                        ProductType = orderIndex % 2 == 0 ? ProductType.LMR : ProductType.FFV,
                        RequestedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)),
                        Items = new List<CreateOrderItemDto>
                        {
                            new()
                            {
                                ProductCode = $"PERF{orderIndex:D3}",
                                Description = $"Performance Test Product {orderIndex}",
                                Quantity = Random.Shared.Next(1, 50),
                                Unit = "EA",
                                UnitPrice = (decimal)(Random.Shared.NextDouble() * 100)
                            }
                        }
                    };

                    var response = await client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
                    statusCode = response.StatusCode;
                    success = response.IsSuccessStatusCode;
                    
                    if (!success)
                    {
                        errorMessage = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
                finally
                {
                    stopwatch.Stop();
                }

                allMetrics.Add(new ApiPerformanceMetric
                {
                    Endpoint = "/api/order",
                    Method = "POST",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = success,
                    StatusCode = statusCode,
                    UserId = userIndex,
                    RequestIndex = orderIndex,
                    ErrorMessage = errorMessage
                });
            }
        });

        await Task.WhenAll(userTasks);

        // Cleanup
        foreach (var client in clients)
        {
            client.Dispose();
        }

        // Assert
        var metrics = allMetrics.ToList();
        var successfulRequests = metrics.Where(m => m.Success).ToList();
        var failedRequests = metrics.Where(m => !m.Success).ToList();

        var successRate = (double)successfulRequests.Count / metrics.Count * 100;
        var averageResponseTime = successfulRequests.Any() ? successfulRequests.Average(m => m.ResponseTimeMs) : 0;
        var maxResponseTime = successfulRequests.Any() ? successfulRequests.Max(m => m.ResponseTimeMs) : 0;
        var throughput = metrics.Count / (metrics.Max(m => m.ResponseTimeMs) / 1000.0);

        Console.WriteLine($"Order Creation Load Test Results:");
        Console.WriteLine($"Total Requests: {metrics.Count}");
        Console.WriteLine($"Successful Requests: {successfulRequests.Count}");
        Console.WriteLine($"Failed Requests: {failedRequests.Count}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"Max Response Time: {maxResponseTime}ms");
        Console.WriteLine($"Estimated Throughput: {throughput:F2} req/sec");

        if (failedRequests.Any())
        {
            Console.WriteLine("Failed Request Details:");
            foreach (var failed in failedRequests.Take(5))
            {
                Console.WriteLine($"  User {failed.UserId}, Request {failed.RequestIndex}: {failed.StatusCode} - {failed.ErrorMessage}");
            }
        }

        // Assertions
        successRate.Should().BeGreaterOrEqualTo(minSuccessRate, 
            $"Success rate should be at least {minSuccessRate}%");
        
        if (successfulRequests.Any())
        {
            averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
                "Average response time should be acceptable under load");
        }
    }

    [Fact]
    public async Task DashboardEndpoint_ConcurrentAccess_ShouldScaleWell()
    {
        // Arrange
        const int concurrentUsers = 50;
        const int requestsPerUser = 3;
        const int maxAcceptableResponseTimeMs = 1500;

        var semaphore = new SemaphoreSlim(concurrentUsers);
        var allMetrics = new ConcurrentBag<ApiPerformanceMetric>();

        // Act
        var tasks = Enumerable.Range(0, concurrentUsers * requestsPerUser).Select(async requestIndex =>
        {
            await semaphore.WaitAsync();
            
            try
            {
                var client = _factory.CreateClient();
                var stopwatch = Stopwatch.StartNew();
                var success = false;
                var statusCode = System.Net.HttpStatusCode.InternalServerError;

                try
                {
                    var response = await client.GetAsync("/api/order/dashboard");
                    statusCode = response.StatusCode;
                    success = response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Request {requestIndex} failed: {ex.Message}");
                }
                finally
                {
                    stopwatch.Stop();
                    client.Dispose();
                }

                allMetrics.Add(new ApiPerformanceMetric
                {
                    Endpoint = "/api/order/dashboard",
                    Method = "GET",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = success,
                    StatusCode = statusCode,
                    RequestIndex = requestIndex
                });
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        var metrics = allMetrics.ToList();
        var successfulRequests = metrics.Where(m => m.Success).ToList();

        var successRate = (double)successfulRequests.Count / metrics.Count * 100;
        var averageResponseTime = successfulRequests.Any() ? successfulRequests.Average(m => m.ResponseTimeMs) : 0;
        var p95ResponseTime = successfulRequests.Any() ? GetPercentile(successfulRequests.Select(m => m.ResponseTimeMs), 95) : 0;

        Console.WriteLine($"Dashboard Concurrent Access Test Results:");
        Console.WriteLine($"Total Requests: {metrics.Count}");
        Console.WriteLine($"Successful Requests: {successfulRequests.Count}");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"95th Percentile Response Time: {p95ResponseTime:F2}ms");

        successRate.Should().BeGreaterThan(95, "Dashboard should handle concurrent access well");
        
        if (successfulRequests.Any())
        {
            averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
                "Dashboard should respond quickly under concurrent load");
        }
    }

    [Fact]
    public async Task ApiThroughput_SustainedLoad_ShouldMeetRequirements()
    {
        // Arrange
        const int testDurationSeconds = 60;
        const int minRequestsPerSecond = 30;
        const int maxConcurrentRequests = 100;

        var client = _factory.CreateClient();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);
        var metrics = new ConcurrentBag<ApiPerformanceMetric>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        var requestCounter = 0;

        while (stopwatch.Elapsed.TotalSeconds < testDurationSeconds)
        {
            var requestIndex = Interlocked.Increment(ref requestCounter);
            
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                
                try
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    var success = false;
                    var statusCode = System.Net.HttpStatusCode.InternalServerError;

                    try
                    {
                        // Mix different endpoints to simulate realistic load
                        var endpoint = requestIndex % 3 switch
                        {
                            0 => "/api/order/dashboard",
                            1 => "/api/order",
                            _ => "/health"
                        };

                        var response = await client.GetAsync(endpoint);
                        statusCode = response.StatusCode;
                        success = response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        // Request failed
                    }
                    finally
                    {
                        requestStopwatch.Stop();
                    }

                    metrics.Add(new ApiPerformanceMetric
                    {
                        Endpoint = "Mixed",
                        Method = "GET",
                        ResponseTimeMs = requestStopwatch.ElapsedMilliseconds,
                        Success = success,
                        StatusCode = statusCode,
                        RequestIndex = requestIndex,
                        Timestamp = DateTime.UtcNow
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);

            // Clean up completed tasks periodically
            if (tasks.Count > maxConcurrentRequests * 2)
            {
                var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
                foreach (var completedTask in completedTasks)
                {
                    tasks.Remove(completedTask);
                    completedTask.Dispose();
                }
            }

            // Small delay to prevent overwhelming the system
            await Task.Delay(10);
        }

        // Wait for remaining tasks
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        client.Dispose();

        // Cleanup tasks
        foreach (var task in tasks)
        {
            task.Dispose();
        }

        // Assert
        var allMetrics = metrics.ToList();
        var successfulRequests = allMetrics.Where(m => m.Success).ToList();
        
        var actualDuration = stopwatch.Elapsed.TotalSeconds;
        var totalRequests = allMetrics.Count;
        var actualThroughput = totalRequests / actualDuration;
        var successRate = (double)successfulRequests.Count / totalRequests * 100;
        var averageResponseTime = successfulRequests.Any() ? successfulRequests.Average(m => m.ResponseTimeMs) : 0;

        Console.WriteLine($"Sustained Load Test Results:");
        Console.WriteLine($"Test Duration: {actualDuration:F2}s");
        Console.WriteLine($"Total Requests: {totalRequests}");
        Console.WriteLine($"Successful Requests: {successfulRequests.Count}");
        Console.WriteLine($"Throughput: {actualThroughput:F2} req/sec");
        Console.WriteLine($"Success Rate: {successRate:F2}%");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");

        // Assertions
        actualThroughput.Should().BeGreaterOrEqualTo(minRequestsPerSecond, 
            $"Should achieve at least {minRequestsPerSecond} requests per second");
        
        successRate.Should().BeGreaterThan(90, 
            "Should maintain high success rate under sustained load");
    }

    [Fact]
    public async Task ApiResponseSize_ShouldBeOptimized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var endpoints = new[]
        {
            "/api/order",
            "/api/order/dashboard",
            "/api/supplier",
            "/health"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var contentLength = content.Length;
                    var contentType = response.Content.Headers.ContentType?.MediaType;

                    Console.WriteLine($"Response Size Analysis for {endpoint}:");
                    Console.WriteLine($"  Content Length: {contentLength:N0} bytes");
                    Console.WriteLine($"  Content Type: {contentType}");
                    Console.WriteLine($"  Has Compression: {response.Content.Headers.ContentEncoding.Any()}");

                    // Basic size checks
                    contentLength.Should().BeLessThan(10 * 1024 * 1024, 
                        $"Response from {endpoint} should be less than 10MB");

                    if (contentType == "application/json")
                    {
                        // JSON responses should be reasonably sized
                        contentLength.Should().BeLessThan(1024 * 1024, 
                            $"JSON response from {endpoint} should be less than 1MB");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not analyze {endpoint}: {ex.Message}");
            }
        }

        client.Dispose();
    }

    // Helper methods
    private static double GetPercentile(IEnumerable<long> values, int percentile)
    {
        var sortedValues = values.OrderBy(x => x).ToList();
        if (!sortedValues.Any()) return 0;
        
        var index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

/// <summary>
/// Represents performance metrics for API requests
/// </summary>
public class ApiPerformanceMetric
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public int UserId { get; set; }
    public int RequestIndex { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}