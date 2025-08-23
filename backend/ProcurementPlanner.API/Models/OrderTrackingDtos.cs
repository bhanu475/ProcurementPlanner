using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.API.Models;

public class OrderTrackingDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public OrderStatus CurrentStatus { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastStatusUpdate { get; set; }
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    public List<OrderMilestoneDto> Milestones { get; set; } = new();
    public bool IsAtRisk { get; set; }
    public string? RiskReason { get; set; }
    public int DaysUntilDelivery { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
    public string? ChangedByUser { get; set; }
}

public class OrderMilestoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public MilestoneStatus Status { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilTarget { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Specifications { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CustomerOrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsAtRisk { get; set; }
}

public class OrderTrackingFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public OrderStatus? Status { get; set; }
    public ProductType? ProductType { get; set; }
    public bool? IsAtRisk { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CustomerNotificationPreferencesDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool StatusChangeNotifications { get; set; } = true;
    public bool DeliveryReminders { get; set; } = true;
    public bool DelayNotifications { get; set; } = true;
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateNotificationPreferencesRequest
{
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool StatusChangeNotifications { get; set; } = true;
    public bool DeliveryReminders { get; set; } = true;
    public bool DelayNotifications { get; set; } = true;
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CustomerOrderTrackingSummaryDto
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