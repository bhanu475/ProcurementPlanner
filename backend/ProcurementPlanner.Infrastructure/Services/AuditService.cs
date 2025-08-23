using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogRequest request)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                UserId = request.UserId,
                Username = request.Username,
                UserRole = request.UserRole,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                OldValues = request.OldValues != null ? JsonSerializer.Serialize(request.OldValues) : null,
                NewValues = request.NewValues != null ? JsonSerializer.Serialize(request.NewValues) : null,
                AdditionalData = request.AdditionalData,
                Result = request.Result,
                ErrorMessage = request.ErrorMessage,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for action {Action} on {EntityType}", 
                request.Action, request.EntityType);
            // Don't throw - audit logging should not break the main operation
        }
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId = null, 
        object? oldValues = null, object? newValues = null, string? additionalData = null)
    {
        var request = new AuditLogRequest
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            AdditionalData = additionalData,
            Result = AuditResult.Success
        };

        await LogAsync(request);
    }

    public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Action))
        {
            query = query.Where(a => a.Action.Contains(filter.Action));
        }

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == filter.EntityId);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == filter.UserId);
        }

        if (!string.IsNullOrEmpty(filter.Username))
        {
            query = query.Where(a => a.Username != null && a.Username.Contains(filter.Username));
        }

        if (!string.IsNullOrEmpty(filter.UserRole))
        {
            query = query.Where(a => a.UserRole == filter.UserRole);
        }

        if (filter.Result.HasValue)
        {
            query = query.Where(a => a.Result == filter.Result);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.FromDate);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.ToDate);
        }

        if (!string.IsNullOrEmpty(filter.IpAddress))
        {
            query = query.Where(a => a.IpAddress == filter.IpAddress);
        }

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "action" => filter.SortDescending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
            "entitytype" => filter.SortDescending ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
            "username" => filter.SortDescending ? query.OrderByDescending(a => a.Username) : query.OrderBy(a => a.Username),
            "result" => filter.SortDescending ? query.OrderByDescending(a => a.Result) : query.OrderBy(a => a.Result),
            _ => filter.SortDescending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<AuditLog>> GetEntityAuditTrailAsync(string entityType, Guid entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserAuditTrailAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<AuditReport> GenerateAuditReportAsync(AuditReportRequest request)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Apply date filters
        query = query.Where(a => a.Timestamp >= request.FromDate && a.Timestamp <= request.ToDate);

        // Apply optional filters
        if (!string.IsNullOrEmpty(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
        }

        if (!string.IsNullOrEmpty(request.Action))
        {
            query = query.Where(a => a.Action.Contains(request.Action));
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId);
        }

        // Apply result filters
        if (!request.IncludeSuccessfulActions)
        {
            query = query.Where(a => a.Result != AuditResult.Success);
        }

        if (!request.IncludeFailedActions)
        {
            query = query.Where(a => a.Result == AuditResult.Success);
        }

        var auditLogs = await query.Include(a => a.User).ToListAsync();
        var totalActions = auditLogs.Count;
        var successfulActions = auditLogs.Count(a => a.Result == AuditResult.Success);
        var failedActions = totalActions - successfulActions;

        var summary = new List<AuditSummary>();

        // Generate summaries based on grouping options
        if (request.GroupByUser)
        {
            var userGroups = auditLogs
                .GroupBy(a => a.Username ?? "Unknown")
                .Select(g => new AuditSummary
                {
                    Category = "User",
                    Value = g.Key,
                    Count = g.Count(),
                    Percentage = totalActions > 0 ? (double)g.Count() / totalActions * 100 : 0
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            summary.AddRange(userGroups);
        }

        if (request.GroupByAction)
        {
            var actionGroups = auditLogs
                .GroupBy(a => a.Action)
                .Select(g => new AuditSummary
                {
                    Category = "Action",
                    Value = g.Key,
                    Count = g.Count(),
                    Percentage = totalActions > 0 ? (double)g.Count() / totalActions * 100 : 0
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            summary.AddRange(actionGroups);
        }

        if (request.GroupByEntityType)
        {
            var entityGroups = auditLogs
                .GroupBy(a => a.EntityType)
                .Select(g => new AuditSummary
                {
                    Category = "Entity Type",
                    Value = g.Key,
                    Count = g.Count(),
                    Percentage = totalActions > 0 ? (double)g.Count() / totalActions * 100 : 0
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            summary.AddRange(entityGroups);
        }

        return new AuditReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalActions = totalActions,
            SuccessfulActions = successfulActions,
            FailedActions = failedActions,
            Summary = summary,
            Details = auditLogs.OrderByDescending(a => a.Timestamp).ToList()
        };
    }

    public async Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "excel")
    {
        var auditLogs = await GetAuditLogsAsync(filter);
        
        if (format.ToLower() == "excel")
        {
            return await ExportToExcelAsync(auditLogs.Items);
        }
        else if (format.ToLower() == "csv")
        {
            return ExportToCsv(auditLogs.Items);
        }
        else
        {
            throw new ArgumentException($"Unsupported export format: {format}");
        }
    }

    private async Task<byte[]> ExportToExcelAsync(List<AuditLog> auditLogs)
    {
        // For now, return CSV format as Excel implementation would require additional packages
        // In a real implementation, you would use a library like EPPlus or ClosedXML
        return ExportToCsv(auditLogs);
    }

    private byte[] ExportToCsv(List<AuditLog> auditLogs)
    {
        var csv = new System.Text.StringBuilder();
        
        // Add header
        csv.AppendLine("Timestamp,Action,EntityType,EntityId,Username,UserRole,IpAddress,Result,ErrorMessage");
        
        // Add data rows
        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"\"{log.Action}\"," +
                          $"\"{log.EntityType}\"," +
                          $"{log.EntityId}," +
                          $"\"{log.Username ?? ""}\"," +
                          $"\"{log.UserRole ?? ""}\"," +
                          $"\"{log.IpAddress ?? ""}\"," +
                          $"{log.Result}," +
                          $"\"{log.ErrorMessage ?? ""}\"");
        }
        
        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }
}