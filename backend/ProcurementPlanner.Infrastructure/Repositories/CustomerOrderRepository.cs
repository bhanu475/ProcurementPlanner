using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Repositories;

public class CustomerOrderRepository : Repository<CustomerOrder>, ICustomerOrderRepository
{
    public CustomerOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<CustomerOrder?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<CustomerOrder?> GetOrderWithItemsAsync(Guid orderId)
    {
        return await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<PagedResult<CustomerOrder>> GetOrdersAsync(OrderFilterRequest filter)
    {
        var query = _dbSet.Include(o => o.Items).AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.CustomerId))
        {
            query = query.Where(o => o.CustomerId.Contains(filter.CustomerId));
        }

        if (!string.IsNullOrEmpty(filter.CustomerName))
        {
            query = query.Where(o => o.CustomerName.Contains(filter.CustomerName));
        }

        if (filter.ProductType.HasValue)
        {
            query = query.Where(o => o.ProductType == filter.ProductType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.DeliveryDateFrom.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate >= filter.DeliveryDateFrom.Value);
        }

        if (filter.DeliveryDateTo.HasValue)
        {
            query = query.Where(o => o.RequestedDeliveryDate <= filter.DeliveryDateTo.Value);
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
            query = query.Where(o => o.OrderNumber.Contains(filter.OrderNumber));
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(o => o.RequestedDeliveryDate < today && 
                               o.Status != OrderStatus.Delivered && 
                               o.Status != OrderStatus.Cancelled);
        }

        // Apply sorting
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

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<CustomerOrder>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<CustomerOrder>> GetOrdersByDeliveryDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.RequestedDeliveryDate >= startDate && o.RequestedDeliveryDate <= endDate)
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();
    }

    public async Task<List<CustomerOrder>> GetOverdueOrdersAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.RequestedDeliveryDate < today && 
                       o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();
    }

    public async Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter)
    {
        var query = _dbSet.Include(o => o.Items).AsQueryable();

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
            query = query.Where(o => o.CustomerId.Contains(filter.CustomerId));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

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
        return await _dbSet.AnyAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<CustomerOrder>> GetOrdersByCustomerIdAsync(string customerId)
    {
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}