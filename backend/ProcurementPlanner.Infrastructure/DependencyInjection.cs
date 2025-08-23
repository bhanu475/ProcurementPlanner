using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Repositories;
using ProcurementPlanner.Infrastructure.Services;

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

        // Add health checks (database check will be added in future tasks)
        services.AddHealthChecks();

        // Add in-memory caching for dashboard (Redis will be configured in future tasks)
        services.AddMemoryCache();

        // Add Redis cache (will be configured in future tasks)
        // services.AddStackExchangeRedisCache(options =>
        // {
        //     options.Configuration = configuration.GetConnectionString("Redis");
        // });

        return services;
    }
}