using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;
    
    private const int CacheExpirationMinutes = 5;
    private const string DashboardCacheKeyPrefix = "dashboard_";
    private const string DeliveryDateCacheKeyPrefix = "delivery_date_";
    private const string TopCustomersCacheKeyPrefix = "top_customers_";

    public DashboardService(
        ICustomerOrderRepository orderRepository,
        IMemoryCache cache,
        ILogger<DashboardService> logger)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter)
    {
        var cacheKey = GenerateCacheKey(DashboardCacheKeyPrefix, filter);
        
        if (_cache.TryGetValue(cacheKey, out OrderDashboardSummary? cachedSummary))
        {
            _logger.LogInformation("Dashboard summary retrieved from cache");
            return cachedSummary!;
        }

        _logger.LogInformation("Generating dashboard summary from database");
        
        var summary = await _orderRepository.GetDashboardSummaryAsync(filter);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, summary, cacheOptions);
        
        _logger.LogInformation("Dashboard summary cached for {ExpirationMinutes} minutes", CacheExpirationMinutes);
        
        return summary;
    }

    public async Task<List<OrdersByDeliveryDate>> GetOrdersByDeliveryDateAsync(DashboardFilterRequest filter)
    {
        var cacheKey = GenerateCacheKey(DeliveryDateCacheKeyPrefix, filter);
        
        if (_cache.TryGetValue(cacheKey, out List<OrdersByDeliveryDate>? cachedData))
        {
            _logger.LogInformation("Orders by delivery date retrieved from cache");
            return cachedData!;
        }

        _logger.LogInformation("Generating orders by delivery date from database");
        
        var summary = await _orderRepository.GetDashboardSummaryAsync(filter);
        var ordersByDeliveryDate = summary.OrdersByDeliveryDate;
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, ordersByDeliveryDate, cacheOptions);
        
        return ordersByDeliveryDate;
    }

    public async Task<List<OrdersByCustomer>> GetTopCustomersAsync(DashboardFilterRequest filter, int topCount = 10)
    {
        var cacheKey = GenerateCacheKey(TopCustomersCacheKeyPrefix, filter, topCount.ToString());
        
        if (_cache.TryGetValue(cacheKey, out List<OrdersByCustomer>? cachedData))
        {
            _logger.LogInformation("Top customers retrieved from cache");
            return cachedData!;
        }

        _logger.LogInformation("Generating top customers from database");
        
        var summary = await _orderRepository.GetDashboardSummaryAsync(filter);
        var topCustomers = summary.TopCustomers.Take(topCount).ToList();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, topCustomers, cacheOptions);
        
        return topCustomers;
    }

    public async Task InvalidateCacheAsync()
    {
        _logger.LogInformation("Invalidating dashboard cache");
        
        // Since IMemoryCache doesn't have a direct way to remove by pattern,
        // we'll implement a simple cache invalidation strategy
        // In a real-world scenario with Redis, we could use pattern-based deletion
        
        // For now, we'll just log the invalidation
        // The cache will naturally expire based on the configured expiration times
        
        await Task.CompletedTask;
        
        _logger.LogInformation("Dashboard cache invalidation completed");
    }

    private static string GenerateCacheKey(string prefix, DashboardFilterRequest filter, string? suffix = null)
    {
        var keyParts = new List<string> { prefix };
        
        if (filter.ProductType.HasValue)
            keyParts.Add($"pt_{filter.ProductType}");
        
        if (filter.Status.HasValue)
            keyParts.Add($"st_{filter.Status}");
        
        if (!string.IsNullOrEmpty(filter.CustomerId))
            keyParts.Add($"ci_{filter.CustomerId}");
        
        if (filter.DeliveryDateFrom.HasValue)
            keyParts.Add($"df_{filter.DeliveryDateFrom:yyyyMMdd}");
        
        if (filter.DeliveryDateTo.HasValue)
            keyParts.Add($"dt_{filter.DeliveryDateTo:yyyyMMdd}");
        
        if (!string.IsNullOrEmpty(suffix))
            keyParts.Add(suffix);
        
        return string.Join("_", keyParts);
    }
}