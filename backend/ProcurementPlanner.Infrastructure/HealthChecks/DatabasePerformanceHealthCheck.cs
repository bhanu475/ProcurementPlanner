using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Infrastructure.Data;
using System.Diagnostics;

namespace ProcurementPlanner.Infrastructure.HealthChecks;

public class DatabasePerformanceHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabasePerformanceHealthCheck> _logger;

    public DatabasePerformanceHealthCheck(ApplicationDbContext context, ILogger<DatabasePerformanceHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Test basic connectivity
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Test a simple query performance
            var orderCount = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(cancellationToken);
            
            stopwatch.Stop();
            
            var responseTime = stopwatch.ElapsedMilliseconds;
            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = responseTime,
                ["order_count"] = orderCount,
                ["connection_state"] = _context.Database.GetConnectionString()?.Length > 0 ? "configured" : "not_configured"
            };

            // Define performance thresholds
            if (responseTime > 5000) // 5 seconds
            {
                _logger.LogWarning("Database performance is degraded. Response time: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded($"Database response time is slow: {responseTime}ms", data: data);
            }
            
            if (responseTime > 10000) // 10 seconds
            {
                _logger.LogError("Database performance is unhealthy. Response time: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Unhealthy($"Database response time is too slow: {responseTime}ms", data: data);
            }

            _logger.LogDebug("Database performance check passed. Response time: {ResponseTime}ms", responseTime);
            return HealthCheckResult.Healthy($"Database is performing well. Response time: {responseTime}ms", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database performance health check failed");
            return HealthCheckResult.Unhealthy("Database performance check failed", ex);
        }
    }
}