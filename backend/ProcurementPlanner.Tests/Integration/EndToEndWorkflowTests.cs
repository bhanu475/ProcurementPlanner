using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ProcurementPlanner.Tests.Integration;

/// <summary>
/// End-to-end integration tests that validate complete procurement workflows
/// from customer order submission through supplier confirmation and delivery
/// </summary>
public class EndToEndWorkflowTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private ApplicationDbContext _context;

    public EndToEndWorkflowTests(WebApplicationFactory<Program> factory)
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
        
        // Ensure database is created and clean
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context?.Dispose();
        _client?.Dispose();
    }

    [Fact]
    public async Task CompleteOrderToProcurementWorkflow_ShouldSucceed()
    {
        // Arrange - Get authentication tokens for different roles
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        var supplierToken = await GetAuthTokenAsync("supplier@test.com", UserRole.Supplier);

        // Step 1: Create a customer order
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);
        
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = "DODAAC001",
            CustomerName = "Test Military Base",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(14),
            Notes = "Urgent order for base operations",
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductCode = "PROD001",
                    Description = "Fresh Vegetables",
                    Quantity = 100,
                    Unit = "LB",
                    UnitPrice = 2.50m
                },
                new()
                {
                    ProductCode = "PROD002", 
                    Description = "Fresh Fruits",
                    Quantity = 50,
                    Unit = "LB",
                    UnitPrice = 3.00m
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var orderContent = await orderResponse.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(orderContent, _jsonOptions);
        createdOrder.Should().NotBeNull();
        createdOrder.Id.Should().NotBeEmpty();

        // Step 2: Verify order appears in dashboard
        var dashboardResponse = await _client.GetAsync("/api/order/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
        var dashboard = JsonSerializer.Deserialize<OrderDashboardResponseDto>(dashboardContent, _jsonOptions);
        dashboard.Should().NotBeNull();
        dashboard.TotalOrders.Should().BeGreaterThan(0);

        // Step 3: Get distribution suggestions
        var suggestionsResponse = await _client.GetAsync($"/api/procurement/suggestions/{createdOrder.Id}");
        suggestionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var suggestionsContent = await suggestionsResponse.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<DistributionSuggestionResponse>(suggestionsContent, _jsonOptions);
        suggestions.Should().NotBeNull();
        suggestions.Allocations.Should().NotBeEmpty();

        // Step 4: Create purchase orders based on suggestions
        var createPORequest = new CreatePurchaseOrdersRequest
        {
            CustomerOrderId = createdOrder.Id,
            DistributionPlan = new DistributionPlanDto
            {
                Allocations = suggestions.Allocations.Select(a => new SupplierAllocationDto
                {
                    SupplierId = a.SupplierId,
                    AllocatedQuantity = a.AllocatedQuantity
                }).ToList(),
                Strategy = DistributionStrategy.Balanced
            },
            Notes = "Auto-generated from suggestions"
        };

        var createPOResponse = await _client.PostAsJsonAsync("/api/procurement/purchase-orders", createPORequest, _jsonOptions);
        createPOResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var poContent = await createPOResponse.Content.ReadAsStringAsync();
        var purchaseOrders = JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(poContent, _jsonOptions);
        purchaseOrders.Should().NotBeEmpty();

        // Step 5: Switch to supplier role and confirm purchase orders
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supplierToken);
        
        var testSupplierId = await GetTestSupplierIdAsync();
        var supplierPOsResponse = await _client.GetAsync($"/api/procurement/supplier/{testSupplierId}/purchase-orders");
        supplierPOsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var supplierPOsContent = await supplierPOsResponse.Content.ReadAsStringAsync();
        var supplierPOs = JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(supplierPOsContent, _jsonOptions);
        
        if (supplierPOs.Any())
        {
            var poToConfirm = supplierPOs.First();
            var confirmation = new SupplierConfirmationRequest
            {
                SupplierNotes = "Order confirmed, ready for production",
                ItemConfirmations = poToConfirm.Items.Select(item => new PurchaseOrderItemConfirmationDto
                {
                    PurchaseOrderItemId = item.Id,
                    PackagingDetails = "Standard food-grade packaging",
                    DeliveryMethod = "Refrigerated truck",
                    EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7),
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            var confirmResponse = await _client.PostAsJsonAsync($"/api/procurement/{poToConfirm.Id}/confirm", confirmation, _jsonOptions);
            confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Step 6: Verify order status progression
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);
        
        var updatedOrderResponse = await _client.GetAsync($"/api/order/{createdOrder.Id}");
        updatedOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedOrderContent = await updatedOrderResponse.Content.ReadAsStringAsync();
        var updatedOrder = JsonSerializer.Deserialize<OrderResponseDto>(updatedOrderContent, _jsonOptions);
        updatedOrder.Should().NotBeNull();
        updatedOrder.Status.Should().NotBe(OrderStatus.Submitted);

        // Step 7: Verify audit trail exists
        var auditResponse = await _client.GetAsync($"/api/audit?entityId={createdOrder.Id}");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var auditContent = await auditResponse.Content.ReadAsStringAsync();
        var auditLogs = JsonSerializer.Deserialize<List<AuditLogDto>>(auditContent, _jsonOptions);
        auditLogs.Should().NotBeEmpty();
        auditLogs.Should().Contain(log => log.Action.Contains("Created"));
    }

    [Fact]
    public async Task OrderRejectionWorkflow_ShouldHandleRejectionProperly()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        var supplierToken = await GetAuthTokenAsync("supplier@test.com", UserRole.Supplier);

        // Step 1: Create order and purchase orders (similar to above)
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);
        
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = "DODAAC002",
            CustomerName = "Test Naval Base",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(3), // Short timeline
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductCode = "PROD003",
                    Description = "Organic Produce",
                    Quantity = 200,
                    Unit = "LB",
                    UnitPrice = 4.00m
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(
            await orderResponse.Content.ReadAsStringAsync(), _jsonOptions);

        // Create purchase orders
        var suggestions = await GetDistributionSuggestionsAsync(createdOrder.Id);
        await CreatePurchaseOrdersAsync(createdOrder.Id, suggestions);

        // Step 2: Supplier rejects the order
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supplierToken);
        
        var testSupplierId = await GetTestSupplierIdAsync();
        var supplierPOs = await GetSupplierPurchaseOrdersAsync(testSupplierId);
        
        if (supplierPOs.Any())
        {
            var poToReject = supplierPOs.First();
            var rejection = new RejectPurchaseOrderRequest
            {
                RejectionReason = "Insufficient capacity for short timeline delivery"
            };

            var rejectResponse = await _client.PostAsJsonAsync($"/api/procurement/{poToReject.Id}/reject", rejection, _jsonOptions);
            rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 3: Verify rejection is recorded and order status updated
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);
            
            var updatedOrderResponse = await _client.GetAsync($"/api/order/{createdOrder.Id}");
            var updatedOrder = JsonSerializer.Deserialize<OrderResponseDto>(
                await updatedOrderResponse.Content.ReadAsStringAsync(), _jsonOptions);
            
            // Order should reflect the rejection in its status or require replanning
            updatedOrder.Status.Should().NotBe(OrderStatus.Delivered);
        }
    }

    [Fact]
    public async Task MultiSupplierDistributionWorkflow_ShouldDistributeCorrectly()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);

        // Step 1: Create large order that requires multiple suppliers
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = "DODAAC003",
            CustomerName = "Large Military Installation",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductCode = "PROD004",
                    Description = "Bulk Vegetables",
                    Quantity = 1000, // Large quantity requiring multiple suppliers
                    Unit = "LB",
                    UnitPrice = 1.50m
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(
            await orderResponse.Content.ReadAsStringAsync(), _jsonOptions);

        // Step 2: Get distribution suggestions - should suggest multiple suppliers
        var suggestionsResponse = await _client.GetAsync($"/api/procurement/suggestions/{createdOrder.Id}");
        var suggestions = JsonSerializer.Deserialize<DistributionSuggestionResponse>(
            await suggestionsResponse.Content.ReadAsStringAsync(), _jsonOptions);

        suggestions.SuggestedAllocations.Should().HaveCountGreaterThan(1, 
            "Large order should be distributed across multiple suppliers");

        // Step 3: Create purchase orders with multi-supplier distribution
        var createPORequest = new CreatePurchaseOrdersRequest
        {
            CustomerOrderId = createdOrder.Id,
            DistributionPlan = new DistributionPlanDto
            {
                Allocations = suggestions.SuggestedAllocations.Select(a => new SupplierAllocationDto
                {
                    SupplierId = a.SupplierId,
                    AllocatedQuantity = a.SuggestedQuantity
                }).ToList(),
                Strategy = DistributionStrategy.Balanced
            }
        };

        var createPOResponse = await _client.PostAsJsonAsync("/api/procurement/purchase-orders", createPORequest, _jsonOptions);
        var purchaseOrders = JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(
            await createPOResponse.Content.ReadAsStringAsync(), _jsonOptions);

        // Step 4: Verify total quantities match original order
        var totalAllocated = purchaseOrders.SelectMany(po => po.Items).Sum(item => item.AllocatedQuantity);
        var originalQuantity = createOrderDto.Items.Sum(item => item.Quantity);
        
        totalAllocated.Should().Be(originalQuantity, 
            "Total allocated quantity should match original order quantity");

        // Step 5: Verify each supplier gets appropriate allocation
        purchaseOrders.Should().AllSatisfy(po => 
            po.Items.Should().AllSatisfy(item => 
                item.AllocatedQuantity.Should().BeGreaterThan(0)));
    }

    [Fact]
    public async Task SecurityAndAuthorizationWorkflow_ShouldEnforcePermissions()
    {
        // Test 1: Unauthorized access should be denied
        _client.DefaultRequestHeaders.Authorization = null;
        
        var unauthorizedResponse = await _client.GetAsync("/api/order");
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Test 2: Wrong role access should be forbidden
        var supplierToken = await GetAuthTokenAsync("supplier@test.com", UserRole.Supplier);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supplierToken);
        
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = "DODAAC004",
            CustomerName = "Test Base",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductCode = "PROD005",
                    Description = "Test Product",
                    Quantity = 10,
                    Unit = "EA",
                    UnitPrice = 5.00m
                }
            }
        };

        var forbiddenResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Test 3: Correct role should have access
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);
        
        var authorizedResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        authorizedResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Test 4: Suppliers should only see their own purchase orders
        var createdOrder = JsonSerializer.Deserialize<OrderResponseDto>(
            await authorizedResponse.Content.ReadAsStringAsync(), _jsonOptions);
        
        await CreatePurchaseOrdersFromOrder(createdOrder.Id);
        
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supplierToken);
        
        var testSupplierId = await GetTestSupplierIdAsync();
        var supplierPOsResponse = await _client.GetAsync($"/api/procurement/supplier/{testSupplierId}/purchase-orders");
        supplierPOsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Supplier should not be able to access another supplier's orders
        var otherSupplierId = Guid.NewGuid();
        var otherSupplierPOsResponse = await _client.GetAsync($"/api/procurement/supplier/{otherSupplierId}/purchase-orders");
        otherSupplierPOsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ErrorHandlingAndRecoveryWorkflow_ShouldHandleErrorsGracefully()
    {
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);

        // Test 1: Invalid order data should return validation errors
        var invalidOrderDto = new CreateOrderDto
        {
            CustomerId = "", // Invalid - empty
            CustomerName = "Test Base",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(-1), // Invalid - past date
            Items = new List<CreateOrderItemDto>() // Invalid - empty items
        };

        var invalidResponse = await _client.PostAsJsonAsync("/api/order", invalidOrderDto, _jsonOptions);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorContent = await invalidResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("validation", "Error response should contain validation information");

        // Test 2: Non-existent resource should return 404
        var nonExistentId = Guid.NewGuid();
        var notFoundResponse = await _client.GetAsync($"/api/order/{nonExistentId}");
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Test 3: Invalid distribution plan should be rejected
        var validOrder = await CreateValidTestOrderAsync();
        
        var invalidDistributionRequest = new CreatePurchaseOrdersRequest
        {
            CustomerOrderId = validOrder.Id,
            DistributionPlan = new DistributionPlanDto
            {
                Allocations = new List<SupplierAllocationDto>
                {
                    new()
                    {
                        SupplierId = Guid.NewGuid(), // Non-existent supplier
                        AllocatedQuantity = 1000000 // Excessive quantity
                    }
                },
                Strategy = DistributionStrategy.Balanced
            }
        };

        var invalidDistributionResponse = await _client.PostAsJsonAsync("/api/procurement/purchase-orders", invalidDistributionRequest, _jsonOptions);
        invalidDistributionResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DataConsistencyWorkflow_ShouldMaintainConsistency()
    {
        var plannerToken = await GetAuthTokenAsync("planner@test.com", UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plannerToken);

        // Step 1: Create order and track initial state
        var order = await CreateValidTestOrderAsync();
        var initialOrderQuantity = order.Items.Sum(item => item.Quantity);

        // Step 2: Create purchase orders and verify quantity consistency
        var suggestions = await GetDistributionSuggestionsAsync(order.Id);
        var purchaseOrders = await CreatePurchaseOrdersAsync(order.Id, suggestions);
        
        var totalPOQuantity = purchaseOrders.SelectMany(po => po.Items).Sum(item => item.AllocatedQuantity);
        totalPOQuantity.Should().Be(initialOrderQuantity, 
            "Total purchase order quantity should match original order quantity");

        // Step 3: Verify database consistency
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var dbOrder = await dbContext.CustomerOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        
        var dbPurchaseOrders = await dbContext.PurchaseOrders
            .Include(po => po.Items)
            .Where(po => po.CustomerOrderId == order.Id)
            .ToListAsync();

        dbOrder.Should().NotBeNull();
        dbPurchaseOrders.Should().NotBeEmpty();
        
        var dbTotalQuantity = dbPurchaseOrders.SelectMany(po => po.Items).Sum(item => item.AllocatedQuantity);
        var dbOriginalQuantity = dbOrder.Items.Sum(item => item.Quantity);
        
        dbTotalQuantity.Should().Be(dbOriginalQuantity, 
            "Database should maintain quantity consistency");
    }

    // Helper methods for test setup and execution
    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "planner@test.com",
                Email = "planner@test.com",
                Role = UserRole.LMRPlanner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "supplier@test.com",
                Email = "supplier@test.com",
                Role = UserRole.Supplier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);

        // Create test suppliers
        var suppliers = new List<Supplier>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier 1",
                ContactEmail = "supplier1@test.com",
                ContactPhone = "555-0001",
                Address = "123 Test St, Test City, TC 12345",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = 500,
                        CurrentCommitments = 100,
                        QualityRating = 4.5m
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier 2",
                ContactEmail = "supplier2@test.com",
                ContactPhone = "555-0002",
                Address = "456 Test Ave, Test City, TC 12345",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = 300,
                        CurrentCommitments = 50,
                        QualityRating = 4.2m
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.FFV,
                        MaxMonthlyCapacity = 400,
                        CurrentCommitments = 75,
                        QualityRating = 4.7m
                    }
                }
            }
        };

        await _context.Suppliers.AddRangeAsync(suppliers);
        await _context.SaveChangesAsync();
    }

    private async Task<string> GetAuthTokenAsync(string email, UserRole role)
    {
        // In a real implementation, this would authenticate against your auth service
        // For testing, we'll create a mock JWT token or use a test authentication scheme
        
        var loginRequest = new
        {
            Email = email,
            Password = "password123"
        };

        try
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
                return authResponse?.Token ?? "mock-jwt-token";
            }
        }
        catch
        {
            // If auth fails, return a mock token for testing
        }

        return "mock-jwt-token";
    }

    private async Task<OrderResponseDto> CreateValidTestOrderAsync()
    {
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = "DODAAC999",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductCode = "PROD999",
                    Description = "Test Product",
                    Quantity = 50,
                    Unit = "EA",
                    UnitPrice = 10.00m
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/order", createOrderDto, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OrderResponseDto>(content, _jsonOptions);
    }

    private async Task<DistributionSuggestionResponse> GetDistributionSuggestionsAsync(Guid customerOrderId)
    {
        var response = await _client.GetAsync($"/api/procurement/suggestions/{customerOrderId}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DistributionSuggestionResponse>(content, _jsonOptions);
    }

    private async Task<List<PurchaseOrderResponse>> CreatePurchaseOrdersAsync(Guid customerOrderId, DistributionSuggestionResponse suggestions)
    {
        var createPORequest = new CreatePurchaseOrdersRequest
        {
            CustomerOrderId = customerOrderId,
            DistributionPlan = new DistributionPlanDto
            {
                Allocations = suggestions.Allocations.Select(a => new SupplierAllocationDto
                {
                    SupplierId = a.SupplierId,
                    AllocatedQuantity = a.AllocatedQuantity
                }).ToList(),
                Strategy = DistributionStrategy.Balanced
            }
        };

        var response = await _client.PostAsJsonAsync("/api/procurement/purchase-orders", createPORequest, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(content, _jsonOptions);
    }

    private async Task<List<PurchaseOrderResponse>> GetSupplierPurchaseOrdersAsync(Guid supplierId)
    {
        var response = await _client.GetAsync($"/api/procurement/supplier/{supplierId}/purchase-orders");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<PurchaseOrderResponse>>(content, _jsonOptions);
    }

    private async Task<Guid> GetTestSupplierIdAsync()
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync();
        return supplier?.Id ?? Guid.NewGuid();
    }

    private async Task CreatePurchaseOrdersFromOrder(Guid customerOrderId)
    {
        var suggestions = await GetDistributionSuggestionsAsync(customerOrderId);
        await CreatePurchaseOrdersAsync(customerOrderId, suggestions);
    }
}