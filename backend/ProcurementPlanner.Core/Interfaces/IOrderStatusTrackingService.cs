using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Interfaces;

public interface IOrderStatusTrackingService
{
    Task<CustomerOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, string? notes = null, Guid? updatedBy = null);
    Task<List<Entities.OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId);
    Task<List<CustomerOrder>> GetAtRiskOrdersAsync();
    Task<List<Entities.OrderMilestone>> GetOrderMilestonesAsync(Guid orderId);
    Task<bool> ValidateStatusTransitionAsync(Guid orderId, OrderStatus newStatus);
    Task ProcessAutomaticStatusTransitionsAsync();
    Task<List<CustomerOrder>> GetOrdersRequiringAttentionAsync();
    Task AddOrderMilestoneAsync(Guid orderId, string milestone, string description, DateTime? targetDate = null);
}