using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Models;

public enum MilestoneStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Overdue = 4,
    Cancelled = 5
}

public class OrderTrackingInfo
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus CurrentStatus { get; set; }
    public DateTime LastStatusUpdate { get; set; }
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    public List<OrderMilestone> Milestones { get; set; } = new();
    public bool IsAtRisk { get; set; }
    public string? RiskReason { get; set; }
    public int DaysUntilDelivery { get; set; }
    public decimal CompletionPercentage { get; set; }
}

public class AtRiskOrderInfo
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public int DaysOverdue { get; set; }
    public string RiskReason { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class StatusTransitionRequest
{
    public Guid OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool ForceTransition { get; set; } = false;
}

public class OrderStatusUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public CustomerOrder? UpdatedOrder { get; set; }
    public OrderStatusHistory? StatusHistory { get; set; }
}