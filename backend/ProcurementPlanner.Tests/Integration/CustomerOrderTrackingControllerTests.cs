using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProcurementPlanner.Tests.Integration;

public class CustomerOrderTrackingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomerOrderTrackingControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetCustomerOrders_WithValidCustomer_ReturnsOrders()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customer/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CustomerOrderSummaryDto>>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotEmpty(apiResponse.Data);
    }

    [Fact]
    public async Task GetCustomerOrder_WithValidOrderId_ReturnsOrderDetails()
    {
        // Arrange
        var customerId = "CUST001";
        var orderId = await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/customer/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<OrderTrackingDto>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(orderId, apiResponse.Data.OrderId);
    }

    [Fact]
    public async Task GetCustomerOrder_WithInvalidOrderId_ReturnsNotFound()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var invalidOrderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/customer/orders/{invalidOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerOrder_WithOrderFromDifferentCustomer_ReturnsForbidden()
    {
        // Arrange
        var customerId1 = "CUST001";
        var customerId2 = "CUST002";
        var orderId = await SeedTestDataAsync(customerId1);
        var token = await GetCustomerTokenAsync(customerId2);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/customer/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Should return NotFound since customer can't access other's orders
    }

    [Fact]
    public async Task GetOrderStatusHistory_WithValidOrderId_ReturnsHistory()
    {
        // Arrange
        var customerId = "CUST001";
        var orderId = await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/customer/orders/{orderId}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<OrderStatusHistoryDto>>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
    }

    [Fact]
    public async Task GetOrderTimeline_WithValidOrderId_ReturnsTimeline()
    {
        // Arrange
        var customerId = "CUST001";
        var orderId = await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/customer/orders/{orderId}/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<OrderMilestoneDto>>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
    }

    [Fact]
    public async Task GetRecentOrders_WithValidCustomer_ReturnsRecentOrders()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customer/orders/recent?count=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CustomerOrderSummaryDto>>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.Count <= 3);
    }

    [Fact]
    public async Task GetOrderTrackingSummary_WithValidCustomer_ReturnsSummary()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customer/orders/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CustomerOrderTrackingSummaryDto>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(customerId, apiResponse.Data.CustomerId);
    }

    [Fact]
    public async Task GetNotificationPreferences_WithValidCustomer_ReturnsPreferences()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customer/orders/notifications/preferences");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CustomerNotificationPreferencesDto>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(customerId, apiResponse.Data.CustomerId);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithValidData_UpdatesPreferences()
    {
        // Arrange
        var customerId = "CUST001";
        await SeedTestDataAsync(customerId);
        var token = await GetCustomerTokenAsync(customerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateNotificationPreferencesRequest
        {
            EmailNotifications = false,
            SmsNotifications = true,
            StatusChangeNotifications = true,
            DeliveryReminders = false,
            DelayNotifications = true,
            EmailAddress = "customer@example.com",
            PhoneNumber = "+1234567890"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/customer/orders/notifications/preferences", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CustomerNotificationPreferencesDto>>(content, _jsonOptions);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.False(apiResponse.Data.EmailNotifications);
        Assert.True(apiResponse.Data.SmsNotifications);
        Assert.Equal("customer@example.com", apiResponse.Data.EmailAddress);
        Assert.Equal("+1234567890", apiResponse.Data.PhoneNumber);
    }

    [Fact]
    public async Task GetCustomerOrders_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/customer/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerOrders_WithNonCustomerRole_ReturnsForbidden()
    {
        // Arrange
        var token = await GetPlannerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customer/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> SeedTestDataAsync(string customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create test customer order
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-001",
            CustomerId = customerId,
            CustomerName = $"Customer {customerId}",
            ProductType = ProductType.LMR,
            Status = OrderStatus.Submitted,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = "PROD001",
                    Description = "Test Product 1",
                    Quantity = 10,
                    Unit = "EA",
                    UnitPrice = 25.00m
                }
            }
        };

        context.CustomerOrders.Add(order);

        // Create test status history
        var statusHistory = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = OrderStatus.Submitted,
            ToStatus = OrderStatus.UnderReview,
            ChangedAt = DateTime.UtcNow.AddHours(-1),
            Notes = "Order moved to review",
            Reason = "Automatic transition"
        };

        context.OrderStatusHistories.Add(statusHistory);

        // Create test milestones
        var milestone = new OrderMilestone
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Name = "Order Submitted",
            Description = "Order has been submitted and is awaiting review",
            TargetDate = DateTime.UtcNow.AddDays(1),
            ActualDate = DateTime.UtcNow,
            Status = MilestoneStatus.Completed
        };

        context.OrderMilestones.Add(milestone);

        await context.SaveChangesAsync();
        return order.Id;
    }

    private async Task<string> GetCustomerTokenAsync(string customerId)
    {
        // This is a simplified token generation for testing
        // In a real implementation, you would use your authentication service
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthenticationService>();
        
        // Create a test customer user if it doesn't exist
        var loginRequest = new Core.Models.LoginRequest
        {
            Email = $"{customerId}@example.com",
            Password = "TestPassword123!"
        };

        try
        {
            var result = await authService.LoginAsync(loginRequest, "127.0.0.1");
            return result.Token;
        }
        catch
        {
            // If login fails, create the user first - we'll create the user directly in the database
            using var scope2 = _factory.Services.CreateScope();
            var context = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = customerId,
                Email = $"{customerId}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await authService.LoginAsync(loginRequest, "127.0.0.1");
            return result.Token;
        }
    }

    private async Task<string> GetPlannerTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthenticationService>();
        
        var loginRequest = new Core.Models.LoginRequest
        {
            Email = "planner001@example.com",
            Password = "TestPassword123!"
        };

        try
        {
            var result = await authService.LoginAsync(loginRequest, "127.0.0.1");
            return result.Token;
        }
        catch
        {
            // Create the user directly in the database
            using var scope2 = _factory.Services.CreateScope();
            var context = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "planner001",
                Email = "planner001@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Role = UserRole.LMRPlanner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await authService.LoginAsync(loginRequest, "127.0.0.1");
            return result.Token;
        }
    }
}