using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class OrderStatusTrackingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<OrderStatusTrackingService>> _mockLogger;
    private readonly OrderStatusTrackingService _service;

    public OrderStatusTrackingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<OrderStatusTrackingService>>();
        _service = new OrderStatusTrackingService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidTransition_UpdatesStatusAndCreatesHistory()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1001",
            CustomerId = "CUST001",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = 10,
                    Unit = "EA",
                    UnitPrice = 100.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var notes = "Status updated for testing";

        // Act
        var result = await _service.UpdateOrderStatusAsync(order.Id, OrderStatus.UnderReview, notes, userId);

        // Assert
        Assert.Equal(OrderStatus.UnderReview, result.Status);

        var statusHistory = await _context.OrderStatusHistories
            .FirstOrDefaultAsync(h => h.OrderId == order.Id);

        Assert.NotNull(statusHistory);
        Assert.Equal(OrderStatus.Submitted, statusHistory.FromStatus);
        Assert.Equal(OrderStatus.UnderReview, statusHistory.ToStatus);
        Assert.Equal(userId, statusHistory.ChangedBy);
        Assert.Equal(notes, statusHistory.Notes);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1002",
            CustomerId = "CUST002",
            CustomerName = "Test Customer 2",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(5),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentOrderId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateOrderStatusAsync(nonExistentOrderId, OrderStatus.UnderReview));
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_ReturnsHistoryInChronologicalOrder()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1003",
            CustomerId = "CUST003",
            CustomerName = "Test Customer 3",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = OrderStatus.PlanningInProgress,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);

        var history1 = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = OrderStatus.Submitted,
            ToStatus = OrderStatus.UnderReview,
            ChangedAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var history2 = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = OrderStatus.UnderReview,
            ToStatus = OrderStatus.PlanningInProgress,
            ChangedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrderStatusHistoryAsync(order.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(OrderStatus.Submitted, result[0].FromStatus);
        Assert.Equal(OrderStatus.UnderReview, result[0].ToStatus);
        Assert.Equal(OrderStatus.UnderReview, result[1].FromStatus);
        Assert.Equal(OrderStatus.PlanningInProgress, result[1].ToStatus);
        Assert.True(result[0].ChangedAt < result[1].ChangedAt);
    }

    [Fact]
    public async Task GetAtRiskOrdersAsync_ReturnsOverdueAndSoonDueOrders()
    {
        // Arrange
        var overdueOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1004",
            CustomerId = "CUST004",
            CustomerName = "Overdue Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(-1), // Overdue
            Status = OrderStatus.InProduction,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        var soonDueOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1005",
            CustomerId = "CUST005",
            CustomerName = "Soon Due Customer",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(2), // Due soon but not progressed
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        var normalOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1006",
            CustomerId = "CUST006",
            CustomerName = "Normal Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10), // Not at risk
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.AddRange(overdueOrder, soonDueOrder, normalOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAtRiskOrdersAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Id == overdueOrder.Id);
        Assert.Contains(result, o => o.Id == soonDueOrder.Id);
        Assert.DoesNotContain(result, o => o.Id == normalOrder.Id);
    }

    [Fact]
    public async Task ValidateStatusTransitionAsync_ValidTransition_ReturnsTrue()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1007",
            CustomerId = "CUST007",
            CustomerName = "Test Customer 7",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(5),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateStatusTransitionAsync(order.Id, OrderStatus.PlanningInProgress);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateStatusTransitionAsync_InvalidTransition_ReturnsFalse()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1008",
            CustomerId = "CUST008",
            CustomerName = "Test Customer 8",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(8),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateStatusTransitionAsync(order.Id, OrderStatus.Delivered);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddOrderMilestoneAsync_ValidOrder_CreatesMilestone()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1009",
            CustomerId = "CUST009",
            CustomerName = "Test Customer 9",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(6),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        var milestoneName = "Custom Milestone";
        var description = "Custom milestone description";
        var targetDate = DateTime.UtcNow.AddDays(3);

        // Act
        await _service.AddOrderMilestoneAsync(order.Id, milestoneName, description, targetDate);

        // Assert
        var milestone = await _context.OrderMilestones
            .FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Name == milestoneName);

        Assert.NotNull(milestone);
        Assert.Equal(description, milestone.Description);
        Assert.Equal(targetDate.Date, milestone.TargetDate?.Date);
        Assert.Equal(MilestoneStatus.Pending, milestone.Status);
    }

    [Fact]
    public async Task GetOrderMilestonesAsync_ReturnsOrderedMilestones()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1010",
            CustomerId = "CUST010",
            CustomerName = "Test Customer 10",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(12),
            Status = OrderStatus.PlanningInProgress,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);

        var milestone1 = new OrderMilestone
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Name = "First Milestone",
            Description = "First milestone description",
            TargetDate = DateTime.UtcNow.AddDays(2),
            Status = MilestoneStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var milestone2 = new OrderMilestone
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Name = "Second Milestone",
            Description = "Second milestone description",
            TargetDate = DateTime.UtcNow.AddDays(1),
            Status = MilestoneStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderMilestones.AddRange(milestone1, milestone2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrderMilestonesAsync(order.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Second Milestone", result[0].Name); // Should be first due to earlier target date
        Assert.Equal("First Milestone", result[1].Name);
    }

    [Fact]
    public async Task GetOrdersRequiringAttentionAsync_ReturnsOrdersNeedingAction()
    {
        // Arrange
        var urgentOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1011",
            CustomerId = "CUST011",
            CustomerName = "Urgent Customer",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.Date.AddDays(1), // Due tomorrow
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            Items = new List<OrderItem>()
        };

        var staleOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1012",
            CustomerId = "CUST012",
            CustomerName = "Stale Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-25), // Submitted over 24h ago
            UpdatedAt = DateTime.UtcNow.AddHours(-25),
            Items = new List<OrderItem>()
        };

        var normalOrder = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1013",
            CustomerId = "CUST013",
            CustomerName = "Normal Customer",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.AddRange(urgentOrder, staleOrder, normalOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrdersRequiringAttentionAsync();

        // Assert
        Assert.True(result.Count >= 1); // At least the urgent order should be returned
        Assert.Contains(result, o => o.Id == urgentOrder.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}