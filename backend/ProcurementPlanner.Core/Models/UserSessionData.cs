using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Models;

public class UserSessionData
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    public bool IsExpired => DateTime.UtcNow > LastAccessedAt.Add(TimeSpan.FromHours(8));
}