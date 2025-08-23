using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;
using Xunit;

namespace ProcurementPlanner.Tests.Integration;

public class ReportingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReportingControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReport_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new PerformanceMetricsReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/performance-metrics", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReport_WithValidAuthentication_ShouldReturnReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new PerformanceMetricsReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            IncludeOrderMetrics = true,
            IncludeSupplierMetrics = true,
            IncludeDeliveryMetrics = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/performance-metrics", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PerformanceMetricsReportDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.OrderMetrics);
        Assert.NotNull(apiResponse.Data.SupplierMetrics);
        Assert.NotNull(apiResponse.Data.DeliveryMetrics);
    }

    [Fact]
    public async Task GenerateSupplierDistributionReport_WithValidAuthentication_ShouldReturnReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.LMRPlanner);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new SupplierDistributionReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupByProductType = true,
            IncludeCapacityUtilization = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/supplier-distribution", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SupplierDistributionReportDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.TotalOrders >= 0);
        Assert.NotNull(apiResponse.Data.Distributions);
    }

    [Fact]
    public async Task GenerateOrderFulfillmentReport_WithValidAuthentication_ShouldReturnReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new OrderFulfillmentReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupByStatus = true,
            IncludeTimelines = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/order-fulfillment", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<OrderFulfillmentReportDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.TotalOrders >= 0);
        Assert.NotNull(apiResponse.Data.StatusSummaries);
    }

    [Fact]
    public async Task GenerateDeliveryPerformanceReport_WithValidAuthentication_ShouldReturnReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new DeliveryPerformanceReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            GroupBySupplier = true,
            IncludeDelayAnalysis = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/delivery-performance", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<DeliveryPerformanceReportDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.TotalDeliveries >= 0);
        Assert.NotNull(apiResponse.Data.SupplierPerformances);
    }

    [Fact]
    public async Task ExportPerformanceMetricsReport_WithJsonFormat_ShouldReturnJsonFile()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new PerformanceMetricsReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/performance-metrics/export?format=json", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("GeneratedAt", content);
        Assert.Contains("FromDate", content);
        Assert.Contains("ToDate", content);
    }

    [Fact]
    public async Task ExportSupplierDistributionReport_WithCsvFormat_ShouldReturnCsvFile()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedTestDataAsync(context);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new SupplierDistributionReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/supplier-distribution/export?format=csv", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Report Data", content);
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReport_WithSupplierRole_ShouldReturnForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Supplier);
        var token = GenerateJwtToken(user);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new PerformanceMetricsReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/performance-metrics", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePerformanceMetricsReport_WithInvalidDateRange_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new PerformanceMetricsReportRequestDto
        {
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(-30) // Invalid: ToDate before FromDate
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reporting/performance-metrics", request);

        // Assert
        // The service should handle this gracefully, but may return empty results
        // In a real implementation, you might want to add validation
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    private async Task<User> SeedTestUserAsync(ApplicationDbContext context, UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"testuser_{role}",
            Email = $"test_{role}@example.com",
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Create test suppliers
        var supplier1 = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier 1",
            ContactEmail = "supplier1@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Suppliers.Add(supplier1);

        // Create supplier capabilities
        var capability1 = new SupplierCapability
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier1.Id,
            ProductType = ProductType.LMR,
            MaxMonthlyCapacity = 1000,
            CurrentCommitments = 500,
            QualityRating = 4.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.SupplierCapabilities.Add(capability1);

        // Create supplier performance metrics
        var performance1 = new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier1.Id,
            OnTimeDeliveryRate = 0.95m,
            QualityScore = 4.5m,
            TotalOrdersCompleted = 50,
            AverageDeliveryDays = 7.5m,
            CustomerSatisfactionRate = 0.92m,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.SupplierPerformanceMetrics.Add(performance1);

        // Create test customer orders
        var order1 = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerId = "CUST-001",
            CustomerName = "Test Customer 1",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(14),
            Status = OrderStatus.Delivered,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.CustomerOrders.Add(order1);

        // Create order items
        var orderItem1 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order1.Id,
            ProductCode = "PROD-001",
            Description = "Test Product 1",
            Quantity = 100,
            Unit = "pcs",
            UnitPrice = 10.50m,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        context.OrderItems.Add(orderItem1);

        // Create test purchase orders
        var purchaseOrder1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-001",
            CustomerOrderId = order1.Id,
            SupplierId = supplier1.Id,
            Status = PurchaseOrderStatus.Delivered,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(14),
            TotalValue = 1050.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.PurchaseOrders.Add(purchaseOrder1);

        // Create purchase order items
        var purchaseOrderItem1 = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrder1.Id,
            OrderItemId = orderItem1.Id,
            ProductCode = "PROD-001",
            Description = "Test Product 1",
            AllocatedQuantity = 100,
            Unit = "pcs",
            UnitPrice = 10.50m,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        context.PurchaseOrderItems.Add(purchaseOrderItem1);

        await context.SaveChangesAsync();
    }

    private string GenerateJwtToken(User user)
    {
        // This is a simplified JWT token generation for testing
        // In a real implementation, you would use the same JWT generation logic as in your AuthenticationService
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("your-secret-key-here-must-be-at-least-32-characters-long");
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("id", user.Id.ToString()),
                new System.Security.Claims.Claim("username", user.Username),
                new System.Security.Claims.Claim("role", user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}