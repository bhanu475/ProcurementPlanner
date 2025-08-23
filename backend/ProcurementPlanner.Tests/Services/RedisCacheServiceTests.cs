using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Services;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<IServer> _mockServer;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockServer = new Mock<IServer>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();

        _mockConnectionMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _cacheService = new RedisCacheService(_mockConnectionMultiplexer.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedObject()
    {
        // Arrange
        var testData = new OrderDashboardSummary 
        { 
            TotalOrders = 100, 
            OverdueOrders = 25,
            TotalValue = 5000m
        };
        var serializedData = JsonSerializer.Serialize(testData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var key = "test_key";

        _mockDatabase.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedData));

        // Act
        var result = await _cacheService.GetAsync<OrderDashboardSummary>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.TotalOrders, result.TotalOrders);
        Assert.Equal(testData.OverdueOrders, result.OverdueOrders);
        Assert.Equal(testData.TotalValue, result.TotalValue);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "nonexistent_key";
        _mockDatabase.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<OrderDashboardSummary>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WithValidData_CallsRedisStringSet()
    {
        // Arrange
        var testData = new OrderDashboardSummary { TotalOrders = 100, OverdueOrders = 25 };
        var key = "test_key";
        var expiration = TimeSpan.FromMinutes(30);

        _mockDatabase.Setup(x => x.StringSetAsync(key, It.IsAny<RedisValue>(), expiration, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetAsync(key, testData, expiration);

        // Assert
        _mockDatabase.Verify(x => x.StringSetAsync(key, It.IsAny<RedisValue>(), expiration, false, When.Always, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_CallsRedisKeyDelete()
    {
        // Arrange
        var key = "test_key";
        _mockDatabase.Setup(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var key = "test_key";
        _mockDatabase.Setup(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var key = "nonexistent_key";
        _mockDatabase.Setup(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_WithValidKey_CallsKeyExpire()
    {
        // Arrange
        var key = "test_key";
        var expiration = TimeSpan.FromMinutes(30);

        _mockDatabase.Setup(x => x.KeyExpireAsync(key, expiration, ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _cacheService.RefreshAsync(key, expiration);

        // Assert
        _mockDatabase.Verify(x => x.KeyExpireAsync(key, expiration, ExpireWhen.Always, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithValidPattern_CallsServerKeysAndKeyDelete()
    {
        // Arrange
        var pattern = "test_*";
        var keys = new RedisKey[] { "test_1", "test_2" };
        var endpoint = new IPEndPoint(IPAddress.Loopback, 6379);

        _mockConnectionMultiplexer.Setup(x => x.GetEndPoints(It.IsAny<bool>()))
            .Returns(new EndPoint[] { endpoint });
        _mockConnectionMultiplexer.Setup(x => x.GetServer(endpoint, It.IsAny<object>()))
            .Returns(_mockServer.Object);
        _mockServer.Setup(x => x.Keys(It.IsAny<int>(), pattern, It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys.AsEnumerable());
        _mockDatabase.Setup(x => x.KeyDeleteAsync(keys, It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);

        // Act
        await _cacheService.RemoveByPatternAsync(pattern);

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync(keys, It.IsAny<CommandFlags>()), Times.Once);
    }
}