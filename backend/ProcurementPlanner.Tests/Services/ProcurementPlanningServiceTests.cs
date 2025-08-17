using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class ProcurementPlanningServiceTests
{
    private readonly Mock<ICustomerOrderRepository> _mockCustomerOrderRepository;
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IRepository<PurchaseOrder>> _mockPurchaseOrderRepository;
    private readonly Mock<IRepository<PurchaseOrderItem>> _mockPurchaseOrderItemRepository;
    private readonly Mock<IDistributionAlgorithmService> _mockDistributionAlgorithmService;
    private readonly Mock<ILogger<ProcurementPlanningService>> _mockLogger;
    private readonly ProcurementPlanningService _service;

    public ProcurementPlanningServiceTests()
    {
        _mockCustomerOrderRepository = new Mock<ICustomerOrderRepository>();
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockPurchaseOrderRepository = new Mock<IRepository<PurchaseOrder>>();
        _mockPurchaseOrderItemRepository = new Mock<IRepository<PurchaseOrderItem>>();
        _mockDistributionAlgorithmService = new Mock<IDistributionAlgorithmService>();
        _mockLogger = new Mock<ILogger<ProcurementPlanningService>>();

        _service = new ProcurementPlanningService(
            _mockCustomerOrderRepository.Object,
            _mockSupplierRepository.Object,
            _mockPurchaseOrderRepository.Object,
            _mockPurchaseOrderItemRepository.Object,
            _mockDistributionAlgorithmService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreatePurchaseOrdersAsync_WithValidDistributionPlan_CreatesPurchaseOrders()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder();
        var distributionPlan = CreateTestDistributionPlan(customerOrder.Id);
        var suppliers = CreateTestSuppliers();

        SetupMocksForSuccessfulCreation(customerOrder, distributionPlan, suppliers);

        // Act
        var result = await _service.CreatePurchaseOrdersAsync(customerOrder.Id, distributionPlan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should create 2 purchase orders for 2 suppliers
        Assert.All(result, po => Assert.Equal(PurchaseOrderStatus.SentToSupplier, po.Status));
        Assert.All(result, po => Assert.Equal(customerOrder.Id, po.CustomerOrderId));
        
        _mockPurchaseOrderRepository.Verify(x => x.AddAsync(It.IsAny<PurchaseOrder>()), Times.Exactly(2));
        _mockCustomerOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<CustomerOrder>()), Times.Once);
    }

    [Fact]
    public async Task CreatePurchaseOrdersAsync_WithInvalidCustomerOrder_ThrowsArgumentException()
    {
        // Arrange
        var customerOrderId = Guid.NewGuid();
        var distributionPlan = CreateTestDistributionPlan(customerOrderId);

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrderId))
            .ReturnsAsync((CustomerOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreatePurchaseOrdersAsync(customerOrderId, distributionPlan));
    }

    [Fact]
    public async Task CreatePurchaseOrdersAsync_WithWrongOrderStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder();
        customerOrder.Status = OrderStatus.Submitted; // Wrong status
        var distributionPlan = CreateTestDistributionPlan(customerOrder.Id);

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrder.Id))
            .ReturnsAsync(customerOrder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreatePurchaseOrdersAsync(customerOrder.Id, distributionPlan));
    }

    [Fact]
    public async Task CreatePurchaseOrdersAsync_WithInvalidDistributionPlan_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder();
        var distributionPlan = CreateTestDistributionPlan(customerOrder.Id);

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrder.Id))
            .ReturnsAsync(customerOrder);

        var validationResult = new DistributionValidationResult();
        validationResult.AddError("Invalid distribution plan");

        _mockDistributionAlgorithmService.Setup(x => x.ValidateDistributionAsync(distributionPlan))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreatePurchaseOrdersAsync(customerOrder.Id, distributionPlan));
    }

    [Fact]
    public async Task SuggestSupplierDistributionAsync_WithValidCustomerOrder_ReturnsDistributionSuggestion()
    {
        // Arrange
        var customerOrder = CreateTestCustomerOrder();
        var expectedSuggestion = new DistributionSuggestion
        {
            CustomerOrderId = customerOrder.Id,
            TotalQuantity = 100,
            ProductType = ProductType.LMR
        };

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrder.Id))
            .ReturnsAsync(customerOrder);

        _mockDistributionAlgorithmService.Setup(x => x.GenerateDistributionSuggestionAsync(customerOrder))
            .ReturnsAsync(expectedSuggestion);

        // Act
        var result = await _service.SuggestSupplierDistributionAsync(customerOrder.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customerOrder.Id, result.CustomerOrderId);
        Assert.Equal(100, result.TotalQuantity);
        Assert.Equal(ProductType.LMR, result.ProductType);
    }

    [Fact]
    public async Task SuggestSupplierDistributionAsync_WithInvalidCustomerOrder_ThrowsArgumentException()
    {
        // Arrange
        var customerOrderId = Guid.NewGuid();

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrderId))
            .ReturnsAsync((CustomerOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SuggestSupplierDistributionAsync(customerOrderId));
    }

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_WithValidPurchaseOrder_ConfirmsOrder()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;
        var confirmation = CreateTestSupplierConfirmation(purchaseOrder);

        _mockPurchaseOrderRepository.Setup(x => x.GetByIdAsync(purchaseOrder.Id))
            .ReturnsAsync(purchaseOrder);

        _mockPurchaseOrderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<PurchaseOrder> { purchaseOrder });

        // Act
        var result = await _service.ConfirmPurchaseOrderAsync(purchaseOrder.Id, confirmation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Confirmed, result.Status);
        Assert.NotNull(result.ConfirmedAt);
        
        _mockPurchaseOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.AtLeastOnce);
        _mockPurchaseOrderItemRepository.Verify(x => x.UpdateAsync(It.IsAny<PurchaseOrderItem>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_WithInvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Confirmed; // Already confirmed
        var confirmation = CreateTestSupplierConfirmation(purchaseOrder);

        _mockPurchaseOrderRepository.Setup(x => x.GetByIdAsync(purchaseOrder.Id))
            .ReturnsAsync(purchaseOrder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ConfirmPurchaseOrderAsync(purchaseOrder.Id, confirmation));
    }

    [Fact]
    public async Task RejectPurchaseOrderAsync_WithValidPurchaseOrder_RejectsOrder()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;
        var rejectionReason = "Insufficient capacity";

        _mockPurchaseOrderRepository.Setup(x => x.GetByIdAsync(purchaseOrder.Id))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.RejectPurchaseOrderAsync(purchaseOrder.Id, rejectionReason);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Rejected, result.Status);
        Assert.Equal(rejectionReason, result.RejectionReason);
        Assert.NotNull(result.RejectedAt);
        
        _mockPurchaseOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Once);
    }

    [Fact]
    public async Task GetPurchaseOrdersBySupplierAsync_WithValidSupplier_ReturnsPurchaseOrders()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var purchaseOrders = new List<PurchaseOrder>
        {
            CreateTestPurchaseOrder(supplierId),
            CreateTestPurchaseOrder(supplierId),
            CreateTestPurchaseOrder(Guid.NewGuid()) // Different supplier
        };

        _mockPurchaseOrderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersBySupplierAsync(supplierId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, po => Assert.Equal(supplierId, po.SupplierId));
    }

    [Fact]
    public async Task GetPurchaseOrdersBySupplierAsync_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var purchaseOrders = new List<PurchaseOrder>
        {
            CreateTestPurchaseOrder(supplierId, PurchaseOrderStatus.Confirmed),
            CreateTestPurchaseOrder(supplierId, PurchaseOrderStatus.SentToSupplier),
            CreateTestPurchaseOrder(supplierId, PurchaseOrderStatus.Confirmed)
        };

        _mockPurchaseOrderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersBySupplierAsync(supplierId, PurchaseOrderStatus.Confirmed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, po => Assert.Equal(PurchaseOrderStatus.Confirmed, po.Status));
    }

    [Fact]
    public async Task UpdatePurchaseOrderItemAsync_WithValidItem_UpdatesItem()
    {
        // Arrange
        var purchaseOrderItem = CreateTestPurchaseOrderItem();
        var itemUpdate = new PurchaseOrderItemUpdate
        {
            PackagingDetails = "Updated packaging",
            DeliveryMethod = "Express",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5),
            UnitPrice = 15.99m,
            SupplierNotes = "Updated notes"
        };

        _mockPurchaseOrderItemRepository.Setup(x => x.GetByIdAsync(purchaseOrderItem.Id))
            .ReturnsAsync(purchaseOrderItem);

        // Act
        var result = await _service.UpdatePurchaseOrderItemAsync(purchaseOrderItem.Id, itemUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated packaging", result.PackagingDetails);
        Assert.Equal("Express", result.DeliveryMethod);
        Assert.Equal(15.99m, result.UnitPrice);
        
        _mockPurchaseOrderItemRepository.Verify(x => x.UpdateAsync(It.IsAny<PurchaseOrderItem>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePurchaseOrderNumberAsync_WithValidInputs_GeneratesUniqueNumber()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var customerOrderId = Guid.NewGuid();
        var supplier = CreateTestSupplier("Test Supplier", supplierId);
        var customerOrder = CreateTestCustomerOrder(customerOrderId, "CO-001");

        _mockSupplierRepository.Setup(x => x.GetByIdAsync(supplierId))
            .ReturnsAsync(supplier);
        
        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrderId))
            .ReturnsAsync(customerOrder);

        _mockPurchaseOrderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<PurchaseOrder>());

        // Act
        var result = await _service.GeneratePurchaseOrderNumberAsync(supplierId, customerOrderId);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("PO-CO-001-TES-", result);
        Assert.EndsWith("001", result);
    }

    [Fact]
    public async Task ValidateDistributionPlanAsync_WithValidPlan_ReturnsValidResult()
    {
        // Arrange
        var customerOrderId = Guid.NewGuid();
        var customerOrder = CreateTestCustomerOrder(customerOrderId);
        var distributionPlan = CreateTestDistributionPlan(customerOrderId);

        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrderId))
            .ReturnsAsync(customerOrder);

        var validationResult = new DistributionValidationResult { IsValid = true };
        _mockDistributionAlgorithmService.Setup(x => x.ValidateDistributionAsync(distributionPlan))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _service.ValidateDistributionPlanAsync(customerOrderId, distributionPlan);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // Helper methods
    private CustomerOrder CreateTestCustomerOrder(Guid? id = null, string orderNumber = "TEST-001")
    {
        return new CustomerOrder
        {
            Id = id ?? Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerId = "DODAAC123",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Status = OrderStatus.PlanningInProgress,
            CreatedBy = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD-001",
                    Description = "Test Product",
                    Quantity = 100,
                    Unit = "EA",
                    UnitPrice = 10.00m
                }
            }
        };
    }

    private DistributionPlan CreateTestDistributionPlan(Guid customerOrderId)
    {
        return new DistributionPlan
        {
            CustomerOrderId = customerOrderId,
            Allocations = new List<SupplierAllocation>
            {
                new SupplierAllocation
                {
                    SupplierId = Guid.NewGuid(),
                    SupplierName = "Supplier A",
                    AllocatedQuantity = 60,
                    AllocationPercentage = 60m,
                    AvailableCapacity = 100,
                    PerformanceScore = 0.9m
                },
                new SupplierAllocation
                {
                    SupplierId = Guid.NewGuid(),
                    SupplierName = "Supplier B",
                    AllocatedQuantity = 40,
                    AllocationPercentage = 40m,
                    AvailableCapacity = 80,
                    PerformanceScore = 0.85m
                }
            },
            Strategy = DistributionStrategy.Balanced,
            CreatedBy = Guid.NewGuid()
        };
    }

    private List<Supplier> CreateTestSuppliers()
    {
        return new List<Supplier>
        {
            CreateTestSupplier("Supplier A"),
            CreateTestSupplier("Supplier B")
        };
    }

    private Supplier CreateTestSupplier(string name, Guid? id = null)
    {
        return new Supplier
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            ContactEmail = $"{name.ToLower().Replace(" ", "")}@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            IsActive = true
        };
    }

    private PurchaseOrder CreateTestPurchaseOrder(Guid? supplierId = null, PurchaseOrderStatus status = PurchaseOrderStatus.Created)
    {
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-TEST-001",
            CustomerOrderId = Guid.NewGuid(),
            SupplierId = supplierId ?? Guid.NewGuid(),
            Status = status,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(30),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        purchaseOrder.Items.Add(CreateTestPurchaseOrderItem(purchaseOrder.Id));
        return purchaseOrder;
    }

    private PurchaseOrderItem CreateTestPurchaseOrderItem(Guid? purchaseOrderId = null)
    {
        return new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrderId ?? Guid.NewGuid(),
            OrderItemId = Guid.NewGuid(),
            ProductCode = "PROD-001",
            Description = "Test Product",
            AllocatedQuantity = 50,
            Unit = "EA",
            UnitPrice = 10.00m,
            CreatedAt = DateTime.UtcNow,
            PurchaseOrder = new PurchaseOrder
            {
                RequiredDeliveryDate = DateTime.UtcNow.AddDays(30)
            }
        };
    }

    private SupplierConfirmation CreateTestSupplierConfirmation(PurchaseOrder purchaseOrder)
    {
        return new SupplierConfirmation
        {
            SupplierNotes = "Order confirmed",
            ItemConfirmations = purchaseOrder.Items.Select(item => new PurchaseOrderItemConfirmation
            {
                PurchaseOrderItemId = item.Id,
                PackagingDetails = "Standard packaging",
                DeliveryMethod = "Standard delivery",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(10),
                UnitPrice = 12.00m,
                SupplierNotes = "Item confirmed"
            }).ToList(),
            ConfirmedBy = Guid.NewGuid()
        };
    }

    private void SetupMocksForSuccessfulCreation(CustomerOrder customerOrder, DistributionPlan distributionPlan, List<Supplier> suppliers)
    {
        _mockCustomerOrderRepository.Setup(x => x.GetByIdAsync(customerOrder.Id))
            .ReturnsAsync(customerOrder);

        var validationResult = new DistributionValidationResult { IsValid = true };
        _mockDistributionAlgorithmService.Setup(x => x.ValidateDistributionAsync(distributionPlan))
            .ReturnsAsync(validationResult);

        // Update supplier IDs to match allocation IDs and setup mocks
        for (int i = 0; i < distributionPlan.Allocations.Count && i < suppliers.Count; i++)
        {
            var allocation = distributionPlan.Allocations[i];
            var supplier = suppliers[i];
            supplier.Id = allocation.SupplierId; // Ensure IDs match
            
            _mockSupplierRepository.Setup(x => x.GetByIdAsync(allocation.SupplierId))
                .ReturnsAsync(supplier);
        }

        _mockPurchaseOrderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<PurchaseOrder>());

        _mockPurchaseOrderRepository.Setup(x => x.AddAsync(It.IsAny<PurchaseOrder>()))
            .ReturnsAsync((PurchaseOrder po) => po);

        _mockPurchaseOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<PurchaseOrder>()))
            .ReturnsAsync((PurchaseOrder po) => po);

        _mockCustomerOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<CustomerOrder>()))
            .ReturnsAsync((CustomerOrder co) => co);
    }
}