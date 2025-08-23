using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using Xunit;

namespace ProcurementPlanner.Tests.Integration;

public class AuditControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuditControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithValidAuthentication_ShouldReturnAuditLogs()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedAuditLogsAsync(context, user.Id);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<AuditLogDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.Items.Count > 0);
    }

    [Fact]
    public async Task GetAuditLogs_WithSupplierRole_ShouldReturnForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Supplier);
        var token = GenerateJwtToken(user);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedAuditLogsAsync(context, user.Id);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit?action=CREATE&entityType=CustomerOrder");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<AuditLogDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.All(apiResponse.Data.Items, log => 
        {
            Assert.Equal("CREATE", log.Action);
            Assert.Equal("CustomerOrder", log.EntityType);
        });
    }

    [Fact]
    public async Task GetEntityAuditTrail_WithValidParameters_ShouldReturnEntityAuditTrail()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.LMRPlanner);
        var token = GenerateJwtToken(user);
        
        var entityId = Guid.NewGuid();
        await SeedAuditLogsAsync(context, user.Id, entityId);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/audit/entity/CustomerOrder/{entityId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<AuditLogDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.All(apiResponse.Data, log => 
        {
            Assert.Equal("CustomerOrder", log.EntityType);
            Assert.Equal(entityId, log.EntityId);
        });
    }

    [Fact]
    public async Task GetUserAuditTrail_WithValidUserId_ShouldReturnUserAuditTrail()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedAuditLogsAsync(context, user.Id);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/audit/user/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<AuditLogDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.All(apiResponse.Data, log => Assert.Equal(user.Id, log.UserId));
    }

    [Fact]
    public async Task GenerateAuditReport_WithValidRequest_ShouldReturnReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedAuditLogsAsync(context, user.Id);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new AuditReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-1),
            ToDate = DateTime.UtcNow.AddDays(1),
            GroupByAction = true,
            GroupByEntityType = true,
            GroupByUser = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit/report", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuditReportDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.TotalActions > 0);
        Assert.NotNull(apiResponse.Data.Summary);
        Assert.NotNull(apiResponse.Data.Details);
    }

    [Fact]
    public async Task ExportAuditLogs_WithCsvFormat_ShouldReturnCsvFile()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await SeedTestUserAsync(context, UserRole.Administrator);
        var token = GenerateJwtToken(user);
        
        await SeedAuditLogsAsync(context, user.Id);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var filter = new AuditLogFilterDto
        {
            Page = 1,
            PageSize = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit/export?format=csv", filter);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Timestamp,Action,EntityType", content);
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

    private async Task SeedAuditLogsAsync(ApplicationDbContext context, Guid userId, Guid? specificEntityId = null)
    {
        var auditLogs = new List<AuditLog>();

        for (int i = 0; i < 5; i++)
        {
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "CREATE",
                EntityType = "CustomerOrder",
                EntityId = specificEntityId ?? Guid.NewGuid(),
                UserId = userId,
                Username = "testuser",
                UserRole = "LMRPlanner",
                IpAddress = "192.168.1.1",
                Result = AuditResult.Success,
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        for (int i = 0; i < 3; i++)
        {
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "UPDATE",
                EntityType = "Supplier",
                EntityId = Guid.NewGuid(),
                UserId = userId,
                Username = "testuser",
                UserRole = "LMRPlanner",
                IpAddress = "192.168.1.1",
                Result = AuditResult.Success,
                Timestamp = DateTime.UtcNow.AddMinutes(-i - 10),
                CreatedAt = DateTime.UtcNow.AddMinutes(-i - 10)
            });
        }

        context.AuditLogs.AddRange(auditLogs);
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