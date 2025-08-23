using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditLogRequest request);
    Task LogAsync(string action, string entityType, Guid? entityId = null, object? oldValues = null, object? newValues = null, string? additionalData = null);
    Task<PagedResult<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter);
    Task<List<AuditLog>> GetEntityAuditTrailAsync(string entityType, Guid entityId);
    Task<List<AuditLog>> GetUserAuditTrailAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<AuditReport> GenerateAuditReportAsync(AuditReportRequest request);
    Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "excel");
}

public class AuditLogRequest
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    public string? AdditionalData { get; set; }
    public AuditResult Result { get; set; } = AuditResult.Success;
    public string? ErrorMessage { get; set; }
}

public class AuditLogFilter
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public AuditResult? Result { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "Timestamp";
    public bool SortDescending { get; set; } = true;
}

public class AuditReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public Guid? UserId { get; set; }
    public bool IncludeSuccessfulActions { get; set; } = true;
    public bool IncludeFailedActions { get; set; } = true;
    public bool GroupByUser { get; set; } = false;
    public bool GroupByAction { get; set; } = false;
    public bool GroupByEntityType { get; set; } = false;
}

public class AuditReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalActions { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public List<AuditSummary> Summary { get; set; } = new();
    public List<AuditLog> Details { get; set; } = new();
}

public class AuditSummary
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}