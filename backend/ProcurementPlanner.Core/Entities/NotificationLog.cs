using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Entities;

public class NotificationLog : BaseEntity
{
    [Required]
    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public int RetryCount { get; set; } = 0;

    [MaxLength(100)]
    public string? ExternalMessageId { get; set; }

    public string MetadataJson { get; set; } = "{}";

    // Helper property to work with metadata as a dictionary
    public Dictionary<string, string> Metadata
    {
        get => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson) ?? new Dictionary<string, string>();
        set => MetadataJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    // Business logic methods
    public void MarkAsSent(string? externalMessageId = null)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalMessageId = externalMessageId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return Status == NotificationStatus.Failed && RetryCount < maxRetries;
    }
}