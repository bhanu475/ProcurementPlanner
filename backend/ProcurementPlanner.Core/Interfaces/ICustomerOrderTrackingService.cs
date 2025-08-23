using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface ICustomerOrderTrackingService
{
    Task<CustomerOrder?> GetCustomerOrderAsync(Guid orderId, string customerId);
    Task<List<CustomerOrder>> GetCustomerOrdersAsync(string customerId, OrderTrackingFilter filter);
    Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId, string customerId);
    Task<List<OrderMilestone>> GetOrderMilestonesAsync(Guid orderId, string customerId);
    Task<CustomerNotificationPreferences?> GetNotificationPreferencesAsync(string customerId);
    Task UpdateNotificationPreferencesAsync(string customerId, UpdateCustomerNotificationPreferencesRequest request);
    Task<bool> ValidateCustomerAccessAsync(Guid orderId, string customerId);
    Task<List<CustomerOrder>> GetRecentOrdersAsync(string customerId, int count = 5);
    Task<OrderTrackingSummary> GetOrderTrackingSummaryAsync(string customerId);
}

public class OrderTrackingFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public OrderStatus? Status { get; set; }
    public ProductType? ProductType { get; set; }
    public bool? IsAtRisk { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CustomerNotificationPreferences
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool StatusChangeNotifications { get; set; } = true;
    public bool DeliveryReminders { get; set; } = true;
    public bool DelayNotifications { get; set; } = true;
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateCustomerNotificationPreferencesRequest
{
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool StatusChangeNotifications { get; set; } = true;
    public bool DeliveryReminders { get; set; } = true;
    public bool DelayNotifications { get; set; } = true;
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
}

public class OrderTrackingSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int AtRiskOrders { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime? NextDeliveryDate { get; set; }
}