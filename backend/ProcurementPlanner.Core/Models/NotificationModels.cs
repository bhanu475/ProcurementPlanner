namespace ProcurementPlanner.Core.Models;

public enum NotificationType
{
    Email = 1,
    SMS = 2,
    InApp = 3,
    Push = 4
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Cancelled = 5
}

public class EmailNotificationRequest
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<NotificationAttachment>? Attachments { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, string>? TemplateData { get; set; }
    public string? TemplateName { get; set; }
}

public class SmsNotificationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, string>? TemplateData { get; set; }
    public string? TemplateName { get; set; }
}

public class BulkNotificationRequest
{
    public List<string> Recipients { get; set; } = new();
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

public class NotificationAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}

public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> RequiredParameters { get; set; } = new();
}

public class CreateNotificationTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> RequiredParameters { get; set; } = new();
}

public class UpdateNotificationTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? RequiredParameters { get; set; }
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public DateTime SentAt { get; set; }
}

public class NotificationLog
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public NotificationPriority Priority { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ExternalMessageId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}