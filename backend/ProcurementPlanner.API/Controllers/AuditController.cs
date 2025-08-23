using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        try
        {
            var auditFilter = new AuditLogFilter
            {
                Action = filter.Action,
                EntityType = filter.EntityType,
                EntityId = filter.EntityId,
                UserId = filter.UserId,
                Username = filter.Username,
                UserRole = filter.UserRole,
                Result = filter.Result,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                IpAddress = filter.IpAddress,
                Page = filter.Page,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending
            };

            var result = await _auditService.GetAuditLogsAsync(auditFilter);
            
            var auditLogDtos = result.Items.Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserId = a.UserId,
                Username = a.Username,
                UserRole = a.UserRole,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AdditionalData = a.AdditionalData,
                Result = a.Result,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp
            }).ToList();

            var pagedResult = new PagedResult<AuditLogDto>
            {
                Items = auditLogDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

            return Ok(ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(pagedResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, ApiResponse<PagedResult<AuditLogDto>>.ErrorResponse("Failed to retrieve audit logs"));
        }
    }

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetEntityAuditTrail(string entityType, Guid entityId)
    {
        try
        {
            var auditLogs = await _auditService.GetEntityAuditTrailAsync(entityType, entityId);
            
            var auditLogDtos = auditLogs.Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserId = a.UserId,
                Username = a.Username,
                UserRole = a.UserRole,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AdditionalData = a.AdditionalData,
                Result = a.Result,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp
            }).ToList();

            return Ok(ApiResponse<List<AuditLogDto>>.SuccessResponse(auditLogDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity audit trail for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, ApiResponse<List<AuditLogDto>>.ErrorResponse("Failed to retrieve entity audit trail"));
        }
    }

    /// <summary>
    /// Get audit trail for a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetUserAuditTrail(Guid userId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var auditLogs = await _auditService.GetUserAuditTrailAsync(userId, fromDate, toDate);
            
            var auditLogDtos = auditLogs.Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserId = a.UserId,
                Username = a.Username,
                UserRole = a.UserRole,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AdditionalData = a.AdditionalData,
                Result = a.Result,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp
            }).ToList();

            return Ok(ApiResponse<List<AuditLogDto>>.SuccessResponse(auditLogDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user audit trail for {UserId}", userId);
            return StatusCode(500, ApiResponse<List<AuditLogDto>>.ErrorResponse("Failed to retrieve user audit trail"));
        }
    }

    /// <summary>
    /// Generate audit report
    /// </summary>
    [HttpPost("report")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<AuditReportDto>>> GenerateAuditReport([FromBody] AuditReportRequestDto request)
    {
        try
        {
            var reportRequest = new AuditReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                EntityType = request.EntityType,
                Action = request.Action,
                UserId = request.UserId,
                IncludeSuccessfulActions = request.IncludeSuccessfulActions,
                IncludeFailedActions = request.IncludeFailedActions,
                GroupByUser = request.GroupByUser,
                GroupByAction = request.GroupByAction,
                GroupByEntityType = request.GroupByEntityType
            };

            var report = await _auditService.GenerateAuditReportAsync(reportRequest);
            
            var reportDto = new AuditReportDto
            {
                GeneratedAt = report.GeneratedAt,
                FromDate = report.FromDate,
                ToDate = report.ToDate,
                TotalActions = report.TotalActions,
                SuccessfulActions = report.SuccessfulActions,
                FailedActions = report.FailedActions,
                Summary = report.Summary.Select(s => new AuditSummaryDto
                {
                    Category = s.Category,
                    Value = s.Value,
                    Count = s.Count,
                    Percentage = s.Percentage
                }).ToList(),
                Details = report.Details.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    UserId = a.UserId,
                    Username = a.Username,
                    UserRole = a.UserRole,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    AdditionalData = a.AdditionalData,
                    Result = a.Result,
                    ErrorMessage = a.ErrorMessage,
                    Timestamp = a.Timestamp
                }).ToList()
            };

            return Ok(ApiResponse<AuditReportDto>.SuccessResponse(reportDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit report");
            return StatusCode(500, ApiResponse<AuditReportDto>.ErrorResponse("Failed to generate audit report"));
        }
    }

    /// <summary>
    /// Export audit logs to Excel or CSV
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ExportAuditLogs([FromBody] AuditLogFilterDto filter, [FromQuery] string format = "excel")
    {
        try
        {
            var auditFilter = new AuditLogFilter
            {
                Action = filter.Action,
                EntityType = filter.EntityType,
                EntityId = filter.EntityId,
                UserId = filter.UserId,
                Username = filter.Username,
                UserRole = filter.UserRole,
                Result = filter.Result,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                IpAddress = filter.IpAddress,
                Page = 1,
                PageSize = int.MaxValue, // Export all matching records
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending
            };

            var exportData = await _auditService.ExportAuditLogsAsync(auditFilter, format);
            
            var contentType = format.ToLower() == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv";
            var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{(format.ToLower() == "excel" ? "xlsx" : "csv")}";
            
            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, "Failed to export audit logs");
        }
    }
}