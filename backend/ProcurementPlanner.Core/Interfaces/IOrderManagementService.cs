using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IOrderManagementService
{
    Task<CustomerOrder> CreateOrderAsync(CreateOrderRequest request);
    Task<PagedResult<CustomerOrder>> GetOrdersAsync(OrderFilterRequest filter);
    Task<CustomerOrder?> GetOrderByIdAsync(Guid orderId);
    Task<CustomerOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
    Task<List<CustomerOrder>> GetOrdersByDeliveryDateAsync(DateTime startDate, DateTime endDate);
    Task<CustomerOrder> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request);
    Task DeleteOrderAsync(Guid orderId);
    Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter);
}