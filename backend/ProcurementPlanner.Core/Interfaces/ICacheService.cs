using System.Text.Json;

namespace ProcurementPlanner.Core.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in cache with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes multiple cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets multiple cached values by keys
    /// </summary>
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;

    /// <summary>
    /// Sets multiple values in cache
    /// </summary>
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Refreshes the expiration time of a cached item
    /// </summary>
    Task RefreshAsync(string key, TimeSpan expiration);
}