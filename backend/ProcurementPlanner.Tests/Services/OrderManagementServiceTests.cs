using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class OrderManagementServiceTests
{
    private readonly Mock<ICustomerOrderRepository> _mockRepository;
    private readonly Mock<ILogger<OrderManagementService>> _mockLogger;
    private readonly OrderManagementService _service;

    public OrderManagementServiceTests()
    {
        _mockRepository = new Mock<ICustomerOrderRepository>();
        _mockLogger = new Mock<ILogger<OrderManagementService>>();
        _service = new OrderManagementService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidRequest_ReturnsCreatedOrder()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST001",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            CreatedBy = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = 10,
                    Unit = "EA",
                    UnitPrice = 25.50m
                }
            }
        };

        _mockRepository.Setup(r => r.OrderNumberExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<CustomerOrder>()))
            .ReturnsAsync((CustomerOrder order) => order);

        // Act
        var result = await _service.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Equal(request.CustomerName, result.CustomerName);
        Assert.Equal(request.ProductType, result.ProductType);
        Assert.Equal(OrderStatus.Submitted, result.Status);
        Assert.Single(result.Items);
        Assert.NotEmpty(result.OrderNumber);
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<CustomerOrder>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_PastDeliveryDate_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST001",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(-1), // Past date
            CreatedBy = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = 10,
                    Unit = "EA"
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(request));
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_NoItems_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST001",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            CreatedBy = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>() // Empty items
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(request));
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersAsync_ValidFilter_ReturnsPagedResult()
    {
        // Arrange
        var filter = new OrderFilterRequest
        {
            Page = 1,
            PageSize = 10,
            CustomerId = "CUST001"
        };

        var expectedResult = new PagedResult<CustomerOrder>
        {
            Items = new List<CustomerOrder>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-001",
                    CustomerId = "CUST001",
                    CustomerName = "Test Customer"
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetOrdersAsync(filter))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetOrdersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        
        _mockRepository.Verify(r => r.GetOrdersAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            CustomerId = "CUST001",
            CustomerName = "Test Customer"
        };

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _service.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        
        _mockRepository.Verify(r => r.GetOrderWithItemsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistingOrder_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((CustomerOrder?)null);

        // Act
        var result = await _service.GetOrderByIdAsync(orderId);

        // Assert
        Assert.Null(result);
        
        _mockRepository.Verify(r => r.GetOrderWithItemsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Submitted
        };

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<CustomerOrder>()))
            .ReturnsAsync((CustomerOrder o) => o);

        // Act
        var result = await _service.UpdateOrderStatusAsync(orderId, OrderStatus.UnderReview);

        // Assert
        Assert.Equal(OrderStatus.UnderReview, result.Status);
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Submitted
        };

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateOrderStatusAsync(orderId, OrderStatus.Delivered));
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_NonExistingOrder_ThrowsArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((CustomerOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateOrderStatusAsync(orderId, OrderStatus.UnderReview));
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByDeliveryDateAsync_ValidDateRange_ReturnsOrders()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = DateTime.UtcNow.Date.AddDays(7);
        var expectedOrders = new List<CustomerOrder>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                RequestedDeliveryDate = startDate.AddDays(1)
            }
        };

        _mockRepository.Setup(r => r.GetOrdersByDeliveryDateRangeAsync(startDate, endDate))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _service.GetOrdersByDeliveryDateAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        
        _mockRepository.Verify(r => r.GetOrdersByDeliveryDateRangeAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByDeliveryDateAsync_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(7);
        var endDate = DateTime.UtcNow.Date; // End date before start date

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetOrdersByDeliveryDateAsync(startDate, endDate));
        
        _mockRepository.Verify(r => r.GetOrdersByDeliveryDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderAsync_ValidUpdate_UpdatesOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Submitted,
            CustomerId = "CUST001",
            CustomerName = "Old Name",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(5),
            CreatedBy = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = 1,
                    Unit = "EA"
                }
            }
        };

        var updateRequest = new UpdateOrderRequest
        {
            CustomerName = "New Name",
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10)
        };

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<CustomerOrder>()))
            .ReturnsAsync((CustomerOrder o) => o);

        // Act
        var result = await _service.UpdateOrderAsync(orderId, updateRequest);

        // Assert
        Assert.Equal("New Name", result.CustomerName);
        Assert.Equal(updateRequest.RequestedDeliveryDate, result.RequestedDeliveryDate);
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Delivered // Cannot update delivered orders
        };

        var updateRequest = new UpdateOrderRequest
        {
            CustomerName = "New Name"
        };

        _mockRepository.Setup(r => r.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateOrderAsync(orderId, updateRequest));
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOrderAsync_ValidDeletion_DeletesOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Submitted
        };

        _mockRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _mockRepository.Setup(r => r.DeleteAsync(orderId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteOrderAsync(orderId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task DeleteOrderAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.UnderReview // Cannot delete orders not in submitted status
        };

        _mockRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteOrderAsync(orderId));
        
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ValidFilter_ReturnsSummary()
    {
        // Arrange
        var filter = new DashboardFilterRequest
        {
            ProductType = ProductType.LMR
        };

        var expectedSummary = new OrderDashboardSummary
        {
            TotalOrders = 5,
            StatusCounts = new Dictionary<OrderStatus, int>
            {
                { OrderStatus.Submitted, 2 },
                { OrderStatus.UnderReview, 3 }
            }
        };

        _mockRepository.Setup(r => r.GetDashboardSummaryAsync(filter))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _service.GetDashboardSummaryAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalOrders);
        Assert.Equal(2, result.StatusCounts.Count);
        
        _mockRepository.Verify(r => r.GetDashboardSummaryAsync(filter), Times.Once);
    }
}