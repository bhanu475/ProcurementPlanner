using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface ICustomerOrderRepository : IRepository<CustomerOrder>
{
    Task<PagedResult<CustomerOrder>> GetOrdersAsync(OrderFilterRequest filter);
    Task<List<CustomerOrder>> GetOrdersByDeliveryDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<CustomerOrder?> GetOrderWithItemsAsync(Guid orderId);
    Task<List<CustomerOrder>> GetOverdueOrdersAsync();
    Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter);
    Task<bool> OrderNumberExistsAsync(string orderNumber);
    Task<List<CustomerOrder>> GetOrdersByCustomerIdAsync(string customerId);
}