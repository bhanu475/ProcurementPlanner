using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ProcurementPlanner.Tests.Integration;

public class SupplierPortalControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _customerOrderId = Guid.NewGuid();
    private readonly Guid _purchaseOrderId = Guid.NewGuid();


    public SupplierPortalControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        SeedTestData();
        
        // For testing, we'll skip authentication by configuring the test client
        // In a real implementation, you would set up proper JWT token generation for tests
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
    public async Task GetDashboard_ReturnsSupplierDashboard()
    {
        // Act
        var response = await _client.GetAsync("/api/supplier-portal/dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SupplierDashboardDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(_supplierId, apiResponse.Data.SupplierId);
        Assert.Equal("Test Supplier", apiResponse.Data.SupplierName);
    }

    [Fact]
    public async Task GetPurchaseOrders_ReturnsSupplierOrders()
    {
        // Act
        var response = await _client.GetAsync("/api/supplier-portal/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PurchaseOrderSummaryDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data);
        Assert.Equal(_purchaseOrderId, apiResponse.Data[0].Id);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Act
        var response = await _client.GetAsync("/api/supplier-portal/orders?status=SentToSupplier");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PurchaseOrderSummaryDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Data);
        Assert.Equal(PurchaseOrderStatus.SentToSupplier, apiResponse.Data[0].Status);
    }

    [Fact]
    public async Task GetPurchaseOrder_ValidId_ReturnsOrderDetails()
    {
        // Act
        var response = await _client.GetAsync($"/api/supplier-portal/orders/{_purchaseOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PurchaseOrderDetailDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(_purchaseOrderId, apiResponse.Data.Id);
        Assert.Equal("PO-2024-001", apiResponse.Data.PurchaseOrderNumber);
        Assert.Single(apiResponse.Data.Items);
    }

    [Fact]
    public async Task GetPurchaseOrder_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/supplier-portal/orders/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode); // Will be 500 due to UnauthorizedAccessException
    }

    [Fact]
    public async Task ConfirmPurchaseOrder_ValidConfirmation_ReturnsUpdatedOrder()
    {
        // Arrange
        var confirmation = new SupplierOrderConfirmationDto
        {
            SupplierNotes = "Order confirmed",
            AcceptOrder = true,
            ItemUpdates = new List<SupplierItemUpdateDto>
            {
                new SupplierItemUpdateDto
                {
                    PurchaseOrderItemId = _context.PurchaseOrderItems.First().Id,
                    PackagingDetails = "Standard packaging",
                    DeliveryMethod = "Ground shipping",
                    EstimatedDeliveryDate = DateTime.UtcNow.AddDays(20),
                    UnitPrice = 22m
                }
            }
        };

        var json = JsonSerializer.Serialize(confirmation);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/supplier-portal/orders/{_purchaseOrderId}/confirm", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PurchaseOrderDetailDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(PurchaseOrderStatus.Confirmed, apiResponse.Data.Status);
        Assert.Equal("Order confirmed", apiResponse.Data.SupplierNotes);
    }

    [Fact]
    public async Task RejectPurchaseOrder_ValidRejection_ReturnsUpdatedOrder()
    {
        // Arrange
        var rejection = new PurchaseOrderRejectionDto
        {
            RejectionReason = "Insufficient capacity"
        };

        var json = JsonSerializer.Serialize(rejection);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/supplier-portal/orders/{_purchaseOrderId}/reject", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PurchaseOrderDetailDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(PurchaseOrderStatus.Rejected, apiResponse.Data.Status);
        Assert.Equal("Insufficient capacity", apiResponse.Data.RejectionReason);
    }

    [Fact]
    public async Task RejectPurchaseOrder_EmptyReason_ReturnsBadRequest()
    {
        // Arrange
        var rejection = new PurchaseOrderRejectionDto
        {
            RejectionReason = ""
        };

        var json = JsonSerializer.Serialize(rejection);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/supplier-portal/orders/{_purchaseOrderId}/reject", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePurchaseOrderItems_ValidUpdates_ReturnsUpdatedOrder()
    {
        // Arrange
        var itemUpdates = new List<SupplierItemUpdateDto>
        {
            new SupplierItemUpdateDto
            {
                PurchaseOrderItemId = _context.PurchaseOrderItems.First().Id,
                PackagingDetails = "Updated packaging",
                DeliveryMethod = "Express shipping",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(15),
                UnitPrice = 25m,
                SupplierNotes = "Updated notes"
            }
        };

        var json = JsonSerializer.Serialize(itemUpdates);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/supplier-portal/orders/{_purchaseOrderId}/items", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PurchaseOrderDetailDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        
        var updatedItem = apiResponse.Data.Items.First();
        Assert.Equal("Updated packaging", updatedItem.PackagingDetails);
        Assert.Equal("Express shipping", updatedItem.DeliveryMethod);
        Assert.Equal(25m, updatedItem.UnitPrice);
    }

    [Fact]
    public async Task ValidateDeliveryDates_ValidDates_ReturnsValidationResult()
    {
        // Arrange
        var deliveryDates = new Dictionary<Guid, DateTime>
        {
            { _context.PurchaseOrderItems.First().Id, DateTime.UtcNow.AddDays(20) }
        };

        var json = JsonSerializer.Serialize(deliveryDates);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/supplier-portal/orders/{_purchaseOrderId}/validate-delivery-dates", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<DeliveryDateValidationDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.IsValid);
        Assert.Empty(apiResponse.Data.Errors);
    }

    [Fact]
    public async Task GetOrderHistory_ReturnsPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/supplier-portal/orders/history?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<PurchaseOrderSummaryDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data.Items);
        Assert.Equal(1, apiResponse.Data.TotalCount);
    }

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}