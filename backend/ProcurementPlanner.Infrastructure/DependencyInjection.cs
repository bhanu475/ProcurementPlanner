using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Repositories;

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

        // Add authentication service
        services.AddScoped<IAuthenticationService, Services.AuthenticationService>();

        // Add health checks (database check will be added in future tasks)
        services.AddHealthChecks();

        // Add Redis cache (will be configured in future tasks)
        // services.AddStackExchangeRedisCache(options =>
        // {
        //     options.Configuration = configuration.GetConnectionString("Redis");
        // });

        return services;
    }
}