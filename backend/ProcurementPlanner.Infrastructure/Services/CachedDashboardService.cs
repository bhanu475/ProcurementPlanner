using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class CachedDashboardService : IDashboardService
{
    private readonly IDashboardService _dashboardService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedDashboardService> _logger;

    public CachedDashboardService(
        IDashboardService dashboardService,
        ICacheService cacheService,
        ILogger<CachedDashboardService> logger)
    {
        _dashboardService = dashboardService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter)
    {
        var filterHash = GenerateFilterHash(filter);
        var cacheKey = $"{CacheKeys.DashboardSummary}:{filterHash}";
        
        var cachedSummary = await _cacheService.GetAsync<OrderDashboardSummary>(cacheKey);
        if (cachedSummary != null)
        {
            _logger.LogDebug("Dashboard summary retrieved from cache");
            return cachedSummary;
        }

        _logger.LogDebug("Dashboard summary not found in cache, fetching from service");
        var summary = await _dashboardService.GetDashboardSummaryAsync(filter);
        
        await _cacheService.SetAsync(cacheKey, summary, CacheKeys.Expiration.Medium);
        _logger.LogDebug("Dashboard summary cached for {Expiration} minutes", CacheKeys.Expiration.Medium.TotalMinutes);
        
        return summary;
    }

    public async Task<List<OrdersByDeliveryDate>> GetOrdersByDeliveryDateAsync(DashboardFilterRequest filter)
    {
        var filterHash = GenerateFilterHash(filter);
        var cacheKey = $"{CacheKeys.DashboardOrdersByDeliveryDate}:{filterHash}";
        
        var cachedOrders = await _cacheService.GetAsync<List<OrdersByDeliveryDate>>(cacheKey);
        if (cachedOrders != null)
        {
            _logger.LogDebug("Orders by delivery date retrieved from cache");
            return cachedOrders;
        }

        _logger.LogDebug("Orders by delivery date not found in cache, fetching from service");
        var orders = await _dashboardService.GetOrdersByDeliveryDateAsync(filter);
        
        await _cacheService.SetAsync(cacheKey, orders, CacheKeys.Expiration.Medium);
        _logger.LogDebug("Orders by delivery date cached for {Expiration} minutes", CacheKeys.Expiration.Medium.TotalMinutes);
        
        return orders;
    }

    public async Task<List<OrdersByCustomer>> GetTopCustomersAsync(DashboardFilterRequest filter, int topCount = 10)
    {
        var filterHash = GenerateFilterHash(filter);
        var cacheKey = $"dashboard:top-customers:{filterHash}:{topCount}";
        
        var cachedCustomers = await _cacheService.GetAsync<List<OrdersByCustomer>>(cacheKey);
        if (cachedCustomers != null)
        {
            _logger.LogDebug("Top customers retrieved from cache");
            return cachedCustomers;
        }

        _logger.LogDebug("Top customers not found in cache, fetching from service");
        var customers = await _dashboardService.GetTopCustomersAsync(filter, topCount);
        
        await _cacheService.SetAsync(cacheKey, customers, CacheKeys.Expiration.Medium);
        _logger.LogDebug("Top customers cached for {Expiration} minutes", CacheKeys.Expiration.Medium.TotalMinutes);
        
        return customers;
    }

    public async Task InvalidateCacheAsync()
    {
        _logger.LogInformation("Invalidating dashboard cache");
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllDashboard);
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllOrders);
    }

    private static string GenerateFilterHash(DashboardFilterRequest filter)
    {
        // Simple hash generation for cache key - in production, consider using a more robust approach
        var hashString = $"{filter.ProductType}_{filter.DeliveryDateFrom:yyyyMMdd}_{filter.DeliveryDateTo:yyyyMMdd}_{filter.CustomerId}_{filter.Status}";
        return hashString.GetHashCode().ToString();
    }
}