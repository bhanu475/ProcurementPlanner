using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Services;

public class OrderStatusTrackingService : IOrderStatusTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderStatusTrackingService> _logger;

    public OrderStatusTrackingService(
        ApplicationDbContext context,
        ILogger<OrderStatusTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, string? notes = null, Guid? updatedBy = null)
    {
        _logger.LogInformation("Updating order {OrderId} status to {NewStatus}", orderId, newStatus);

        var order = await _context.CustomerOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new ArgumentException($"Order with ID {orderId} not found");
        }

        var previousStatus = order.Status;

        // Validate status transition
        if (!order.CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition order from {previousStatus} to {newStatus}");
        }

        // Update order status
        order.TransitionTo(newStatus);
        order.UpdatedAt = DateTime.UtcNow;

        // Create status history record
        var statusHistory = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = updatedBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistories.Add(statusHistory);

        // Update milestones based on status change
        await UpdateMilestonesForStatusChangeAsync(orderId, newStatus);

        // Create automatic milestones for certain status transitions
        await CreateAutomaticMilestonesAsync(orderId, newStatus);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} status updated from {PreviousStatus} to {NewStatus}", 
            order.OrderNumber, previousStatus, newStatus);

        return order;
    }

    public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving status history for order {OrderId}", orderId);

        return await _context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .Include(h => h.ChangedByUser)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<List<CustomerOrder>> GetAtRiskOrdersAsync()
    {
        _logger.LogInformation("Retrieving at-risk orders");

        var today = DateTime.UtcNow.Date;
        var warningThreshold = today.AddDays(3); // Orders due within 3 days

        var atRiskOrders = await _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled &&
                       (o.RequestedDeliveryDate < today || // Overdue
                        (o.RequestedDeliveryDate <= warningThreshold && 
                         (o.Status == OrderStatus.Submitted || 
                          o.Status == OrderStatus.UnderReview)))) // Due soon but not progressed
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();

        return atRiskOrders;
    }

    public async Task<List<OrderMilestone>> GetOrderMilestonesAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving milestones for order {OrderId}", orderId);

        return await _context.OrderMilestones
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.TargetDate)
            .ToListAsync();
    }

    public async Task<bool> ValidateStatusTransitionAsync(Guid orderId, OrderStatus newStatus)
    {
        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        return order.CanTransitionTo(newStatus);
    }

    public async Task ProcessAutomaticStatusTransitionsAsync()
    {
        _logger.LogInformation("Processing automatic status transitions");

        // Find orders that should be automatically transitioned
        var ordersToUpdate = await _context.CustomerOrders
            .Where(o => o.Status == OrderStatus.AwaitingSupplierConfirmation)
            .Include(o => o.Items)
            .ToListAsync();

        foreach (var order in ordersToUpdate)
        {
            // Check if all purchase orders for this order are confirmed
            var allPurchaseOrdersConfirmed = await _context.PurchaseOrders
                .Where(po => po.CustomerOrderId == order.Id)
                .AllAsync(po => po.Status == PurchaseOrderStatus.Confirmed || 
                               po.Status == PurchaseOrderStatus.InProduction);

            if (allPurchaseOrdersConfirmed)
            {
                await UpdateOrderStatusAsync(order.Id, OrderStatus.InProduction, 
                    "Automatically transitioned - all purchase orders confirmed");
            }
        }

        // Check for orders that should be marked as ready for delivery
        var ordersInProduction = await _context.CustomerOrders
            .Where(o => o.Status == OrderStatus.InProduction)
            .ToListAsync();

        foreach (var order in ordersInProduction)
        {
            var allPurchaseOrdersReady = await _context.PurchaseOrders
                .Where(po => po.CustomerOrderId == order.Id)
                .AllAsync(po => po.Status == PurchaseOrderStatus.ReadyForShipment);

            if (allPurchaseOrdersReady)
            {
                await UpdateOrderStatusAsync(order.Id, OrderStatus.ReadyForDelivery,
                    "Automatically transitioned - all items ready for delivery");
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<CustomerOrder>> GetOrdersRequiringAttentionAsync()
    {
        _logger.LogInformation("Retrieving orders requiring attention");

        var today = DateTime.UtcNow.Date;
        var urgentThreshold = today.AddDays(1); // Orders due tomorrow

        var ordersRequiringAttention = await _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled &&
                       (o.RequestedDeliveryDate <= urgentThreshold || // Due soon
                        (o.Status == OrderStatus.Submitted && 
                         o.CreatedAt < DateTime.UtcNow.AddHours(-24)) || // Submitted over 24h ago
                        (o.Status == OrderStatus.UnderReview && 
                         o.UpdatedAt < DateTime.UtcNow.AddHours(-48)))) // Under review for over 48h
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync();

        return ordersRequiringAttention;
    }

    public async Task AddOrderMilestoneAsync(Guid orderId, string milestone, string description, DateTime? targetDate = null)
    {
        _logger.LogInformation("Adding milestone {Milestone} to order {OrderId}", milestone, orderId);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new ArgumentException($"Order with ID {orderId} not found");
        }

        var orderMilestone = new OrderMilestone
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Name = milestone,
            Description = description,
            TargetDate = targetDate,
            Status = MilestoneStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderMilestones.Add(orderMilestone);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Milestone {Milestone} added to order {OrderNumber}", milestone, order.OrderNumber);
    }

    private async Task UpdateMilestonesForStatusChangeAsync(Guid orderId, OrderStatus newStatus)
    {
        var milestones = await _context.OrderMilestones
            .Where(m => m.OrderId == orderId && m.Status == MilestoneStatus.Pending)
            .ToListAsync();

        foreach (var milestone in milestones)
        {
            // Mark milestones as completed based on status transitions
            var shouldComplete = newStatus switch
            {
                OrderStatus.UnderReview when milestone.Name == "Order Submitted" => true,
                OrderStatus.PlanningInProgress when milestone.Name == "Review Completed" => true,
                OrderStatus.PurchaseOrdersCreated when milestone.Name == "Planning Completed" => true,
                OrderStatus.AwaitingSupplierConfirmation when milestone.Name == "Purchase Orders Created" => true,
                OrderStatus.InProduction when milestone.Name == "Supplier Confirmation" => true,
                OrderStatus.ReadyForDelivery when milestone.Name == "Production Completed" => true,
                OrderStatus.Delivered when milestone.Name == "Ready for Delivery" => true,
                _ => false
            };

            if (shouldComplete)
            {
                milestone.MarkCompleted();
            }
        }
    }

    private async Task CreateAutomaticMilestonesAsync(Guid orderId, OrderStatus newStatus)
    {
        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return;

        // Create milestones based on status transitions
        switch (newStatus)
        {
            case OrderStatus.UnderReview:
                await CreateMilestoneIfNotExistsAsync(orderId, "Review Completed", 
                    "Order review and validation completed", 
                    DateTime.UtcNow.AddDays(1));
                break;

            case OrderStatus.PlanningInProgress:
                await CreateMilestoneIfNotExistsAsync(orderId, "Planning Completed", 
                    "Procurement planning and supplier allocation completed", 
                    DateTime.UtcNow.AddDays(2));
                break;

            case OrderStatus.PurchaseOrdersCreated:
                await CreateMilestoneIfNotExistsAsync(orderId, "Supplier Confirmation", 
                    "All suppliers confirm purchase orders", 
                    DateTime.UtcNow.AddDays(3));
                break;

            case OrderStatus.InProduction:
                var productionDays = order.ProductType == ProductType.LMR ? 7 : 5; // LMR takes longer
                await CreateMilestoneIfNotExistsAsync(orderId, "Production Completed", 
                    "All items produced and ready for shipment", 
                    DateTime.UtcNow.AddDays(productionDays));
                break;

            case OrderStatus.ReadyForDelivery:
                var deliveryBuffer = (order.RequestedDeliveryDate - DateTime.UtcNow.Date).TotalDays;
                var targetDeliveryDate = deliveryBuffer > 1 ? order.RequestedDeliveryDate.AddDays(-1) : order.RequestedDeliveryDate;
                await CreateMilestoneIfNotExistsAsync(orderId, "Delivery Completed", 
                    "Order delivered to customer", 
                    targetDeliveryDate);
                break;
        }
    }

    private async Task CreateMilestoneIfNotExistsAsync(Guid orderId, string name, string description, DateTime targetDate)
    {
        var existingMilestone = await _context.OrderMilestones
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.Name == name);

        if (existingMilestone == null)
        {
            await AddOrderMilestoneAsync(orderId, name, description, targetDate);
        }
    }
}