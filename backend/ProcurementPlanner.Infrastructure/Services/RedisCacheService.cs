using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ProcurementPlanner.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(key, serializedValue, expiration);
            
            _logger.LogDebug("Cached value set for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            var keyArray = keys.ToArray();
            if (keyArray.Length > 0)
            {
                await _database.KeyDeleteAsync(keyArray);
                _logger.LogDebug("Removed {Count} cached values matching pattern: {Pattern}", keyArray.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
    {
        var result = new Dictionary<string, T?>();
        
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            
            for (int i = 0; i < redisKeys.Length; i++)
            {
                var key = redisKeys[i].ToString();
                var value = values[i];
                
                if (value.HasValue)
                {
                    try
                    {
                        result[key] = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Error deserializing cached value for key: {Key}", key);
                        result[key] = null;
                    }
                }
                else
                {
                    result[key] = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple cached values");
        }
        
        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var tasks = keyValuePairs.Select(async kvp =>
            {
                var serializedValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                await _database.StringSetAsync(kvp.Key, serializedValue, expiration);
            });
            
            await Task.WhenAll(tasks);
            _logger.LogDebug("Set {Count} cached values with expiration: {Expiration}", keyValuePairs.Count, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple cached values");
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiration)
    {
        try
        {
            await _database.KeyExpireAsync(key, expiration);
            _logger.LogDebug("Refreshed expiration for key: {Key} to {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing expiration for key: {Key}", key);
        }
    }
}