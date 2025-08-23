using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly IAuditService _auditService;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRole.LMRPlanner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task LogAsync_WithValidRequest_ShouldCreateAuditLog()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            Action = "CREATE",
            EntityType = "CustomerOrder",
            EntityId = Guid.NewGuid(),
            UserId = _context.Users.First().Id,
            Username = "testuser",
            UserRole = "LMRPlanner",
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent",
            NewValues = new { Name = "Test Order" },
            Result = AuditResult.Success
        };

        // Act
        await _auditService.LogAsync(request);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("CREATE", auditLog.Action);
        Assert.Equal("CustomerOrder", auditLog.EntityType);
        Assert.Equal(request.EntityId, auditLog.EntityId);
        Assert.Equal(request.UserId, auditLog.UserId);
        Assert.Equal("testuser", auditLog.Username);
        Assert.Equal("LMRPlanner", auditLog.UserRole);
        Assert.Equal("192.168.1.1", auditLog.IpAddress);
        Assert.Equal(AuditResult.Success, auditLog.Result);
        Assert.NotNull(auditLog.NewValues);
    }

    [Fact]
    public async Task LogAsync_WithSimpleParameters_ShouldCreateAuditLog()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var oldValues = new { Name = "Old Name" };
        var newValues = new { Name = "New Name" };

        // Act
        await _auditService.LogAsync("UPDATE", "CustomerOrder", entityId, oldValues, newValues, "Test update");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("UPDATE", auditLog.Action);
        Assert.Equal("CustomerOrder", auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal(AuditResult.Success, auditLog.Result);
        Assert.NotNull(auditLog.OldValues);
        Assert.NotNull(auditLog.NewValues);
        Assert.Equal("Test update", auditLog.AdditionalData);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        await SeedAuditLogs(userId);

        var filter = new AuditLogFilter
        {
            Action = "CREATE",
            EntityType = "CustomerOrder",
            UserId = userId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _auditService.GetAuditLogsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
        Assert.All(result.Items, log => 
        {
            Assert.Equal("CREATE", log.Action);
            Assert.Equal("CustomerOrder", log.EntityType);
            Assert.Equal(userId, log.UserId);
        });
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithDateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        await SeedAuditLogs(userId);

        var fromDate = DateTime.UtcNow.AddDays(-1);
        var toDate = DateTime.UtcNow.AddDays(1);

        var filter = new AuditLogFilter
        {
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _auditService.GetAuditLogsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
        Assert.All(result.Items, log => 
        {
            Assert.True(log.Timestamp >= fromDate);
            Assert.True(log.Timestamp <= toDate);
        });
    }

    [Fact]
    public async Task GetEntityAuditTrailAsync_ShouldReturnEntitySpecificLogs()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        var entityId = Guid.NewGuid();
        await SeedAuditLogs(userId, entityId);

        // Act
        var result = await _auditService.GetEntityAuditTrailAsync("CustomerOrder", entityId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, log => 
        {
            Assert.Equal("CustomerOrder", log.EntityType);
            Assert.Equal(entityId, log.EntityId);
        });
    }

    [Fact]
    public async Task GetUserAuditTrailAsync_ShouldReturnUserSpecificLogs()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        await SeedAuditLogs(userId);

        // Act
        var result = await _auditService.GetUserAuditTrailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, log => Assert.Equal(userId, log.UserId));
    }

    [Fact]
    public async Task GenerateAuditReportAsync_ShouldReturnValidReport()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        await SeedAuditLogs(userId);

        var request = new AuditReportRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-1),
            ToDate = DateTime.UtcNow.AddDays(1),
            GroupByAction = true,
            GroupByEntityType = true,
            GroupByUser = true
        };

        // Act
        var result = await _auditService.GenerateAuditReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalActions > 0);
        Assert.True(result.SuccessfulActions >= 0);
        Assert.True(result.FailedActions >= 0);
        Assert.Equal(result.TotalActions, result.SuccessfulActions + result.FailedActions);
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task ExportAuditLogsAsync_WithCsvFormat_ShouldReturnCsvData()
    {
        // Arrange
        var userId = _context.Users.First().Id;
        await SeedAuditLogs(userId);

        var filter = new AuditLogFilter
        {
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = await _auditService.ExportAuditLogsAsync(filter, "csv");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        
        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Timestamp,Action,EntityType", csvContent);
    }

    [Fact]
    public async Task ExportAuditLogsAsync_WithInvalidFormat_ShouldThrowException()
    {
        // Arrange
        var filter = new AuditLogFilter();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _auditService.ExportAuditLogsAsync(filter, "invalid"));
    }

    private async Task SeedAuditLogs(Guid userId, Guid? specificEntityId = null)
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

        // Add some failed actions
        auditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "DELETE",
            EntityType = "CustomerOrder",
            EntityId = Guid.NewGuid(),
            UserId = userId,
            Username = "testuser",
            UserRole = "LMRPlanner",
            IpAddress = "192.168.1.1",
            Result = AuditResult.Failed,
            ErrorMessage = "Access denied",
            Timestamp = DateTime.UtcNow.AddMinutes(-20),
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        });

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}