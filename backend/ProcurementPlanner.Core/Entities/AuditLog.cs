using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class AuditLog : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(50)]
    public string? UserRole { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(1000)]
    public string? AdditionalData { get; set; }

    public AuditResult Result { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; }

    // Navigation properties
    public User? User { get; set; }
}

public enum AuditResult
{
    Success,
    Failed,
    Unauthorized,
    ValidationError
}