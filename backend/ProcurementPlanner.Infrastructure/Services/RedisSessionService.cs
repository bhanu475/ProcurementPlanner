using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class RedisSessionService : ISessionService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<RedisSessionService> _logger;

    public RedisSessionService(ICacheService cacheService, ILogger<RedisSessionService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<string> CreateSessionAsync(Guid userId, UserSessionData sessionData)
    {
        var sessionId = Guid.NewGuid().ToString();
        var cacheKey = string.Format(CacheKeys.UserSession, sessionId);
        
        sessionData.CreatedAt = DateTime.UtcNow;
        sessionData.LastAccessedAt = DateTime.UtcNow;
        
        await _cacheService.SetAsync(cacheKey, sessionData, CacheKeys.Expiration.Session);
        
        _logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId);
        return sessionId;
    }

    public async Task<UserSessionData?> GetSessionAsync(string sessionId)
    {
        var cacheKey = string.Format(CacheKeys.UserSession, sessionId);
        var sessionData = await _cacheService.GetAsync<UserSessionData>(cacheKey);
        
        if (sessionData != null && !sessionData.IsExpired)
        {
            // Update last accessed time
            sessionData.LastAccessedAt = DateTime.UtcNow;
            await _cacheService.SetAsync(cacheKey, sessionData, CacheKeys.Expiration.Session);
            
            _logger.LogDebug("Retrieved and refreshed session {SessionId}", sessionId);
            return sessionData;
        }
        
        if (sessionData?.IsExpired == true)
        {
            _logger.LogDebug("Session {SessionId} has expired, removing", sessionId);
            await _cacheService.RemoveAsync(cacheKey);
        }
        
        return null;
    }

    public async Task UpdateSessionAsync(string sessionId, UserSessionData sessionData)
    {
        var cacheKey = string.Format(CacheKeys.UserSession, sessionId);
        sessionData.LastAccessedAt = DateTime.UtcNow;
        
        await _cacheService.SetAsync(cacheKey, sessionData, CacheKeys.Expiration.Session);
        _logger.LogDebug("Updated session {SessionId}", sessionId);
    }

    public async Task RemoveSessionAsync(string sessionId)
    {
        var cacheKey = string.Format(CacheKeys.UserSession, sessionId);
        await _cacheService.RemoveAsync(cacheKey);
        
        _logger.LogInformation("Removed session {SessionId}", sessionId);
    }

    public async Task RemoveAllUserSessionsAsync(Guid userId)
    {
        // This is a simplified implementation - in production, you might want to maintain
        // a separate index of user sessions for more efficient cleanup
        var pattern = CacheKeys.Patterns.AllSessions;
        await _cacheService.RemoveByPatternAsync(pattern);
        
        _logger.LogInformation("Removed all sessions for user {UserId}", userId);
    }

    public async Task RefreshSessionAsync(string sessionId)
    {
        var sessionData = await GetSessionAsync(sessionId);
        if (sessionData != null)
        {
            await UpdateSessionAsync(sessionId, sessionData);
        }
    }

    public async Task<List<UserSessionData>> GetUserSessionsAsync(Guid userId)
    {
        // This is a simplified implementation - in production, you would maintain
        // a separate index for user sessions
        _logger.LogWarning("GetUserSessionsAsync is not efficiently implemented - consider maintaining a user session index");
        return new List<UserSessionData>();
    }

    public async Task<bool> IsSessionValidAsync(string sessionId)
    {
        var sessionData = await GetSessionAsync(sessionId);
        return sessionData != null && !sessionData.IsExpired;
    }
}