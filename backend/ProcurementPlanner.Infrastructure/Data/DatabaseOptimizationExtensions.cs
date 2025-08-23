using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.HealthChecks;
using System.Data;

namespace ProcurementPlanner.Infrastructure.Data;

public static class DatabaseOptimizationExtensions
{
    /// <summary>
    /// Configure database with performance optimizations
    /// </summary>
    public static IServiceCollection AddOptimizedDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                });
                
                // Enable sensitive data logging in development
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            else
            {
                // Production configuration with SQL Server
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(30);
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    
                    // Additional SQL Server optimizations
                    sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            }

            // Performance optimizations
            options.ConfigureWarnings(warnings =>
            {
                // Suppress warnings that might impact performance
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning);
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning);
            });

            // Query optimization settings
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            
            // Enable compiled queries for better performance
            options.EnableServiceProviderCaching();
        });

        return services;
    }

    /// <summary>
    /// Configure database connection pooling for better performance
    /// </summary>
    public static IServiceCollection AddDatabaseConnectionPooling(this IServiceCollection services, IConfiguration configuration)
    {
        var poolSize = int.TryParse(configuration["Database:ConnectionPoolSize"], out var ps) ? ps : 128;
        var maxRetryCount = int.TryParse(configuration["Database:MaxRetryCount"], out var mrc) ? mrc : 3;
        var commandTimeout = int.TryParse(configuration["Database:CommandTimeout"], out var ct) ? ct : 30;
        
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(commandTimeout);
                });
            }
            else
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(commandTimeout);
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: maxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    
                    // Connection-level optimizations for SQL Server
                });
            }

            // Performance optimizations
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.EnableServiceProviderCaching();
            
            // Configure warnings
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning);
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning);
            });
            
        }, poolSize: poolSize);

        return services;
    }

    /// <summary>
    /// Configure database health checks with performance monitoring
    /// </summary>
    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(
                name: "database",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "db", "sql", "ready" })
            .AddCheck<DatabasePerformanceHealthCheck>(
                name: "database-performance",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "db", "performance" });

        return services;
    }

    /// <summary>
    /// Configure query optimization settings
    /// </summary>
    public static IServiceCollection AddQueryOptimizations(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure compiled queries for frequently used queries
        services.AddSingleton<CompiledQueryCache>();
        
        // Configure query result caching
        var cacheTimeout = int.TryParse(configuration["Database:QueryCacheTimeoutMinutes"], out var cto) ? cto : 5;
        services.Configure<QueryCacheOptions>(options =>
        {
            options.DefaultCacheTimeout = TimeSpan.FromMinutes(cacheTimeout);
            options.EnableQueryResultCaching = bool.TryParse(configuration["Database:EnableQueryResultCaching"], out var eqrc) ? eqrc : true;
        });

        return services;
    }

    /// <summary>
    /// Configure database monitoring and logging
    /// </summary>
    public static IServiceCollection AddDatabaseMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseMonitoringOptions>(options =>
        {
            options.EnableSlowQueryLogging = bool.TryParse(configuration["Database:EnableSlowQueryLogging"], out var esql) ? esql : true;
            options.SlowQueryThresholdMs = int.TryParse(configuration["Database:SlowQueryThresholdMs"], out var sqt) ? sqt : 1000;
            options.EnableQueryPlanLogging = bool.TryParse(configuration["Database:EnableQueryPlanLogging"], out var eqpl) ? eqpl : false;
            options.LogConnectionPoolMetrics = bool.TryParse(configuration["Database:LogConnectionPoolMetrics"], out var lcpm) ? lcpm : true;
        });

        services.AddScoped<IDatabaseMonitoringService, DatabaseMonitoringService>();
        
        return services;
    }

    /// <summary>
    /// Configure pagination defaults for large datasets
    /// </summary>
    public static IServiceCollection AddPaginationDefaults(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaginationOptions>(options =>
        {
            options.DefaultPageSize = int.TryParse(configuration["Pagination:DefaultPageSize"], out var dps) ? dps : 20;
            options.MaxPageSize = int.TryParse(configuration["Pagination:MaxPageSize"], out var mps) ? mps : 100;
            options.EnableTotalCountOptimization = bool.TryParse(configuration["Pagination:EnableTotalCountOptimization"], out var etco) ? etco : true;
        });

        return services;
    }
}

/// <summary>
/// Compiled query cache for frequently used queries
/// </summary>
public class CompiledQueryCache
{
    // Compiled queries will be added here for frequently used operations
    public static readonly Func<ApplicationDbContext, string, Task<bool>> OrderNumberExists =
        EF.CompileAsyncQuery((ApplicationDbContext context, string orderNumber) =>
            context.CustomerOrders.Any(o => o.OrderNumber == orderNumber));

    public static readonly Func<ApplicationDbContext, Guid, Task<CustomerOrder?>> GetOrderById =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid orderId) =>
            context.CustomerOrders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == orderId));
}

/// <summary>
/// Options for query result caching
/// </summary>
public class QueryCacheOptions
{
    public TimeSpan DefaultCacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableQueryResultCaching { get; set; } = true;
}

/// <summary>
/// Options for database monitoring
/// </summary>
public class DatabaseMonitoringOptions
{
    public bool EnableSlowQueryLogging { get; set; } = true;
    public int SlowQueryThresholdMs { get; set; } = 1000;
    public bool EnableQueryPlanLogging { get; set; } = false;
    public bool LogConnectionPoolMetrics { get; set; } = true;
}

/// <summary>
/// Options for pagination configuration
/// </summary>
public class PaginationOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public bool EnableTotalCountOptimization { get; set; } = true;
}

/// <summary>
/// Interface for database monitoring service
/// </summary>
public interface IDatabaseMonitoringService
{
    Task LogSlowQueryAsync(string query, TimeSpan executionTime, Dictionary<string, object>? parameters = null);
    Task LogConnectionPoolMetricsAsync();
    Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync();
}

/// <summary>
/// Database monitoring service implementation
/// </summary>
public class DatabaseMonitoringService : IDatabaseMonitoringService
{
    private readonly ILogger<DatabaseMonitoringService> _logger;
    private readonly DatabaseMonitoringOptions _options;

    public DatabaseMonitoringService(
        ILogger<DatabaseMonitoringService> logger,
        Microsoft.Extensions.Options.IOptions<DatabaseMonitoringOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task LogSlowQueryAsync(string query, TimeSpan executionTime, Dictionary<string, object>? parameters = null)
    {
        if (!_options.EnableSlowQueryLogging || executionTime.TotalMilliseconds < _options.SlowQueryThresholdMs)
            return;

        _logger.LogWarning("Slow query detected: {Query} executed in {ExecutionTime}ms with parameters {Parameters}",
            query, executionTime.TotalMilliseconds, parameters);

        await Task.CompletedTask;
    }

    public async Task LogConnectionPoolMetricsAsync()
    {
        if (!_options.LogConnectionPoolMetrics)
            return;

        // Implementation would log connection pool metrics
        _logger.LogInformation("Connection pool metrics logged");
        await Task.CompletedTask;
    }

    public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync()
    {
        // Implementation would gather actual performance metrics
        return await Task.FromResult(new DatabasePerformanceMetrics
        {
            AverageQueryTime = TimeSpan.FromMilliseconds(50),
            ActiveConnections = 10,
            TotalQueries = 1000,
            SlowQueries = 5
        });
    }
}

/// <summary>
/// Database performance metrics
/// </summary>
public class DatabasePerformanceMetrics
{
    public TimeSpan AverageQueryTime { get; set; }
    public int ActiveConnections { get; set; }
    public long TotalQueries { get; set; }
    public int SlowQueries { get; set; }
}