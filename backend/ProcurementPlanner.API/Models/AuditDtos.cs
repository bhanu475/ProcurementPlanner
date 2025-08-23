using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Models;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AdditionalData { get; set; }
    public AuditResult Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogFilterDto
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

public class AuditReportRequestDto
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
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

public class AuditReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalActions { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public List<AuditSummaryDto> Summary { get; set; } = new();
    public List<AuditLogDto> Details { get; set; } = new();
}

public class AuditSummaryDto
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}