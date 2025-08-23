using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Services;

public class CustomerOrderTrackingService : ICustomerOrderTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerOrderTrackingService> _logger;

    public CustomerOrderTrackingService(
        ApplicationDbContext context,
        ILogger<CustomerOrderTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerOrder?> GetCustomerOrderAsync(Guid orderId, string customerId)
    {
        _logger.LogInformation("Retrieving order {OrderId} for customer {CustomerId}", orderId, customerId);

        return await _context.CustomerOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
    }

    public async Task<List<CustomerOrder>> GetCustomerOrdersAsync(string customerId, OrderTrackingFilter filter)
    {
        _logger.LogInformation("Retrieving orders for customer {CustomerId}", customerId);

        var query = _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId);

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.EndDate.Value);

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        if (filter.ProductType.HasValue)
            query = query.Where(o => o.ProductType == filter.ProductType.Value);

        if (filter.IsAtRisk.HasValue && filter.IsAtRisk.Value)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(o => o.Status != OrderStatus.Delivered && 
                               o.Status != OrderStatus.Cancelled && 
                               o.RequestedDeliveryDate < today);
        }

        // Apply pagination
        var skip = (filter.Page - 1) * filter.PageSize;
        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId, string customerId)
    {
        _logger.LogInformation("Retrieving status history for order {OrderId} and customer {CustomerId}", orderId, customerId);

        // First verify the customer has access to this order
        var hasAccess = await ValidateCustomerAccessAsync(orderId, customerId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException($"Customer {customerId} does not have access to order {orderId}");
        }

        return await _context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .Include(h => h.ChangedByUser)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<List<OrderMilestone>> GetOrderMilestonesAsync(Guid orderId, string customerId)
    {
        _logger.LogInformation("Retrieving milestones for order {OrderId} and customer {CustomerId}", orderId, customerId);

        // First verify the customer has access to this order
        var hasAccess = await ValidateCustomerAccessAsync(orderId, customerId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException($"Customer {customerId} does not have access to order {orderId}");
        }

        return await _context.OrderMilestones
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.TargetDate)
            .ToListAsync();
    }

    public async Task<Core.Interfaces.CustomerNotificationPreferences?> GetNotificationPreferencesAsync(string customerId)
    {
        _logger.LogInformation("Retrieving notification preferences for customer {CustomerId}", customerId);

        var preferences = await _context.CustomerNotificationPreferences
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);

        if (preferences == null)
        {
            return null;
        }

        return new Core.Interfaces.CustomerNotificationPreferences
        {
            Id = preferences.Id,
            CustomerId = preferences.CustomerId,
            CustomerName = preferences.CustomerName,
            EmailNotifications = preferences.EmailNotifications,
            SmsNotifications = preferences.SmsNotifications,
            StatusChangeNotifications = preferences.StatusChangeNotifications,
            DeliveryReminders = preferences.DeliveryReminders,
            DelayNotifications = preferences.DelayNotifications,
            EmailAddress = preferences.EmailAddress,
            PhoneNumber = preferences.PhoneNumber,
            CreatedAt = preferences.CreatedAt,
            UpdatedAt = preferences.UpdatedAt ?? preferences.CreatedAt
        };
    }

    public async Task UpdateNotificationPreferencesAsync(string customerId, UpdateCustomerNotificationPreferencesRequest request)
    {
        _logger.LogInformation("Updating notification preferences for customer {CustomerId}", customerId);

        var preferences = await _context.CustomerNotificationPreferences
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);

        if (preferences == null)
        {
            // Get customer name from an existing order
            var customerOrder = await _context.CustomerOrders
                .FirstOrDefaultAsync(o => o.CustomerId == customerId);

            var customerName = customerOrder?.CustomerName ?? customerId;

            preferences = new Core.Entities.CustomerNotificationPreferences
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CustomerName = customerName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomerNotificationPreferences.Add(preferences);
        }

        // Update preferences
        preferences.EmailNotifications = request.EmailNotifications;
        preferences.SmsNotifications = request.SmsNotifications;
        preferences.StatusChangeNotifications = request.StatusChangeNotifications;
        preferences.DeliveryReminders = request.DeliveryReminders;
        preferences.DelayNotifications = request.DelayNotifications;
        preferences.EmailAddress = request.EmailAddress;
        preferences.PhoneNumber = request.PhoneNumber;
        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification preferences updated for customer {CustomerId}", customerId);
    }

    public async Task<bool> ValidateCustomerAccessAsync(Guid orderId, string customerId)
    {
        return await _context.CustomerOrders
            .AnyAsync(o => o.Id == orderId && o.CustomerId == customerId);
    }

    public async Task<List<CustomerOrder>> GetRecentOrdersAsync(string customerId, int count = 5)
    {
        _logger.LogInformation("Retrieving {Count} recent orders for customer {CustomerId}", count, customerId);

        return await _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<OrderTrackingSummary> GetOrderTrackingSummaryAsync(string customerId)
    {
        _logger.LogInformation("Retrieving order tracking summary for customer {CustomerId}", customerId);

        var orders = await _context.CustomerOrders
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();

        if (!orders.Any())
        {
            return new OrderTrackingSummary
            {
                CustomerId = customerId,
                CustomerName = customerId
            };
        }

        var today = DateTime.UtcNow.Date;
        var activeStatuses = new[] { OrderStatus.Submitted, OrderStatus.UnderReview, OrderStatus.PlanningInProgress, 
                                   OrderStatus.PurchaseOrdersCreated, OrderStatus.AwaitingSupplierConfirmation, 
                                   OrderStatus.InProduction, OrderStatus.ReadyForDelivery };

        var summary = new OrderTrackingSummary
        {
            CustomerId = customerId,
            CustomerName = orders.First().CustomerName,
            TotalOrders = orders.Count,
            ActiveOrders = orders.Count(o => activeStatuses.Contains(o.Status)),
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
            AtRiskOrders = orders.Count(o => activeStatuses.Contains(o.Status) && o.RequestedDeliveryDate < today),
            LastOrderDate = orders.Max(o => o.CreatedAt),
            NextDeliveryDate = orders
                .Where(o => activeStatuses.Contains(o.Status))
                .OrderBy(o => o.RequestedDeliveryDate)
                .FirstOrDefault()?.RequestedDeliveryDate
        };

        return summary;
    }
}