using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IDashboardService
{
    Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter);
    Task<List<OrdersByDeliveryDate>> GetOrdersByDeliveryDateAsync(DashboardFilterRequest filter);
    Task<List<OrdersByCustomer>> GetTopCustomersAsync(DashboardFilterRequest filter, int topCount = 10);
    Task InvalidateCacheAsync();
}