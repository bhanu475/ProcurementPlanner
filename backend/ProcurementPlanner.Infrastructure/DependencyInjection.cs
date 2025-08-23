using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Repositories;
using ProcurementPlanner.Infrastructure.Services;
using StackExchange.Redis;

namespace ProcurementPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
            
            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();

        // Add services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IOrderManagementService, OrderManagementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISupplierManagementService, SupplierManagementService>();
        services.AddScoped<IDistributionAlgorithmService, DistributionAlgorithmService>();
        services.AddScoped<IProcurementPlanningService, ProcurementPlanningService>();
        services.AddScoped<ISupplierOrderConfirmationService, SupplierOrderConfirmationService>();
        services.AddScoped<IOrderStatusTrackingService, OrderStatusTrackingService>();
        services.AddScoped<ICustomerOrderTrackingService, CustomerOrderTrackingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportingService, ReportingService>();

        // Add health checks (database check will be added in future tasks)
        services.AddHealthChecks();

        // Add in-memory caching as fallback
        services.AddMemoryCache();

        // Add Redis cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "ProcurementPlanner";
        });

        // Add Redis connection multiplexer for direct Redis operations
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Add caching services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ISessionService, RedisSessionService>();

        // Decorate services with caching
        services.Decorate<IDashboardService, CachedDashboardService>();
        services.Decorate<ISupplierManagementService, CachedSupplierManagementService>();

        return services;
    }
}