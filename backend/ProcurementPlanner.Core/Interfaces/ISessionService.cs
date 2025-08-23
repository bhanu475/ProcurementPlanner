using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface ISessionService
{
    /// <summary>
    /// Creates a new user session
    /// </summary>
    Task<string> CreateSessionAsync(Guid userId, UserSessionData sessionData);

    /// <summary>
    /// Gets user session data
    /// </summary>
    Task<UserSessionData?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Updates user session data
    /// </summary>
    Task UpdateSessionAsync(string sessionId, UserSessionData sessionData);

    /// <summary>
    /// Removes a user session
    /// </summary>
    Task RemoveSessionAsync(string sessionId);

    /// <summary>
    /// Removes all sessions for a user
    /// </summary>
    Task RemoveAllUserSessionsAsync(Guid userId);

    /// <summary>
    /// Refreshes session expiration
    /// </summary>
    Task RefreshSessionAsync(string sessionId);

    /// <summary>
    /// Gets all active sessions for a user
    /// </summary>
    Task<List<UserSessionData>> GetUserSessionsAsync(Guid userId);

    /// <summary>
    /// Validates if a session is active and valid
    /// </summary>
    Task<bool> IsSessionValidAsync(string sessionId);
}