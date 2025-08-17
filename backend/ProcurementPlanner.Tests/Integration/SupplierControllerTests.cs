using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Tests.Integration;

public class SupplierControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SupplierControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSuppliers_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/supplier");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSuppliers_WithValidAuth_ReturnsSuppliers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Seed test data
        var supplier = CreateTestSupplier("Test Supplier");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        // Add auth header (mock JWT token)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/supplier");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<SupplierResponse>>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Contains(apiResponse.Data, s => s.Name == "Test Supplier");

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSupplier_WithValidId_ReturnsSupplier()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Test Supplier");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.GetAsync($"/api/supplier/{supplier.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SupplierResponse>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("Test Supplier", apiResponse.Data.Name);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSupplier_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/supplier/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSupplier_WithValidData_ReturnsCreated()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var request = new CreateSupplierRequest
        {
            Name = "New Test Supplier",
            ContactEmail = "test@newsupplier.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test Street",
            ContactPersonName = "John Doe",
            Capabilities = new List<CreateSupplierCapabilityRequest>
            {
                new CreateSupplierCapabilityRequest
                {
                    ProductType = ProductType.LMR,
                    MaxMonthlyCapacity = 1000,
                    CurrentCommitments = 500,
                    QualityRating = 4.0m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/supplier", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SupplierResponse>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("New Test Supplier", apiResponse.Data.Name);
        Assert.Single(apiResponse.Data.Capabilities);

        // Cleanup
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == apiResponse.Data.Id);
        if (createdSupplier != null)
        {
            context.Suppliers.Remove(createdSupplier);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateSupplier_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var request = new CreateSupplierRequest
        {
            Name = "", // Invalid - empty name
            ContactEmail = "invalid-email", // Invalid email format
            ContactPhone = "123-456-7890",
            Address = "123 Test Street",
            Capabilities = new List<CreateSupplierCapabilityRequest>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/supplier", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_WithValidData_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Original Name");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var request = new UpdateSupplierRequest
        {
            Name = "Updated Name",
            ContactEmail = supplier.ContactEmail,
            ContactPhone = supplier.ContactPhone,
            Address = supplier.Address,
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/supplier/{supplier.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SupplierResponse>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("Updated Name", apiResponse.Data.Name);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateSupplierCapacity_WithValidData_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Test Supplier");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var request = new UpdateSupplierCapacityRequest
        {
            MaxMonthlyCapacity = 2000,
            CurrentCommitments = 800,
            QualityRating = 4.5m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/supplier/{supplier.Id}/capacity/{ProductType.LMR}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SupplierResponse>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        
        var capability = apiResponse.Data.Capabilities.First(c => c.ProductType == ProductType.LMR);
        Assert.Equal(2000, capability.MaxMonthlyCapacity);
        Assert.Equal(800, capability.CurrentCommitments);
        Assert.Equal(1200, capability.AvailableCapacity);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvailableSuppliers_WithValidRequest_ReturnsSuppliers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Available Supplier");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        var request = new AvailableSuppliersRequest
        {
            ProductType = ProductType.LMR,
            RequiredCapacity = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/supplier/available", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<SupplierResponse>>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task DeactivateSupplier_WithValidId_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Test Supplier");
        supplier.IsActive = true;
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.DeleteAsync($"/api/supplier/{supplier.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);

        // Verify supplier is deactivated
        var updatedSupplier = await context.Suppliers.FindAsync(supplier.Id);
        Assert.NotNull(updatedSupplier);
        Assert.False(updatedSupplier.IsActive);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ActivateSupplier_WithValidId_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier = CreateTestSupplier("Test Supplier");
        supplier.IsActive = false;
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.PostAsync($"/api/supplier/{supplier.Id}/activate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);

        // Verify supplier is activated
        var updatedSupplier = await context.Suppliers.FindAsync(supplier.Id);
        Assert.NotNull(updatedSupplier);
        Assert.True(updatedSupplier.IsActive);

        // Cleanup
        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSupplierSummary_ReturnsCorrectStatistics()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var supplier1 = CreateTestSupplier("Active Supplier 1");
        var supplier2 = CreateTestSupplier("Active Supplier 2");
        var supplier3 = CreateTestSupplier("Inactive Supplier");
        supplier3.IsActive = false;

        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/supplier/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<SupplierSummaryResponse>>(content);
        
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(3, apiResponse.Data.TotalSuppliers);
        Assert.Equal(2, apiResponse.Data.ActiveSuppliers);
        Assert.Equal(1, apiResponse.Data.InactiveSuppliers);

        // Cleanup
        context.Suppliers.RemoveRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync();
    }

    private static Supplier CreateTestSupplier(string name)
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactEmail = $"{name.Replace(" ", "").ToLower()}@test.com",
            ContactPhone = "123-456-7890",
            Address = "123 Test Street",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Capabilities = new List<SupplierCapability>
            {
                new SupplierCapability
                {
                    Id = Guid.NewGuid(),
                    ProductType = ProductType.LMR,
                    MaxMonthlyCapacity = 1000,
                    CurrentCommitments = 500,
                    QualityRating = 4.0m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };
    }
}