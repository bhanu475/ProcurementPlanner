using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportingController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportingController> _logger;

    public ReportingController(IReportingService reportingService, ILogger<ReportingController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Generate performance metrics report
    /// </summary>
    [HttpPost("performance-metrics")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<PerformanceMetricsReportDto>>> GeneratePerformanceMetricsReport([FromBody] PerformanceMetricsReportRequestDto request)
    {
        try
        {
            var reportRequest = new PerformanceMetricsReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SupplierId = request.SupplierId,
                ProductType = request.ProductType,
                IncludeOrderMetrics = request.IncludeOrderMetrics,
                IncludeSupplierMetrics = request.IncludeSupplierMetrics,
                IncludeDeliveryMetrics = request.IncludeDeliveryMetrics
            };

            var report = await _reportingService.GeneratePerformanceMetricsReportAsync(reportRequest);
            var reportDto = MapToPerformanceMetricsReportDto(report);

            return Ok(ApiResponse<PerformanceMetricsReportDto>.SuccessResponse(reportDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance metrics report");
            return StatusCode(500, ApiResponse<PerformanceMetricsReportDto>.ErrorResponse("Failed to generate performance metrics report"));
        }
    }

    /// <summary>
    /// Generate supplier distribution report
    /// </summary>
    [HttpPost("supplier-distribution")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<SupplierDistributionReportDto>>> GenerateSupplierDistributionReport([FromBody] SupplierDistributionReportRequestDto request)
    {
        try
        {
            var reportRequest = new SupplierDistributionReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                ProductType = request.ProductType,
                GroupByProductType = request.GroupByProductType,
                GroupByMonth = request.GroupByMonth,
                IncludeCapacityUtilization = request.IncludeCapacityUtilization
            };

            var report = await _reportingService.GenerateSupplierDistributionReportAsync(reportRequest);
            var reportDto = MapToSupplierDistributionReportDto(report);

            return Ok(ApiResponse<SupplierDistributionReportDto>.SuccessResponse(reportDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating supplier distribution report");
            return StatusCode(500, ApiResponse<SupplierDistributionReportDto>.ErrorResponse("Failed to generate supplier distribution report"));
        }
    }

    /// <summary>
    /// Generate order fulfillment report
    /// </summary>
    [HttpPost("order-fulfillment")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<OrderFulfillmentReportDto>>> GenerateOrderFulfillmentReport([FromBody] OrderFulfillmentReportRequestDto request)
    {
        try
        {
            var reportRequest = new OrderFulfillmentReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                CustomerId = request.CustomerId,
                ProductType = request.ProductType,
                GroupByStatus = request.GroupByStatus,
                GroupByCustomer = request.GroupByCustomer,
                IncludeTimelines = request.IncludeTimelines
            };

            var report = await _reportingService.GenerateOrderFulfillmentReportAsync(reportRequest);
            var reportDto = MapToOrderFulfillmentReportDto(report);

            return Ok(ApiResponse<OrderFulfillmentReportDto>.SuccessResponse(reportDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating order fulfillment report");
            return StatusCode(500, ApiResponse<OrderFulfillmentReportDto>.ErrorResponse("Failed to generate order fulfillment report"));
        }
    }

    /// <summary>
    /// Generate delivery performance report
    /// </summary>
    [HttpPost("delivery-performance")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<ActionResult<ApiResponse<DeliveryPerformanceReportDto>>> GenerateDeliveryPerformanceReport([FromBody] DeliveryPerformanceReportRequestDto request)
    {
        try
        {
            var reportRequest = new DeliveryPerformanceReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SupplierId = request.SupplierId,
                ProductType = request.ProductType,
                GroupBySupplier = request.GroupBySupplier,
                GroupByMonth = request.GroupByMonth,
                IncludeDelayAnalysis = request.IncludeDelayAnalysis
            };

            var report = await _reportingService.GenerateDeliveryPerformanceReportAsync(reportRequest);
            var reportDto = MapToDeliveryPerformanceReportDto(report);

            return Ok(ApiResponse<DeliveryPerformanceReportDto>.SuccessResponse(reportDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating delivery performance report");
            return StatusCode(500, ApiResponse<DeliveryPerformanceReportDto>.ErrorResponse("Failed to generate delivery performance report"));
        }
    }

    /// <summary>
    /// Export performance metrics report
    /// </summary>
    [HttpPost("performance-metrics/export")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<IActionResult> ExportPerformanceMetricsReport([FromBody] PerformanceMetricsReportRequestDto request, [FromQuery] string format = "excel")
    {
        try
        {
            var reportRequest = new PerformanceMetricsReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SupplierId = request.SupplierId,
                ProductType = request.ProductType,
                IncludeOrderMetrics = request.IncludeOrderMetrics,
                IncludeSupplierMetrics = request.IncludeSupplierMetrics,
                IncludeDeliveryMetrics = request.IncludeDeliveryMetrics
            };

            var report = await _reportingService.GeneratePerformanceMetricsReportAsync(reportRequest);
            var exportData = await _reportingService.ExportReportAsync(report, format);

            var contentType = GetContentType(format);
            var fileName = $"performance_metrics_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";

            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting performance metrics report");
            return StatusCode(500, "Failed to export performance metrics report");
        }
    }

    /// <summary>
    /// Export supplier distribution report
    /// </summary>
    [HttpPost("supplier-distribution/export")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<IActionResult> ExportSupplierDistributionReport([FromBody] SupplierDistributionReportRequestDto request, [FromQuery] string format = "excel")
    {
        try
        {
            var reportRequest = new SupplierDistributionReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                ProductType = request.ProductType,
                GroupByProductType = request.GroupByProductType,
                GroupByMonth = request.GroupByMonth,
                IncludeCapacityUtilization = request.IncludeCapacityUtilization
            };

            var report = await _reportingService.GenerateSupplierDistributionReportAsync(reportRequest);
            var exportData = await _reportingService.ExportReportAsync(report, format);

            var contentType = GetContentType(format);
            var fileName = $"supplier_distribution_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";

            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting supplier distribution report");
            return StatusCode(500, "Failed to export supplier distribution report");
        }
    }

    /// <summary>
    /// Export order fulfillment report
    /// </summary>
    [HttpPost("order-fulfillment/export")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<IActionResult> ExportOrderFulfillmentReport([FromBody] OrderFulfillmentReportRequestDto request, [FromQuery] string format = "excel")
    {
        try
        {
            var reportRequest = new OrderFulfillmentReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                CustomerId = request.CustomerId,
                ProductType = request.ProductType,
                GroupByStatus = request.GroupByStatus,
                GroupByCustomer = request.GroupByCustomer,
                IncludeTimelines = request.IncludeTimelines
            };

            var report = await _reportingService.GenerateOrderFulfillmentReportAsync(reportRequest);
            var exportData = await _reportingService.ExportReportAsync(report, format);

            var contentType = GetContentType(format);
            var fileName = $"order_fulfillment_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";

            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting order fulfillment report");
            return StatusCode(500, "Failed to export order fulfillment report");
        }
    }

    /// <summary>
    /// Export delivery performance report
    /// </summary>
    [HttpPost("delivery-performance/export")]
    [Authorize(Roles = "Administrator,LMRPlanner")]
    public async Task<IActionResult> ExportDeliveryPerformanceReport([FromBody] DeliveryPerformanceReportRequestDto request, [FromQuery] string format = "excel")
    {
        try
        {
            var reportRequest = new DeliveryPerformanceReportRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SupplierId = request.SupplierId,
                ProductType = request.ProductType,
                GroupBySupplier = request.GroupBySupplier,
                GroupByMonth = request.GroupByMonth,
                IncludeDelayAnalysis = request.IncludeDelayAnalysis
            };

            var report = await _reportingService.GenerateDeliveryPerformanceReportAsync(reportRequest);
            var exportData = await _reportingService.ExportReportAsync(report, format);

            var contentType = GetContentType(format);
            var fileName = $"delivery_performance_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";

            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting delivery performance report");
            return StatusCode(500, "Failed to export delivery performance report");
        }
    }

    // Private helper methods for mapping
    private PerformanceMetricsReportDto MapToPerformanceMetricsReportDto(PerformanceMetricsReport report)
    {
        return new PerformanceMetricsReportDto
        {
            GeneratedAt = report.GeneratedAt,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            OrderMetrics = new OrderMetricsDto
            {
                TotalOrders = report.OrderMetrics.TotalOrders,
                CompletedOrders = report.OrderMetrics.CompletedOrders,
                PendingOrders = report.OrderMetrics.PendingOrders,
                CancelledOrders = report.OrderMetrics.CancelledOrders,
                TotalValue = report.OrderMetrics.TotalValue,
                CompletionRate = report.OrderMetrics.CompletionRate,
                AverageOrderValue = report.OrderMetrics.AverageOrderValue
            },
            SupplierMetrics = report.SupplierMetrics.Select(s => new SupplierMetricsDto
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                OrdersAssigned = s.OrdersAssigned,
                OrdersCompleted = s.OrdersCompleted,
                TotalValue = s.TotalValue,
                OnTimeDeliveryRate = s.OnTimeDeliveryRate,
                QualityScore = s.QualityScore,
                CapacityUtilization = s.CapacityUtilization
            }).ToList(),
            DeliveryMetrics = new DeliveryMetricsDto
            {
                TotalDeliveries = report.DeliveryMetrics.TotalDeliveries,
                OnTimeDeliveries = report.DeliveryMetrics.OnTimeDeliveries,
                LateDeliveries = report.DeliveryMetrics.LateDeliveries,
                OnTimeRate = report.DeliveryMetrics.OnTimeRate,
                AverageDeliveryDays = report.DeliveryMetrics.AverageDeliveryDays,
                AverageDelayDays = report.DeliveryMetrics.AverageDelayDays
            }
        };
    }

    private SupplierDistributionReportDto MapToSupplierDistributionReportDto(SupplierDistributionReport report)
    {
        return new SupplierDistributionReportDto
        {
            GeneratedAt = report.GeneratedAt,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            TotalOrders = report.TotalOrders,
            TotalValue = report.TotalValue,
            Distributions = report.Distributions.Select(d => new SupplierDistributionDto
            {
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                OrderCount = d.OrderCount,
                TotalValue = d.TotalValue,
                Percentage = d.Percentage,
                CapacityUtilization = d.CapacityUtilization
            }).ToList()
        };
    }

    private OrderFulfillmentReportDto MapToOrderFulfillmentReportDto(OrderFulfillmentReport report)
    {
        return new OrderFulfillmentReportDto
        {
            GeneratedAt = report.GeneratedAt,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            TotalOrders = report.TotalOrders,
            AverageFulfillmentDays = report.AverageFulfillmentDays,
            StatusSummaries = report.StatusSummaries.Select(s => new OrderStatusSummaryDto
            {
                Status = s.Status,
                Count = s.Count,
                Percentage = s.Percentage,
                TotalValue = s.TotalValue,
                AverageDaysInStatus = s.AverageDaysInStatus
            }).ToList()
        };
    }

    private DeliveryPerformanceReportDto MapToDeliveryPerformanceReportDto(DeliveryPerformanceReport report)
    {
        return new DeliveryPerformanceReportDto
        {
            GeneratedAt = report.GeneratedAt,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            TotalDeliveries = report.TotalDeliveries,
            OnTimeDeliveryRate = report.OnTimeDeliveryRate,
            AverageDeliveryDays = report.AverageDeliveryDays,
            SupplierPerformances = report.SupplierPerformances.Select(p => new SupplierDeliveryPerformanceDto
            {
                SupplierId = p.SupplierId,
                SupplierName = p.SupplierName,
                TotalDeliveries = p.TotalDeliveries,
                OnTimeDeliveries = p.OnTimeDeliveries,
                OnTimeRate = p.OnTimeRate,
                AverageDeliveryDays = p.AverageDeliveryDays,
                AverageDelayDays = p.AverageDelayDays,
                PerformanceGrade = p.PerformanceGrade
            }).ToList()
        };
    }

    private string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "csv" => "text/csv",
            "json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private string GetFileExtension(string format)
    {
        return format.ToLower() switch
        {
            "excel" => "xlsx",
            "csv" => "csv",
            "json" => "json",
            _ => "bin"
        };
    }
}