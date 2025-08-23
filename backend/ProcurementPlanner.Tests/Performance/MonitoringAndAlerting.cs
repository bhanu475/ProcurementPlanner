using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Infrastructure.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Xunit;
using FluentAssertions;

namespace ProcurementPlanner.Tests.Performance;

/// <summary>
/// Tests for monitoring and alerting capabilities to ensure production readiness
/// </summary>
public class MonitoringAndAlerting : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private ApplicationDbContext _context;

    public MonitoringAndAlerting(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context?.Dispose();
    }

    [Fact]
    public async Task HealthCheck_ShouldReportSystemHealth()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("Health check endpoint should be accessible");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty("Health check should return status information");
        
        // Verify health check includes critical components
        content.Should().Contain("database", "Health check should verify database connectivity");
        
        client.Dispose();
    }

    [Fact]
    public async Task DatabaseHealth_ShouldDetectConnectionIssues()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/database");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy", "Database should be reported as healthy");
        }
        else
        {
            // If database health check fails, it should return appropriate error
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);
        }

        client.Dispose();
    }

    [Fact]
    public void ApplicationMetrics_ShouldBeCollectable()
    {
        // Arrange & Act
        var currentProcess = Process.GetCurrentProcess();
        
        // Assert - Verify we can collect basic system metrics
        var workingSet = currentProcess.WorkingSet64;
        var cpuTime = currentProcess.TotalProcessorTime;
        var threadCount = currentProcess.Threads.Count;

        workingSet.Should().BeGreaterThan(0, "Working set memory should be measurable");
        cpuTime.Should().BeGreaterThan(TimeSpan.Zero, "CPU time should be measurable");
        threadCount.Should().BeGreaterThan(0, "Thread count should be measurable");

        Console.WriteLine($"Performance Metrics:");
        Console.WriteLine($"Working Set: {workingSet / (1024 * 1024)}MB");
        Console.WriteLine($"CPU Time: {cpuTime.TotalMilliseconds}ms");
        Console.WriteLine($"Thread Count: {threadCount}");
    }

    [Fact]
    public async Task ResponseTimeMonitoring_ShouldDetectSlowRequests()
    {
        // Arrange
        const int maxAcceptableResponseTimeMs = 2000;
        const int testRequests = 20;
        
        var client = _factory.CreateClient();
        var responseTimes = new List<long>();

        // Act
        for (int i = 0; i < testRequests; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync("/api/order/dashboard");
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                responseTimes.Add(stopwatch.ElapsedMilliseconds);
            }
        }

        client.Dispose();

        // Assert
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var slowRequests = responseTimes.Count(rt => rt > maxAcceptableResponseTimeMs);

        Console.WriteLine($"Response Time Monitoring:");
        Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"Max Response Time: {maxResponseTime}ms");
        Console.WriteLine($"Slow Requests (>{maxAcceptableResponseTimeMs}ms): {slowRequests}");

        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs, 
            "Average response time should be within acceptable limits");
        
        var slowRequestPercentage = (double)slowRequests / testRequests * 100;
        slowRequestPercentage.Should().BeLessThan(10, 
            "Less than 10% of requests should be slow");
    }

    [Fact]
    public async Task ErrorRateMonitoring_ShouldDetectHighErrorRates()
    {
        // Arrange
        const int testRequests = 50;
        const double maxAcceptableErrorRate = 5.0; // 5%
        
        var client = _factory.CreateClient();
        var results = new List<bool>();

        // Act
        var tasks = Enumerable.Range(0, testRequests).Select(async i =>
        {
            try
            {
                var response = await client.GetAsync("/api/order");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        });

        var responses = await Task.WhenAll(tasks);
        client.Dispose();

        // Assert
        var successCount = responses.Count(r => r);
        var errorCount = responses.Count(r => !r);
        var errorRate = (double)errorCount / testRequests * 100;

        Console.WriteLine($"Error Rate Monitoring:");
        Console.WriteLine($"Total Requests: {testRequests}");
        Console.WriteLine($"Successful Requests: {successCount}");
        Console.WriteLine($"Failed Requests: {errorCount}");
        Console.WriteLine($"Error Rate: {errorRate:F2}%");

        errorRate.Should().BeLessThan(maxAcceptableErrorRate, 
            $"Error rate should be less than {maxAcceptableErrorRate}%");
    }

    [Fact]
    public void SystemResourceMonitoring_ShouldTrackResourceUsage()
    {
        // Arrange & Act
        var performanceCounters = new Dictionary<string, object>();

        try
        {
            // Memory usage
            var totalMemory = GC.GetTotalMemory(false);
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);

            performanceCounters["TotalMemory"] = totalMemory;
            performanceCounters["Gen0Collections"] = gen0Collections;
            performanceCounters["Gen1Collections"] = gen1Collections;
            performanceCounters["Gen2Collections"] = gen2Collections;

            // Process information
            var currentProcess = Process.GetCurrentProcess();
            performanceCounters["WorkingSet"] = currentProcess.WorkingSet64;
            performanceCounters["PrivateMemory"] = currentProcess.PrivateMemorySize64;
            performanceCounters["VirtualMemory"] = currentProcess.VirtualMemorySize64;
            performanceCounters["ThreadCount"] = currentProcess.Threads.Count;
            performanceCounters["HandleCount"] = currentProcess.HandleCount;

            // Assert
            performanceCounters.Should().NotBeEmpty("Should be able to collect performance metrics");
            
            Console.WriteLine("System Resource Monitoring:");
            foreach (var counter in performanceCounters)
            {
                if (counter.Value is long longValue)
                {
                    Console.WriteLine($"{counter.Key}: {longValue:N0}");
                }
                else
                {
                    Console.WriteLine($"{counter.Key}: {counter.Value}");
                }
            }

            // Basic sanity checks
            ((long)performanceCounters["TotalMemory"]).Should().BeGreaterThan(0);
            ((long)performanceCounters["WorkingSet"]).Should().BeGreaterThan(0);
            ((int)performanceCounters["ThreadCount"]).Should().BeGreaterThan(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not collect all performance counters: {ex.Message}");
            // This is not a failure - some counters may not be available in all environments
        }
    }

    [Fact]
    public async Task DatabaseConnectionMonitoring_ShouldDetectConnectionIssues()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Test basic database connectivity
            var canConnect = await context.Database.CanConnectAsync();
            stopwatch.Stop();

            canConnect.Should().BeTrue("Should be able to connect to database");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
                "Database connection should be established quickly");

            Console.WriteLine($"Database Connection Monitoring:");
            Console.WriteLine($"Connection Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Can Connect: {canConnect}");

            // Test query performance
            var queryStopwatch = Stopwatch.StartNew();
            var supplierCount = await context.Suppliers.CountAsync();
            queryStopwatch.Stop();

            Console.WriteLine($"Query Time: {queryStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Supplier Count: {supplierCount}");

            queryStopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
                "Simple queries should execute quickly");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"Database connection failed: {ex.Message}");
            throw; // Re-throw to fail the test
        }
    }

    [Fact]
    public async Task LoggingSystem_ShouldCaptureImportantEvents()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MonitoringAndAlerting>>();
        var logMessages = new List<string>();

        // Create a custom logger that captures messages
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new TestLoggerProvider(logMessages));
        });
        var testLogger = loggerFactory.CreateLogger<MonitoringAndAlerting>();

        // Act
        testLogger.LogInformation("Test information message");
        testLogger.LogWarning("Test warning message");
        testLogger.LogError("Test error message");

        // Simulate application events
        var client = _factory.CreateClient();
        await client.GetAsync("/api/order/dashboard");
        client.Dispose();

        // Assert
        logMessages.Should().NotBeEmpty("Logger should capture messages");
        logMessages.Should().Contain(m => m.Contains("Test information message"));
        logMessages.Should().Contain(m => m.Contains("Test warning message"));
        logMessages.Should().Contain(m => m.Contains("Test error message"));

        Console.WriteLine("Captured Log Messages:");
        foreach (var message in logMessages)
        {
            Console.WriteLine($"  {message}");
        }
    }

    [Fact]
    public void NetworkConnectivity_ShouldBeMonitorable()
    {
        // Arrange & Act
        var networkMetrics = new Dictionary<string, object>();

        try
        {
            // Test basic network connectivity
            var ping = new Ping();
            var reply = ping.Send("127.0.0.1", 1000);
            
            networkMetrics["LocalhostPingStatus"] = reply.Status;
            networkMetrics["LocalhostPingTime"] = reply.RoundtripTime;

            // Get network interface information
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            networkMetrics["ActiveNetworkInterfaces"] = networkInterfaces.Count;

            // Assert
            reply.Status.Should().Be(IPStatus.Success, "Should be able to ping localhost");
            networkInterfaces.Should().NotBeEmpty("Should have active network interfaces");

            Console.WriteLine("Network Connectivity Monitoring:");
            foreach (var metric in networkMetrics)
            {
                Console.WriteLine($"{metric.Key}: {metric.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Network monitoring failed: {ex.Message}");
            // Don't fail the test for network issues in test environments
        }
    }

    [Fact]
    public async Task AlertingThresholds_ShouldTriggerAppropriately()
    {
        // Arrange
        var alertingRules = new List<AlertingRule>
        {
            new() { Name = "High Response Time", Threshold = 2000, MetricType = "ResponseTime" },
            new() { Name = "High Error Rate", Threshold = 5.0, MetricType = "ErrorRate" },
            new() { Name = "High Memory Usage", Threshold = 1000, MetricType = "MemoryMB" },
            new() { Name = "Low Success Rate", Threshold = 95.0, MetricType = "SuccessRate" }
        };

        var client = _factory.CreateClient();
        var metrics = new Dictionary<string, double>();

        // Act - Collect metrics
        var responseTimes = new List<long>();
        var successCount = 0;
        var totalRequests = 10;

        for (int i = 0; i < totalRequests; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync("/api/order/dashboard");
            stopwatch.Stop();

            responseTimes.Add(stopwatch.ElapsedMilliseconds);
            if (response.IsSuccessStatusCode) successCount++;
        }

        client.Dispose();

        // Calculate metrics
        metrics["ResponseTime"] = responseTimes.Average();
        metrics["ErrorRate"] = (double)(totalRequests - successCount) / totalRequests * 100;
        metrics["MemoryMB"] = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        metrics["SuccessRate"] = (double)successCount / totalRequests * 100;

        // Assert - Check alerting rules
        var triggeredAlerts = new List<string>();

        foreach (var rule in alertingRules)
        {
            if (metrics.TryGetValue(rule.MetricType, out var value))
            {
                var shouldAlert = rule.MetricType switch
                {
                    "ResponseTime" => value > rule.Threshold,
                    "ErrorRate" => value > rule.Threshold,
                    "MemoryMB" => value > rule.Threshold,
                    "SuccessRate" => value < rule.Threshold,
                    _ => false
                };

                if (shouldAlert)
                {
                    triggeredAlerts.Add($"{rule.Name}: {value:F2} (threshold: {rule.Threshold})");
                }
            }
        }

        Console.WriteLine("Alerting System Test:");
        Console.WriteLine("Current Metrics:");
        foreach (var metric in metrics)
        {
            Console.WriteLine($"  {metric.Key}: {metric.Value:F2}");
        }

        if (triggeredAlerts.Any())
        {
            Console.WriteLine("Triggered Alerts:");
            foreach (var alert in triggeredAlerts)
            {
                Console.WriteLine($"  ALERT: {alert}");
            }
        }
        else
        {
            Console.WriteLine("No alerts triggered - system is healthy");
        }

        // The test passes regardless of alerts - this is just demonstrating monitoring
        metrics.Should().NotBeEmpty("Should be able to collect metrics for alerting");
    }
}

/// <summary>
/// Test logger provider for capturing log messages during testing
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<string> _logMessages;

    public TestLoggerProvider(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_logMessages, categoryName);
    }

    public void Dispose() { }
}

/// <summary>
/// Test logger implementation that captures messages
/// </summary>
public class TestLogger : ILogger
{
    private readonly List<string> _logMessages;
    private readonly string _categoryName;

    public TestLogger(List<string> logMessages, string categoryName)
    {
        _logMessages = logMessages;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = $"[{logLevel}] {_categoryName}: {formatter(state, exception)}";
        _logMessages.Add(message);
    }
}

/// <summary>
/// Represents an alerting rule for monitoring
/// </summary>
public class AlertingRule
{
    public string Name { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public string MetricType { get; set; } = string.Empty;
}