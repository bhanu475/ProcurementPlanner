using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class OrderManagementService : IOrderManagementService
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(
        ICustomerOrderRepository orderRepository,
        ILogger<OrderManagementService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<CustomerOrder> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating new order for customer {CustomerId}", request.CustomerId);

        // Validate delivery date
        if (request.RequestedDeliveryDate <= DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Requested delivery date must be in the future");
        }

        // Validate items
        if (!request.Items.Any())
        {
            throw new ArgumentException("Order must contain at least one item");
        }

        // Generate unique order number
        var orderNumber = await GenerateOrderNumberAsync();

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            ProductType = request.ProductType,
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            Status = OrderStatus.Submitted,
            CreatedBy = request.CreatedBy,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = request.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductCode = item.ProductCode,
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Specifications = item.Specifications,
                UnitPrice = item.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList()
        };

        // Validate business rules
        ValidateOrder(order);

        var createdOrder = await _orderRepository.AddAsync(order);
        
        _logger.LogInformation("Order {OrderNumber} created successfully with {ItemCount} items", 
            orderNumber, request.Items.Count);

        return createdOrder;
    }

    public async Task<PagedResult<CustomerOrder>> GetOrdersAsync(OrderFilterRequest filter)
    {
        _logger.LogInformation("Retrieving orders with filter - Page: {Page}, PageSize: {PageSize}", 
            filter.Page, filter.PageSize);

        // Validate pagination parameters
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 20;

        return await _orderRepository.GetOrdersAsync(filter);
    }

    public async Task<CustomerOrder?> GetOrderByIdAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving order {OrderId}", orderId);
        return await _orderRepository.GetOrderWithItemsAsync(orderId);
    }

    public async Task<CustomerOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", orderId, status);

        var order = await _orderRepository.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order with ID {orderId} not found");
        }

        // Validate status transition
        if (!order.CanTransitionTo(status))
        {
            throw new InvalidOperationException($"Cannot transition order from {order.Status} to {status}");
        }

        var previousStatus = order.Status;
        order.TransitionTo(status);
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        _logger.LogInformation("Order {OrderNumber} status updated from {PreviousStatus} to {NewStatus}", 
            order.OrderNumber, previousStatus, status);

        return updatedOrder;
    }

    public async Task<List<CustomerOrder>> GetOrdersByDeliveryDateAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Retrieving orders by delivery date range {StartDate} to {EndDate}", 
            startDate, endDate);

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be greater than end date");
        }

        return await _orderRepository.GetOrdersByDeliveryDateRangeAsync(startDate, endDate);
    }

    public async Task<CustomerOrder> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request)
    {
        _logger.LogInformation("Updating order {OrderId}", orderId);

        var order = await _orderRepository.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order with ID {orderId} not found");
        }

        // Only allow updates if order is in certain statuses
        if (order.Status != OrderStatus.Submitted && order.Status != OrderStatus.UnderReview)
        {
            throw new InvalidOperationException($"Cannot update order in {order.Status} status");
        }

        // Update order properties
        if (!string.IsNullOrEmpty(request.CustomerName))
        {
            order.CustomerName = request.CustomerName;
        }

        if (request.RequestedDeliveryDate.HasValue)
        {
            if (request.RequestedDeliveryDate.Value <= DateTime.UtcNow.Date)
            {
                throw new ArgumentException("Requested delivery date must be in the future");
            }
            order.RequestedDeliveryDate = request.RequestedDeliveryDate.Value;
        }

        if (request.Notes != null)
        {
            order.Notes = request.Notes;
        }

        // Update items if provided
        if (request.Items != null)
        {
            // Remove existing items
            order.Items.Clear();

            // Add updated items
            order.Items = request.Items.Select(item => new OrderItem
            {
                Id = item.Id ?? Guid.NewGuid(),
                OrderId = orderId,
                ProductCode = item.ProductCode,
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Specifications = item.Specifications,
                UnitPrice = item.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();
        }

        order.UpdatedAt = DateTime.UtcNow;

        // Validate updated order
        ValidateOrder(order);

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        _logger.LogInformation("Order {OrderNumber} updated successfully", order.OrderNumber);

        return updatedOrder;
    }

    public async Task DeleteOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Deleting order {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order with ID {orderId} not found");
        }

        // Only allow deletion if order is in submitted status
        if (order.Status != OrderStatus.Submitted)
        {
            throw new InvalidOperationException($"Cannot delete order in {order.Status} status");
        }

        await _orderRepository.DeleteAsync(orderId);

        _logger.LogInformation("Order {OrderNumber} deleted successfully", order.OrderNumber);
    }

    public async Task<OrderDashboardSummary> GetDashboardSummaryAsync(DashboardFilterRequest filter)
    {
        _logger.LogInformation("Retrieving dashboard summary");
        return await _orderRepository.GetDashboardSummaryAsync(filter);
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        string orderNumber;
        bool exists;
        
        do
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            orderNumber = $"ORD-{timestamp}-{random}";
            exists = await _orderRepository.OrderNumberExistsAsync(orderNumber);
        } 
        while (exists);

        return orderNumber;
    }

    private static void ValidateOrder(CustomerOrder order)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerId))
        {
            throw new ArgumentException("Customer ID is required");
        }

        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            throw new ArgumentException("Customer name is required");
        }

        if (order.RequestedDeliveryDate <= DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Requested delivery date must be in the future");
        }

        if (!order.Items.Any())
        {
            throw new ArgumentException("Order must contain at least one item");
        }

        // Validate each item
        foreach (var item in order.Items)
        {
            item.ValidateQuantity();
            item.ValidateProductCode();
            item.ValidateSpecifications();
        }
    }
}