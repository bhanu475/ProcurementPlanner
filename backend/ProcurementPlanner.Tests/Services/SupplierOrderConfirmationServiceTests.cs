using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class SupplierOrderConfirmationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SupplierOrderConfirmationService>> _mockLogger;
    private readonly SupplierOrderConfirmationService _service;
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _customerOrderId = Guid.NewGuid();
    private readonly Guid _purchaseOrderId = Guid.NewGuid();

    public SupplierOrderConfirmationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<SupplierOrderConfirmationService>>();
        _service = new SupplierOrderConfirmationService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var supplier = new Supplier
        {
            Id = _supplierId,
            Name = "Test Supplier",
            ContactEmail = "supplier@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            IsActive = true,
            Performance = new SupplierPerformanceMetrics
            {
                SupplierId = _supplierId,
                OnTimeDeliveryRate = 95.5m,
                QualityScore = 4.5m,
                TotalOrdersCompleted = 50,
                LastUpdated = DateTime.UtcNow
            }
        };

        var customerOrder = new CustomerOrder
        {
            Id = _customerOrderId,
            OrderNumber = "CO-2024-001",
            CustomerId = "DODAAC123",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Status = OrderStatus.PurchaseOrdersCreated,
            CreatedBy = Guid.NewGuid()
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = _customerOrderId,
            ProductCode = "PROD-001",
            Description = "Test Product",
            Quantity = 100,
            Unit = "EA",
            Specifications = "Test specifications"
        };

        var purchaseOrder = new PurchaseOrder
        {
            Id = _purchaseOrderId,
            PurchaseOrderNumber = "PO-2024-001",
            CustomerOrderId = _customerOrderId,
            SupplierId = _supplierId,
            Status = PurchaseOrderStatus.SentToSupplier,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(25),
            CreatedBy = Guid.NewGuid(),
            TotalValue = 1000m
        };

        var purchaseOrderItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = _purchaseOrderId,
            OrderItemId = orderItem.Id,
            ProductCode = "PROD-001",
            Description = "Test Product",
            AllocatedQuantity = 50,
            Unit = "EA",
            UnitPrice = 20m
        };

        customerOrder.Items.Add(orderItem);
        purchaseOrder.Items.Add(purchaseOrderItem);

        _context.Suppliers.Add(supplier);
        _context.CustomerOrders.Add(customerOrder);
        _context.PurchaseOrders.Add(purchaseOrder);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSupplierPurchaseOrdersAsync_ReturnsOrdersForSupplier()
    {
        // Act
        var result = await _service.GetSupplierPurchaseOrdersAsync(_supplierId);

        // Assert
        Assert.Single(result);
        Assert.Equal(_purchaseOrderId, result[0].Id);
        Assert.Equal(_supplierId, result[0].SupplierId);
    }

    [Fact]
    public async Task GetSupplierPurchaseOrdersAsync_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Act
        var result = await _service.GetSupplierPurchaseOrdersAsync(_supplierId, PurchaseOrderStatus.SentToSupplier);

        // Assert
        Assert.Single(result);
        Assert.Equal(PurchaseOrderStatus.SentToSupplier, result[0].Status);
    }

    [Fact]
    public async Task GetSupplierPurchaseOrderAsync_ValidSupplier_ReturnsOrder()
    {
        // Act
        var result = await _service.GetSupplierPurchaseOrderAsync(_purchaseOrderId, _supplierId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_purchaseOrderId, result.Id);
        Assert.Equal(_supplierId, result.SupplierId);
    }

    [Fact]
    public async Task GetSupplierPurchaseOrderAsync_InvalidSupplier_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var invalidSupplierId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetSupplierPurchaseOrderAsync(_purchaseOrderId, invalidSupplierId));
    }

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_ValidConfirmation_UpdatesOrderStatus()
    {
        // Arrange
        var confirmation = new SupplierOrderConfirmation
        {
            SupplierNotes = "Order confirmed",
            ItemUpdates = new List<SupplierItemUpdate>
            {
                new SupplierItemUpdate
                {
                    PurchaseOrderItemId = _context.PurchaseOrderItems.First().Id,
                    PackagingDetails = "Standard packaging",
                    DeliveryMethod = "Ground shipping",
                    EstimatedDeliveryDate = DateTime.UtcNow.AddDays(20),
                    UnitPrice = 22m,
                    SupplierNotes = "Item confirmed"
                }
            }
        };

        // Act
        var result = await _service.ConfirmPurchaseOrderAsync(_purchaseOrderId, _supplierId, confirmation);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Confirmed, result.Status);
        Assert.Equal("Order confirmed", result.SupplierNotes);
        Assert.NotNull(result.ConfirmedAt);

        var item = result.Items.First();
        Assert.Equal("Standard packaging", item.PackagingDetails);
        Assert.Equal("Ground shipping", item.DeliveryMethod);
        Assert.Equal(22m, item.UnitPrice);
    }

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(_purchaseOrderId);
        purchaseOrder!.Status = PurchaseOrderStatus.Confirmed;
        await _context.SaveChangesAsync();

        var confirmation = new SupplierOrderConfirmation
        {
            SupplierNotes = "Order confirmed"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ConfirmPurchaseOrderAsync(_purchaseOrderId, _supplierId, confirmation));
    }

    [Fact]
    public async Task RejectPurchaseOrderAsync_ValidRejection_UpdatesOrderStatus()
    {
        // Arrange
        var rejectionReason = "Insufficient capacity";

        // Act
        var result = await _service.RejectPurchaseOrderAsync(_purchaseOrderId, _supplierId, rejectionReason);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Rejected, result.Status);
        Assert.Equal(rejectionReason, result.RejectionReason);
        Assert.NotNull(result.RejectedAt);
    }

    [Fact]
    public async Task RejectPurchaseOrderAsync_EmptyReason_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RejectPurchaseOrderAsync(_purchaseOrderId, _supplierId, ""));
    }

    [Fact]
    public async Task ValidateDeliveryDatesAsync_ValidDates_ReturnsValidResult()
    {
        // Arrange
        var deliveryDates = new Dictionary<Guid, DateTime>
        {
            { _context.PurchaseOrderItems.First().Id, DateTime.UtcNow.AddDays(20) }
        };

        // Act
        var result = await _service.ValidateDeliveryDatesAsync(_purchaseOrderId, deliveryDates);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateDeliveryDatesAsync_DateInPast_ReturnsInvalidResult()
    {
        // Arrange
        var deliveryDates = new Dictionary<Guid, DateTime>
        {
            { _context.PurchaseOrderItems.First().Id, DateTime.UtcNow.AddDays(-1) }
        };

        // Act
        var result = await _service.ValidateDeliveryDatesAsync(_purchaseOrderId, deliveryDates);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be in the past", result.Errors[0].Message);
    }

    [Fact]
    public async Task ValidateDeliveryDatesAsync_DateAfterRequired_ReturnsInvalidResult()
    {
        // Arrange
        var deliveryDates = new Dictionary<Guid, DateTime>
        {
            { _context.PurchaseOrderItems.First().Id, DateTime.UtcNow.AddDays(30) }
        };

        // Act
        var result = await _service.ValidateDeliveryDatesAsync(_purchaseOrderId, deliveryDates);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("after required delivery date", result.Errors[0].Message);
    }

    [Fact]
    public async Task ValidateDeliveryDatesAsync_CloseToRequiredDate_ReturnsWarning()
    {
        // Arrange
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(_purchaseOrderId);
        var deliveryDate = purchaseOrder!.RequiredDeliveryDate.AddDays(-1);
        
        var deliveryDates = new Dictionary<Guid, DateTime>
        {
            { _context.PurchaseOrderItems.First().Id, deliveryDate }
        };

        // Act
        var result = await _service.ValidateDeliveryDatesAsync(_purchaseOrderId, deliveryDates);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Warnings);
        Assert.Contains("buffer", result.Warnings[0].Message);
    }

    [Fact]
    public async Task GetSupplierDashboardSummaryAsync_ReturnsCorrectSummary()
    {
        // Act
        var result = await _service.GetSupplierDashboardSummaryAsync(_supplierId);

        // Assert
        Assert.Equal(_supplierId, result.SupplierId);
        Assert.Equal("Test Supplier", result.SupplierName);
        Assert.Equal(1, result.PendingOrdersCount);
        Assert.Equal(0, result.ConfirmedOrdersCount);
        Assert.Equal(95.5m, result.Performance.OnTimeDeliveryRate);
        Assert.Equal(4.5m, result.Performance.QualityScore);
        Assert.Single(result.RecentOrders);
    }

    [Fact]
    public async Task NotifySupplierOfNewOrderAsync_ValidOrder_ReturnsTrue()
    {
        // Act
        var result = await _service.NotifySupplierOfNewOrderAsync(_purchaseOrderId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task NotifySupplierOfNewOrderAsync_InvalidOrder_ReturnsFalse()
    {
        // Arrange
        var invalidOrderId = Guid.NewGuid();

        // Act
        var result = await _service.NotifySupplierOfNewOrderAsync(invalidOrderId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetSupplierOrderHistoryAsync_ReturnsPagedResults()
    {
        // Arrange
        var filter = new SupplierOrderHistoryFilter
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetSupplierOrderHistoryAsync(_supplierId, filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetSupplierOrderHistoryAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var filter = new SupplierOrderHistoryFilter
        {
            Status = PurchaseOrderStatus.SentToSupplier,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetSupplierOrderHistoryAsync(_supplierId, filter);

        // Assert
        Assert.Single(result.Items);
        Assert.All(result.Items, item => Assert.Equal(PurchaseOrderStatus.SentToSupplier, item.Status));
    }

    [Fact]
    public async Task UpdatePurchaseOrderItemsAsync_ValidUpdates_UpdatesItems()
    {
        // Arrange
        var itemUpdates = new List<SupplierItemUpdate>
        {
            new SupplierItemUpdate
            {
                PurchaseOrderItemId = _context.PurchaseOrderItems.First().Id,
                PackagingDetails = "Updated packaging",
                DeliveryMethod = "Express shipping",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(15),
                UnitPrice = 25m,
                SupplierNotes = "Updated notes"
            }
        };

        // Act
        var result = await _service.UpdatePurchaseOrderItemsAsync(_purchaseOrderId, _supplierId, itemUpdates);

        // Assert
        var item = result.Items.First();
        Assert.Equal("Updated packaging", item.PackagingDetails);
        Assert.Equal("Express shipping", item.DeliveryMethod);
        Assert.Equal(25m, item.UnitPrice);
        Assert.Contains("Updated notes", item.SupplierNotes);
    }

    [Fact]
    public async Task UpdatePurchaseOrderItemsAsync_InvalidDeliveryDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var itemUpdates = new List<SupplierItemUpdate>
        {
            new SupplierItemUpdate
            {
                PurchaseOrderItemId = _context.PurchaseOrderItems.First().Id,
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(30) // After required delivery date
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePurchaseOrderItemsAsync(_purchaseOrderId, _supplierId, itemUpdates));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}