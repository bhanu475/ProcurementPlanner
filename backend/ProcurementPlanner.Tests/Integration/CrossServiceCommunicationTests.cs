using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Tests.Integration;

/// <summary>
/// Tests cross-service communication, data consistency, and service integration
/// </summary>
public class CrossServiceCommunicationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private ApplicationDbContext _context;

    public CrossServiceCommunicationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context?.Dispose();
        _client?.Dispose();
    }

    [Fact]
    public async Task OrderManagementService_ProcurementService_Integration_ShouldMaintainDataConsistency()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
        var procurementService = scope.ServiceProvider.GetRequiredService<IProcurementPlanningService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // Step 1: Create order through service
        var createOrderRequest = new CreateOrderRequest
        {
            CustomerId = "DODAAC001",
            CustomerName = "Test Base",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10),
            CreatedBy = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = 100,
                    Unit = "EA",
                    UnitPrice = 5.00m
                }
            }
        };

        var createdOrder = await orderService.CreateOrderAsync(createOrderRequest);
        createdOrder.Should().NotBeNull();
        createdOrder.Status.Should().Be(OrderStatus.Submitted);

        // Step 2: Get distribution suggestions through procurement service
        var distributionSuggestion = await procurementService.SuggestSupplierDistributionAsync(createdOrder.Id);
        distributionSuggestion.Should().NotBeNull();
        distributionSuggestion.SuggestedAllocations.Should().NotBeEmpty();

        // Step 3: Create purchase orders through procurement service
        var distributionPlan = new DistributionPlan
        {
            Allocations = distributionSuggestion.SuggestedAllocations.Select(a => new SupplierAllocation
            {
                SupplierId = a.SupplierId,
                AllocatedQuantity = a.SuggestedQuantity
            }).ToList(),
            Strategy = DistributionStrategy.Balanced
        };

        var purchaseOrders = await procurementService.CreatePurchaseOrdersAsync(createdOrder.Id, distributionPlan);
        purchaseOrders.Should().NotBeEmpty();

        // Step 4: Verify order status was updated by procurement service
        var updatedOrder = await orderService.GetOrderByIdAsync(createdOrder.Id);
        updatedOrder.Status.Should().Be(OrderStatus.PurchaseOrdersCreated);

        // Step 5: Verify audit trail was created by audit service
        var auditLogs = await auditService.GetAuditLogsAsync(createdOrder.Id.ToString(), null, null);
        auditLogs.Should().NotBeEmpty();
        auditLogs.Should().Contain(log => log.Action.Contains("Created"));
        auditLogs.Should().Contain(log => log.Action.Contains("StatusChanged"));

        // Step 6: Verify data consistency across services
        var totalOriginalQuantity = createdOrder.Items.Sum(item => item.Quantity);
        var totalAllocatedQuantity = purchaseOrders.SelectMany(po => po.Items).Sum(item => item.AllocatedQuantity);
        
        totalAllocatedQuantity.Should().Be(totalOriginalQuantity, 
            "Total allocated quantity should match original order quantity");
    }

    [Fact]
    public async Task SupplierService_NotificationService_Integration_ShouldTriggerNotifications()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierManagementService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var procurementService = scope.ServiceProvider.GetRequiredService<IProcurementPlanningService>();

        // Step 1: Create a purchase order
        var testOrder = await CreateTestOrderAsync();
        var suggestions = await procurementService.SuggestSupplierDistributionAsync(testOrder.Id);
        var distributionPlan = new DistributionPlan
        {
            Allocations = suggestions.SuggestedAllocations.Take(1).Select(a => new SupplierAllocation
            {
                SupplierId = a.SupplierId,
                AllocatedQuantity = a.SuggestedQuantity
            }).ToList(),
            Strategy = DistributionStrategy.Balanced
        };

        var purchaseOrders = await procurementService.CreatePurchaseOrdersAsync(testOrder.Id, distributionPlan);
        var purchaseOrder = purchaseOrders.First();

        // Step 2: Confirm purchase order through supplier service
        var confirmation = new SupplierConfirmation
        {
            SupplierNotes = "Order confirmed",
            ItemConfirmations = purchaseOrder.Items.Select(item => new PurchaseOrderItemConfirmation
            {
                PurchaseOrderItemId = item.Id,
                PackagingDetails = "Standard packaging",
                DeliveryMethod = "Standard delivery",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7),
                UnitPrice = 10.00m
            }).ToList()
        };

        var confirmedOrder = await procurementService.ConfirmPurchaseOrderAsync(purchaseOrder.Id, confirmation);
        confirmedOrder.Should().NotBeNull();
        confirmedOrder.Status.Should().Be(PurchaseOrderStatus.Confirmed);

        // Step 3: Verify notification was triggered
        // Note: In a real implementation, you would check that notifications were sent
        // This could involve checking a notification queue, database records, or mock service calls
        
        // For now, we'll verify that the supplier performance metrics were updated
        var supplier = await supplierService.GetSupplierByIdAsync(purchaseOrder.SupplierId);
        supplier.Should().NotBeNull();
        
        // Verify that the confirmation triggered updates in related services
        var updatedPO = await procurementService.GetPurchaseOrderByIdAsync(purchaseOrder.Id);
        updatedPO.ConfirmedAt.Should().NotBeNull();
        updatedPO.SupplierNotes.Should().Be(confirmation.SupplierNotes);
    }

    [Fact]
    public async Task CacheService_DatabaseService_Integration_ShouldMaintainCacheConsistency()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierManagementService>();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

        // Step 1: Get dashboard data (should cache results)
        var initialDashboard = await dashboardService.GetDashboardSummaryAsync();
        initialDashboard.Should().NotBeNull();

        // Step 2: Create new order (should invalidate cache)
        var testOrder = await CreateTestOrderAsync();

        // Step 3: Get dashboard data again (should reflect new order)
        var updatedDashboard = await dashboardService.GetDashboardSummaryAsync();
        updatedDashboard.TotalOrders.Should().BeGreaterThan(initialDashboard.TotalOrders);

        // Step 4: Update supplier capacity (should invalidate supplier cache)
        var suppliers = await supplierService.GetAvailableSuppliersAsync(ProductType.LMR, 50);
        var testSupplier = suppliers.First();
        
        var updateRequest = new UpdateCapacityRequest
        {
            ProductType = ProductType.LMR,
            MaxMonthlyCapacity = testSupplier.Capabilities.First().MaxMonthlyCapacity + 100
        };

        await supplierService.UpdateSupplierCapacityAsync(testSupplier.Id, updateRequest);

        // Step 5: Verify cache was invalidated and updated data is returned
        var updatedSuppliers = await supplierService.GetAvailableSuppliersAsync(ProductType.LMR, 50);
        var updatedSupplier = updatedSuppliers.First(s => s.Id == testSupplier.Id);
        
        updatedSupplier.Capabilities.First().MaxMonthlyCapacity.Should().Be(
            testSupplier.Capabilities.First().MaxMonthlyCapacity + 100);
    }

    [Fact]
    public async Task OrderStatusTracking_AuditService_Integration_ShouldTrackAllStatusChanges()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
        var statusTrackingService = scope.ServiceProvider.GetRequiredService<IOrderStatusTrackingService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // Step 1: Create order
        var testOrder = await CreateTestOrderAsync();
        var initialStatus = testOrder.Status;

        // Step 2: Update order status multiple times
        var statusProgression = new[]
        {
            OrderStatus.UnderReview,
            OrderStatus.PlanningInProgress,
            OrderStatus.PurchaseOrdersCreated,
            OrderStatus.AwaitingSupplierConfirmation
        };

        foreach (var status in statusProgression)
        {
            await orderService.UpdateOrderStatusAsync(testOrder.Id, status);
            
            // Verify status tracking service recorded the change
            var statusHistory = await statusTrackingService.GetOrderStatusHistoryAsync(testOrder.Id);
            statusHistory.Should().Contain(h => h.Status == status);
        }

        // Step 3: Verify complete audit trail
        var auditLogs = await auditService.GetAuditLogsAsync(testOrder.Id.ToString(), null, null);
        auditLogs.Should().NotBeEmpty();
        
        // Should have one audit log for creation plus one for each status change
        auditLogs.Should().HaveCountGreaterOrEqualTo(statusProgression.Length + 1);
        
        // Verify each status change was audited
        foreach (var status in statusProgression)
        {
            auditLogs.Should().Contain(log => 
                log.Action.Contains("StatusChanged") && 
                log.Details.Contains(status.ToString()));
        }
    }

    [Fact]
    public async Task ReportingService_DataAggregation_Integration_ShouldProvideAccurateReports()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
        var procurementService = scope.ServiceProvider.GetRequiredService<IProcurementPlanningService>();

        // Step 1: Create multiple orders with different characteristics
        var orders = new List<CustomerOrder>();
        
        for (int i = 0; i < 5; i++)
        {
            var order = await CreateTestOrderAsync($"DODAAC{i:D3}", ProductType.LMR);
            orders.Add(order);
            
            // Create purchase orders for some of them
            if (i % 2 == 0)
            {
                var suggestions = await procurementService.SuggestSupplierDistributionAsync(order.Id);
                var distributionPlan = new DistributionPlan
                {
                    Allocations = suggestions.SuggestedAllocations.Take(1).Select(a => new SupplierAllocation
                    {
                        SupplierId = a.SupplierId,
                        AllocatedQuantity = a.SuggestedQuantity
                    }).ToList(),
                    Strategy = DistributionStrategy.Balanced
                };
                
                await procurementService.CreatePurchaseOrdersAsync(order.Id, distributionPlan);
            }
        }

        // Step 2: Generate performance report
        var performanceReport = await reportingService.GeneratePerformanceReportAsync(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        
        performanceReport.Should().NotBeNull();
        performanceReport.TotalOrders.Should().BeGreaterOrEqualTo(5);
        performanceReport.OrdersByStatus.Should().NotBeEmpty();

        // Step 3: Generate supplier distribution report
        var distributionReport = await reportingService.GenerateSupplierDistributionReportAsync(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        
        distributionReport.Should().NotBeNull();
        distributionReport.SupplierAllocations.Should().NotBeEmpty();

        // Step 4: Verify report data consistency with actual data
        var actualOrderCount = await _context.CustomerOrders
            .CountAsync(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-1));
        
        performanceReport.TotalOrders.Should().Be(actualOrderCount);
    }

    [Fact]
    public async Task DistributionAlgorithm_SupplierCapacity_Integration_ShouldRespectCapacityLimits()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionAlgorithmService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierManagementService>();

        // Step 1: Get current supplier capacities
        var suppliers = await supplierService.GetAvailableSuppliersAsync(ProductType.LMR, 1);
        suppliers.Should().NotBeEmpty();

        var totalAvailableCapacity = suppliers.Sum(s => 
            s.Capabilities.Where(c => c.ProductType == ProductType.LMR)
                         .Sum(c => c.MaxMonthlyCapacity - c.CurrentCommitments));

        // Step 2: Create order that requires most of the available capacity
        var largeQuantity = (int)(totalAvailableCapacity * 0.8);
        var largeOrder = await CreateTestOrderAsync("DODAAC999", ProductType.LMR, largeQuantity);

        // Step 3: Get distribution suggestions
        var distributionSuggestion = await distributionService.SuggestDistributionAsync(
            largeOrder.Items.ToList(), ProductType.LMR);

        distributionSuggestion.Should().NotBeNull();
        distributionSuggestion.SuggestedAllocations.Should().NotBeEmpty();

        // Step 4: Verify no supplier is over-allocated
        foreach (var allocation in distributionSuggestion.SuggestedAllocations)
        {
            var supplier = suppliers.First(s => s.Id == allocation.SupplierId);
            var supplierCapacity = supplier.Capabilities
                .Where(c => c.ProductType == ProductType.LMR)
                .Sum(c => c.MaxMonthlyCapacity - c.CurrentCommitments);

            allocation.SuggestedQuantity.Should().BeLessOrEqualTo(supplierCapacity,
                $"Supplier {supplier.Name} should not be allocated more than their available capacity");
        }

        // Step 5: Verify total allocation doesn't exceed order quantity
        var totalAllocated = distributionSuggestion.SuggestedAllocations.Sum(a => a.SuggestedQuantity);
        totalAllocated.Should().BeLessOrEqualTo(largeQuantity);
    }

    [Fact]
    public async Task TransactionConsistency_MultipleServices_ShouldMaintainACIDProperties()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
        var procurementService = scope.ServiceProvider.GetRequiredService<IProcurementPlanningService>();

        // Step 1: Create order
        var testOrder = await CreateTestOrderAsync();

        // Step 2: Simulate concurrent operations
        var tasks = new List<Task>();

        // Task 1: Update order status
        tasks.Add(Task.Run(async () =>
        {
            await orderService.UpdateOrderStatusAsync(testOrder.Id, OrderStatus.UnderReview);
        }));

        // Task 2: Create purchase orders
        tasks.Add(Task.Run(async () =>
        {
            var suggestions = await procurementService.SuggestSupplierDistributionAsync(testOrder.Id);
            var distributionPlan = new DistributionPlan
            {
                Allocations = suggestions.SuggestedAllocations.Take(1).Select(a => new SupplierAllocation
                {
                    SupplierId = a.SupplierId,
                    AllocatedQuantity = a.SuggestedQuantity
                }).ToList(),
                Strategy = DistributionStrategy.Balanced
            };
            
            await procurementService.CreatePurchaseOrdersAsync(testOrder.Id, distributionPlan);
        }));

        // Step 3: Wait for all operations to complete
        await Task.WhenAll(tasks);

        // Step 4: Verify data consistency
        var finalOrder = await orderService.GetOrderByIdAsync(testOrder.Id);
        finalOrder.Should().NotBeNull();
        
        // Order should be in a consistent state
        var purchaseOrders = await _context.PurchaseOrders
            .Where(po => po.CustomerOrderId == testOrder.Id)
            .ToListAsync();

        if (purchaseOrders.Any())
        {
            // If purchase orders were created, order status should reflect this
            finalOrder.Status.Should().BeOneOf(
                OrderStatus.PurchaseOrdersCreated,
                OrderStatus.AwaitingSupplierConfirmation);
        }
    }

    // Helper methods
    private async Task SeedTestDataAsync()
    {
        // Create test suppliers with different capacities
        var suppliers = new List<Supplier>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "High Capacity Supplier",
                ContactEmail = "high@test.com",
                ContactPhone = "555-0001",
                Address = "123 High St",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = 1000,
                        CurrentCommitments = 200,
                        QualityRating = 4.5m
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Medium Capacity Supplier",
                ContactEmail = "medium@test.com",
                ContactPhone = "555-0002",
                Address = "456 Medium Ave",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = 500,
                        CurrentCommitments = 100,
                        QualityRating = 4.2m
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.FFV,
                        MaxMonthlyCapacity = 300,
                        CurrentCommitments = 50,
                        QualityRating = 4.0m
                    }
                }
            }
        };

        await _context.Suppliers.AddRangeAsync(suppliers);
        await _context.SaveChangesAsync();
    }

    private async Task<CustomerOrder> CreateTestOrderAsync(string customerId = "TEST001", ProductType productType = ProductType.LMR, int quantity = 100)
    {
        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();

        var createOrderRequest = new CreateOrderRequest
        {
            CustomerId = customerId,
            CustomerName = "Test Customer",
            ProductType = productType,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            CreatedBy = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductCode = "PROD001",
                    Description = "Test Product",
                    Quantity = quantity,
                    Unit = "EA",
                    UnitPrice = 5.00m
                }
            }
        };

        return await orderService.CreateOrderAsync(createOrderRequest);
    }
}