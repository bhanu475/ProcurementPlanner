using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Diagnostics;

namespace ProcurementPlanner.Infrastructure.Repositories;

/// <summary>
/// Optimized version of CustomerOrderRepository with improved query performance
/// </summary>
public class OptimizedCustomerOrderRepository : Repository<CustomerOrder>, ICustomerOrderRepository
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<OptimizedCustomerOrderRepository> _logger;
    private readonly PaginationOptions _paginationOptions;
    private readonly QueryCacheOptions _cacheOptions;
    private readonly IDatabaseMonitoringService _monitoringService;

    public OptimizedCustomerOrderRepository(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<OptimizedCustomerOrderRepository> logger,
        IOptions<PaginationOptions> paginationOptions,
        IOptions<QueryCacheOptions> cacheOptions,
        IDatabaseMonitoringService monitoringService) : base(context)
    {
        _cache = cache;
        _logger = logger;
        _paginationOptions = paginationOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _monitoringService = monitoringService;
    }

    public override async Task<CustomerOrder?> GetByIdAsync(Guid id)
    {
        // Use compiled query for better performance
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await CompiledQueryCache.GetOrderById(_context, id);
            return result;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100) // Log if query takes more than 100ms
            {
                await _monitoringService.LogSlowQueryAsync(
                    "GetOrderById", 
                    stopwatch.Elapsed, 
                    new Dictionary<string, object> { { "orderId", id } });
            }
        }
    }

    public async Task<CustomerOrder?> GetOrderWithItemsAsync(Guid orderId)
    {
        return await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<PagedResult<CustomerOrder>> GetOrdersAsync(OrderFilterRequest filter)
    {
        var query = _dbSet.AsNoTracking(); // Use AsNoTracking for read-only operations

        // Apply filters in order of selectivity (most selective first)
        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.ProductType.HasValue)
        {
            query = query.Where(o => o.ProductType == filter.ProductType.Value);
        }

        if (filter.DeliveryDateFrom.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate >= filter.DeliveryDateFrom.Value);
        }

        if (filter.DeliveryDateTo.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate <= filter.DeliveryDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.CustomerId))
        {
            // Use exact match instead of Contains for better performance if possible
            if (filter.CustomerId.Contains("*") || filter.CustomerId.Contains("%"))
            {
                var searchTerm = filter.CustomerId.Replace("*", "%");
                query = query.Where(o => EF.Functions.Like(o.CustomerId, searchTerm));
            }
            else
            {
                query = query.Where(o => o.CustomerId == filter.CustomerId);
            }
        }

        if (!string.IsNullOrEmpty(filter.CustomerName))
        {
            // Use EF.Functions.Like for better performance than Contains
            query = query.Where(o => EF.Functions.Like(o.CustomerName, $"%{filter.CustomerName}%"));
        }

        if (filter.CreatedDateFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.CreatedDateFrom.Value);
        }

        if (filter.CreatedDateTo.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.CreatedDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.OrderNumber))
        {
            // Use exact match for order number if no wildcards
            if (filter.OrderNumber.Contains("*") || filter.OrderNumber.Contains("%"))
            {
                var searchTerm = filter.OrderNumber.Replace("*", "%");
                query = query.Where(o => EF.Functions.Like(o.OrderNumber, searchTerm));
            }
            else
            {
                query = query.Where(o => o.OrderNumber == filter.OrderNumber);
            }
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(o => o.RequestedDeliveryDate < today && 
                               o.Status != OrderStatus.Delivered && 
                               o.Status != OrderStatus.Cancelled);
        }

        // Apply sorting with optimized field selection
        query = filter.SortBy?.ToLower() switch
        {
            "ordernumber" => filter.SortDescending 
                ? query.OrderByDescending(o => o.OrderNumber)
                : query.OrderBy(o => o.OrderNumber),
            "customername" => filter.SortDescending
                ? query.OrderByDescending(o => o.CustomerName)
                : query.OrderBy(o => o.CustomerName),
            "deliverydate" => filter.SortDescending
                ? query.OrderByDescending(o => o.RequestedDeliveryDate)
                : query.OrderBy(o => o.RequestedDeliveryDate),
            "status" => filter.SortDescending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => filter.SortDescending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };

        // Use optimized pagination with caching
        var cacheKey = GenerateCacheKey(filter);
        return await GetPagedResultOptimizedAsync(query, filter, cacheKey);
    }

    public async Task<List<CustomerOrder>> GetOrdersByDeliveryDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.RequestedDeliveryDate >= startDate && o.RequestedDeliveryDate <= endDate)
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();
    }

    public async Task<List<CustomerOrder>> GetOverdueOrdersAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.RequestedDeliveryDate < today && 
                       o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();
    }

    public async Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter)
    {
        // For complex aggregations, load data into memory first to avoid translation issues
        IQueryable<CustomerOrder> query = _dbSet.AsNoTracking().Include(o => o.Items);

        // Apply filters
        if (filter.ProductType.HasValue)
        {
            query = query.Where(o => o.ProductType == filter.ProductType.Value);
        }

        if (filter.DeliveryDateFrom.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate >= filter.DeliveryDateFrom.Value);
        }

        if (filter.DeliveryDateTo.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate <= filter.DeliveryDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.CustomerId))
        {
            query = query.Where(o => o.CustomerId == filter.CustomerId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        // Load filtered data into memory for complex aggregations
        var orders = await query.ToListAsync();
        var today = DateTime.UtcNow.Date;

        var summary = new OrderDashboardSummary
        {
            TotalOrders = orders.Count,
            StatusCounts = orders.GroupBy(o => o.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            ProductTypeCounts = orders.GroupBy(o => o.ProductType)
                .ToDictionary(g => g.Key, g => g.Count()),
            OverdueOrders = orders.Count(o => o.RequestedDeliveryDate < today && 
                                            o.Status != OrderStatus.Delivered && 
                                            o.Status != OrderStatus.Cancelled),
            OrdersByDeliveryDate = orders
                .GroupBy(o => o.RequestedDeliveryDate.Date)
                .Select(g => new OrdersByDeliveryDate
                {
                    DeliveryDate = g.Key,
                    OrderCount = g.Count(),
                    TotalQuantity = g.Sum(o => o.TotalQuantity)
                })
                .OrderBy(x => x.DeliveryDate)
                .ToList(),
            TopCustomers = orders
                .GroupBy(o => new { o.CustomerId, o.CustomerName })
                .Select(g => new OrdersByCustomer
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    OrderCount = g.Count(),
                    TotalQuantity = g.Sum(o => o.TotalQuantity),
                    TotalValue = g.SelectMany(o => o.Items).Sum(i => i.TotalPrice)
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(10)
                .ToList(),
            TotalValue = orders.SelectMany(o => o.Items).Sum(i => i.TotalPrice)
        };

        return summary;
    }

    public async Task<bool> OrderNumberExistsAsync(string orderNumber)
    {
        // Use compiled query for better performance
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await CompiledQueryCache.OrderNumberExists(_context, orderNumber);
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 50)
            {
                await _monitoringService.LogSlowQueryAsync(
                    "OrderNumberExists", 
                    stopwatch.Elapsed, 
                    new Dictionary<string, object> { { "orderNumber", orderNumber } });
            }
        }
    }

    public async Task<List<CustomerOrder>> GetOrdersByCustomerIdAsync(string customerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get orders with minimal data for dashboard performance
    /// </summary>
    public async Task<List<CustomerOrder>> GetOrdersSummaryAsync(DashboardFilterRequest filter)
    {
        var query = _dbSet
            .AsNoTracking()
            .Select(o => new CustomerOrder
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName,
                ProductType = o.ProductType,
                Status = o.Status,
                RequestedDeliveryDate = o.RequestedDeliveryDate,
                CreatedAt = o.CreatedAt
            });

        // Apply filters
        if (filter.ProductType.HasValue)
        {
            query = query.Where(o => o.ProductType == filter.ProductType.Value);
        }

        if (filter.DeliveryDateFrom.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate >= filter.DeliveryDateFrom.Value);
        }

        if (filter.DeliveryDateTo.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate <= filter.DeliveryDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.CustomerId))
        {
            query = query.Where(o => o.CustomerId == filter.CustomerId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Optimized pagination with total count caching
    /// </summary>
    private async Task<PagedResult<CustomerOrder>> GetPagedResultOptimizedAsync(
        IQueryable<CustomerOrder> query, 
        OrderFilterRequest filter,
        string cacheKey)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Validate and adjust page size
            var pageSize = Math.Min(filter.PageSize, _paginationOptions.MaxPageSize);
            var page = Math.Max(1, filter.Page);

            // Try to get total count from cache if enabled
            int totalCount;
            if (_cacheOptions.EnableQueryResultCaching && _paginationOptions.EnableTotalCountOptimization)
            {
                var countCacheKey = $"{cacheKey}_count";
                if (!_cache.TryGetValue(countCacheKey, out totalCount))
                {
                    totalCount = await query.CountAsync();
                    _cache.Set(countCacheKey, totalCount, TimeSpan.FromMinutes(2)); // Cache count for 2 minutes
                }
            }
            else
            {
                totalCount = await query.CountAsync();
            }

            // Get paginated results with items included
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Items)
                .ToListAsync();

            return new PagedResult<CustomerOrder>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                await _monitoringService.LogSlowQueryAsync(
                    "GetPagedResultOptimized", 
                    stopwatch.Elapsed, 
                    new Dictionary<string, object> 
                    { 
                        { "cacheKey", cacheKey },
                        { "page", filter.Page },
                        { "pageSize", filter.PageSize }
                    });
            }
        }
    }

    /// <summary>
    /// Generate cache key for query results
    /// </summary>
    private static string GenerateCacheKey(OrderFilterRequest filter)
    {
        var keyParts = new List<string>
        {
            "orders",
            filter.Status?.ToString() ?? "all",
            filter.ProductType?.ToString() ?? "all",
            filter.CustomerId ?? "all",
            filter.DeliveryDateFrom?.ToString("yyyyMMdd") ?? "all",
            filter.DeliveryDateTo?.ToString("yyyyMMdd") ?? "all",
            filter.IsOverdue?.ToString() ?? "all",
            filter.SortBy ?? "default",
            filter.SortDescending.ToString()
        };

        return string.Join("_", keyParts);
    }
}