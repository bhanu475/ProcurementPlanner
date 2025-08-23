using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(ApplicationDbContext context, ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PerformanceMetricsReport> GeneratePerformanceMetricsReportAsync(PerformanceMetricsReportRequest request)
    {
        _logger.LogInformation("Generating performance metrics report for period {FromDate} to {ToDate}", 
            request.FromDate, request.ToDate);

        var report = new PerformanceMetricsReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        // Get orders within the date range
        var ordersQuery = _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= request.FromDate && o.CreatedAt <= request.ToDate);

        if (!string.IsNullOrEmpty(request.ProductType))
        {
            ordersQuery = ordersQuery.Where(o => o.ProductType.ToString() == request.ProductType);
        }

        var orders = await ordersQuery.ToListAsync();

        // Generate order metrics
        if (request.IncludeOrderMetrics)
        {
            report.OrderMetrics = GenerateOrderMetrics(orders);
        }

        // Generate supplier metrics
        if (request.IncludeSupplierMetrics)
        {
            report.SupplierMetrics = await GenerateSupplierMetricsAsync(request, orders);
        }

        // Generate delivery metrics
        if (request.IncludeDeliveryMetrics)
        {
            report.DeliveryMetrics = await GenerateDeliveryMetricsAsync(request);
        }

        // Generate trends
        report.Trends = await GeneratePerformanceTrendsAsync(request);

        return report;
    }

    public async Task<SupplierDistributionReport> GenerateSupplierDistributionReportAsync(SupplierDistributionReportRequest request)
    {
        _logger.LogInformation("Generating supplier distribution report for period {FromDate} to {ToDate}", 
            request.FromDate, request.ToDate);

        var report = new SupplierDistributionReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        // Get purchase orders within the date range
        var purchaseOrdersQuery = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .Include(po => po.CustomerOrder)
            .Where(po => po.CreatedAt >= request.FromDate && po.CreatedAt <= request.ToDate);

        if (!string.IsNullOrEmpty(request.ProductType))
        {
            purchaseOrdersQuery = purchaseOrdersQuery.Where(po => po.CustomerOrder.ProductType.ToString() == request.ProductType);
        }

        var purchaseOrders = await purchaseOrdersQuery.ToListAsync();

        report.TotalOrders = purchaseOrders.Count;
        report.TotalValue = purchaseOrders.Sum(po => po.TotalValue ?? 0);

        // Generate supplier distributions
        report.Distributions = GenerateSupplierDistributions(purchaseOrders);

        // Generate product type distributions
        if (request.GroupByProductType)
        {
            report.ProductTypeDistributions = GenerateProductTypeDistributions(purchaseOrders);
        }

        // Generate capacity utilizations
        if (request.IncludeCapacityUtilization)
        {
            report.CapacityUtilizations = await GenerateCapacityUtilizationsAsync(request);
        }

        return report;
    }

    public async Task<OrderFulfillmentReport> GenerateOrderFulfillmentReportAsync(OrderFulfillmentReportRequest request)
    {
        _logger.LogInformation("Generating order fulfillment report for period {FromDate} to {ToDate}", 
            request.FromDate, request.ToDate);

        var report = new OrderFulfillmentReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        // Get orders within the date range
        var ordersQuery = _context.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= request.FromDate && o.CreatedAt <= request.ToDate);

        if (!string.IsNullOrEmpty(request.CustomerId))
        {
            ordersQuery = ordersQuery.Where(o => o.CustomerId == request.CustomerId);
        }

        if (!string.IsNullOrEmpty(request.ProductType))
        {
            ordersQuery = ordersQuery.Where(o => o.ProductType.ToString() == request.ProductType);
        }

        var orders = await ordersQuery.ToListAsync();
        report.TotalOrders = orders.Count;

        // Generate status summaries
        if (request.GroupByStatus)
        {
            report.StatusSummaries = GenerateOrderStatusSummaries(orders);
        }

        // Generate customer summaries
        if (request.GroupByCustomer)
        {
            report.CustomerSummaries = GenerateCustomerOrderSummaries(orders);
        }

        // Generate fulfillment timelines
        if (request.IncludeTimelines)
        {
            report.Timelines = await GenerateFulfillmentTimelinesAsync(orders);
            report.AverageFulfillmentDays = CalculateAverageFulfillmentDays(orders);
        }

        return report;
    }

    public async Task<DeliveryPerformanceReport> GenerateDeliveryPerformanceReportAsync(DeliveryPerformanceReportRequest request)
    {
        _logger.LogInformation("Generating delivery performance report for period {FromDate} to {ToDate}", 
            request.FromDate, request.ToDate);

        var report = new DeliveryPerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        // Get delivered purchase orders within the date range
        var deliveredOrdersQuery = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.CustomerOrder)
            .Where(po => po.Status == PurchaseOrderStatus.Delivered &&
                        po.UpdatedAt >= request.FromDate && po.UpdatedAt <= request.ToDate);

        if (request.SupplierId.HasValue)
        {
            deliveredOrdersQuery = deliveredOrdersQuery.Where(po => po.SupplierId == request.SupplierId);
        }

        if (!string.IsNullOrEmpty(request.ProductType))
        {
            deliveredOrdersQuery = deliveredOrdersQuery.Where(po => po.CustomerOrder.ProductType.ToString() == request.ProductType);
        }

        var deliveredOrders = await deliveredOrdersQuery.ToListAsync();

        report.TotalDeliveries = deliveredOrders.Count;
        report.OnTimeDeliveryRate = CalculateOnTimeDeliveryRate(deliveredOrders);
        report.AverageDeliveryDays = CalculateAverageDeliveryDays(deliveredOrders);

        // Generate supplier delivery performances
        if (request.GroupBySupplier)
        {
            report.SupplierPerformances = GenerateSupplierDeliveryPerformances(deliveredOrders);
        }

        // Generate delay analyses
        if (request.IncludeDelayAnalysis)
        {
            report.DelayAnalyses = GenerateDeliveryDelayAnalyses(deliveredOrders);
        }

        // Generate monthly trends
        if (request.GroupByMonth)
        {
            report.MonthlyTrends = GenerateMonthlyDeliveryTrends(deliveredOrders);
        }

        return report;
    }

    public async Task<byte[]> ExportReportAsync<T>(T report, string format = "excel") where T : class
    {
        if (format.ToLower() == "excel")
        {
            return await ExportToExcelAsync(report);
        }
        else if (format.ToLower() == "csv")
        {
            return await ExportToCsv(report);
        }
        else if (format.ToLower() == "json")
        {
            return await ExportToJson(report);
        }
        else
        {
            throw new ArgumentException($"Unsupported export format: {format}");
        }
    }

    // Private helper methods
    private OrderMetrics GenerateOrderMetrics(List<CustomerOrder> orders)
    {
        var totalValue = orders.SelectMany(o => o.Items).Sum(i => i.Quantity * (i.UnitPrice ?? 0));
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Delivered);

        return new OrderMetrics
        {
            TotalOrders = orders.Count,
            CompletedOrders = completedOrders,
            PendingOrders = orders.Count(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalValue = totalValue,
            CompletionRate = orders.Count > 0 ? (double)completedOrders / orders.Count * 100 : 0,
            AverageOrderValue = orders.Count > 0 ? (double)totalValue / orders.Count : 0
        };
    }

    private async Task<List<SupplierMetrics>> GenerateSupplierMetricsAsync(PerformanceMetricsReportRequest request, List<CustomerOrder> orders)
    {
        var supplierMetrics = new List<SupplierMetrics>();

        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .Where(po => po.CreatedAt >= request.FromDate && po.CreatedAt <= request.ToDate)
            .ToListAsync();

        if (request.SupplierId.HasValue)
        {
            purchaseOrders = purchaseOrders.Where(po => po.SupplierId == request.SupplierId).ToList();
        }

        var supplierGroups = purchaseOrders.GroupBy(po => po.Supplier);

        foreach (var group in supplierGroups)
        {
            var supplier = group.Key;
            var supplierOrders = group.ToList();
            var completedOrders = supplierOrders.Count(o => o.Status == PurchaseOrderStatus.Delivered);
            var totalValue = supplierOrders.Sum(o => o.TotalValue ?? 0);

            var performance = await _context.SupplierPerformanceMetrics
                .FirstOrDefaultAsync(p => p.SupplierId == supplier.Id);

            supplierMetrics.Add(new SupplierMetrics
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                OrdersAssigned = supplierOrders.Count,
                OrdersCompleted = completedOrders,
                TotalValue = totalValue,
                OnTimeDeliveryRate = (double)(performance?.OnTimeDeliveryRate ?? 0),
                QualityScore = (double)(performance?.QualityScore ?? 0),
                CapacityUtilization = CalculateCapacityUtilization(supplier.Id, supplierOrders)
            });
        }

        return supplierMetrics;
    }

    private async Task<DeliveryMetrics> GenerateDeliveryMetricsAsync(PerformanceMetricsReportRequest request)
    {
        var deliveredOrders = await _context.PurchaseOrders
            .Where(po => po.Status == PurchaseOrderStatus.Delivered &&
                        po.UpdatedAt >= request.FromDate && po.UpdatedAt <= request.ToDate)
            .ToListAsync();

        if (request.SupplierId.HasValue)
        {
            deliveredOrders = deliveredOrders.Where(po => po.SupplierId == request.SupplierId).ToList();
        }

        var onTimeDeliveries = deliveredOrders.Count(o => IsOnTimeDelivery(o));
        var totalDeliveries = deliveredOrders.Count;

        return new DeliveryMetrics
        {
            TotalDeliveries = totalDeliveries,
            OnTimeDeliveries = onTimeDeliveries,
            LateDeliveries = totalDeliveries - onTimeDeliveries,
            OnTimeRate = totalDeliveries > 0 ? (double)onTimeDeliveries / totalDeliveries * 100 : 0,
            AverageDeliveryDays = CalculateAverageDeliveryDays(deliveredOrders),
            AverageDelayDays = CalculateAverageDelayDays(deliveredOrders)
        };
    }

    private async Task<List<PerformanceTrend>> GeneratePerformanceTrendsAsync(PerformanceMetricsReportRequest request)
    {
        // This is a simplified implementation - in a real scenario, you'd calculate trends over time
        var trends = new List<PerformanceTrend>();

        // Calculate monthly trends for the period
        var currentDate = request.FromDate;
        while (currentDate <= request.ToDate)
        {
            var monthEnd = currentDate.AddMonths(1).AddDays(-1);
            if (monthEnd > request.ToDate) monthEnd = request.ToDate;

            var monthlyOrders = await _context.CustomerOrders
                .Where(o => o.CreatedAt >= currentDate && o.CreatedAt <= monthEnd)
                .CountAsync();

            trends.Add(new PerformanceTrend
            {
                Period = currentDate,
                MetricName = "Monthly Orders",
                Value = monthlyOrders,
                ChangeFromPrevious = 0 // Would calculate actual change in real implementation
            });

            currentDate = currentDate.AddMonths(1);
        }

        return trends;
    }

    private List<SupplierDistribution> GenerateSupplierDistributions(List<PurchaseOrder> purchaseOrders)
    {
        var supplierGroups = purchaseOrders.GroupBy(po => po.Supplier);
        var totalValue = purchaseOrders.Sum(po => po.TotalValue ?? 0);

        return supplierGroups.Select(group => new SupplierDistribution
        {
            SupplierId = group.Key.Id,
            SupplierName = group.Key.Name,
            OrderCount = group.Count(),
            TotalValue = group.Sum(po => po.TotalValue ?? 0),
            Percentage = totalValue > 0 ? (double)group.Sum(po => po.TotalValue ?? 0) / (double)totalValue * 100 : 0,
            CapacityUtilization = CalculateCapacityUtilization(group.Key.Id, group.ToList())
        }).OrderByDescending(d => d.TotalValue).ToList();
    }

    private List<ProductTypeDistribution> GenerateProductTypeDistributions(List<PurchaseOrder> purchaseOrders)
    {
        var productTypeGroups = purchaseOrders.GroupBy(po => po.CustomerOrder.ProductType);
        var totalValue = purchaseOrders.Sum(po => po.TotalValue ?? 0);

        return productTypeGroups.Select(group => new ProductTypeDistribution
        {
            ProductType = group.Key.ToString(),
            OrderCount = group.Count(),
            TotalValue = group.Sum(po => po.TotalValue ?? 0),
            Percentage = totalValue > 0 ? (double)group.Sum(po => po.TotalValue ?? 0) / (double)totalValue * 100 : 0,
            SupplierBreakdown = GenerateSupplierDistributions(group.ToList())
        }).ToList();
    }

    private async Task<List<CapacityUtilization>> GenerateCapacityUtilizationsAsync(SupplierDistributionReportRequest request)
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Capabilities)
            .Where(s => s.IsActive)
            .ToListAsync();

        var utilizations = new List<CapacityUtilization>();

        foreach (var supplier in suppliers)
        {
            foreach (var capability in supplier.Capabilities.Where(c => c.IsActive))
            {
                var usedCapacity = await CalculateUsedCapacityAsync(supplier.Id, capability.ProductType, request.FromDate, request.ToDate);

                utilizations.Add(new CapacityUtilization
                {
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    ProductType = capability.ProductType.ToString(),
                    MaxCapacity = capability.MaxMonthlyCapacity,
                    UsedCapacity = usedCapacity,
                    UtilizationRate = capability.MaxMonthlyCapacity > 0 ? (double)usedCapacity / capability.MaxMonthlyCapacity * 100 : 0,
                    AvailableCapacity = Math.Max(0, capability.MaxMonthlyCapacity - usedCapacity)
                });
            }
        }

        return utilizations;
    }

    private List<OrderStatusSummary> GenerateOrderStatusSummaries(List<CustomerOrder> orders)
    {
        var statusGroups = orders.GroupBy(o => o.Status);
        var totalValue = orders.SelectMany(o => o.Items).Sum(i => i.Quantity * i.UnitPrice);

        return statusGroups.Select(group => new OrderStatusSummary
        {
            Status = group.Key.ToString(),
            Count = group.Count(),
            Percentage = orders.Count > 0 ? (double)group.Count() / orders.Count * 100 : 0,
            TotalValue = group.SelectMany(o => o.Items).Sum(i => i.Quantity * (i.UnitPrice ?? 0)),
            AverageDaysInStatus = CalculateAverageDaysInStatus(group.ToList())
        }).OrderByDescending(s => s.Count).ToList();
    }

    private List<CustomerOrderSummary> GenerateCustomerOrderSummaries(List<CustomerOrder> orders)
    {
        var customerGroups = orders.GroupBy(o => new { o.CustomerId, o.CustomerName });

        return customerGroups.Select(group => new CustomerOrderSummary
        {
            CustomerId = group.Key.CustomerId,
            CustomerName = group.Key.CustomerName,
            OrderCount = group.Count(),
            TotalValue = group.SelectMany(o => o.Items).Sum(i => i.Quantity * (i.UnitPrice ?? 0)),
            CompletionRate = group.Count() > 0 ? (double)group.Count(o => o.Status == OrderStatus.Delivered) / group.Count() * 100 : 0,
            AverageFulfillmentDays = CalculateAverageFulfillmentDays(group.ToList())
        }).OrderByDescending(s => s.TotalValue).ToList();
    }

    private Task<List<FulfillmentTimeline>> GenerateFulfillmentTimelinesAsync(List<CustomerOrder> orders)
    {
        // This is a simplified implementation - in a real scenario, you'd track detailed stage timings
        var timelines = new List<FulfillmentTimeline>
        {
            new FulfillmentTimeline
            {
                Stage = "Order Submission to Review",
                AverageDays = 1.5,
                MinDays = 0.5,
                MaxDays = 3.0,
                OrderCount = orders.Count
            },
            new FulfillmentTimeline
            {
                Stage = "Review to Purchase Order Creation",
                AverageDays = 2.0,
                MinDays = 1.0,
                MaxDays = 5.0,
                OrderCount = orders.Count(o => o.Status != OrderStatus.Submitted)
            },
            new FulfillmentTimeline
            {
                Stage = "Purchase Order to Delivery",
                AverageDays = 7.5,
                MinDays = 3.0,
                MaxDays = 14.0,
                OrderCount = orders.Count(o => o.Status == OrderStatus.Delivered)
            }
        };

        return Task.FromResult(timelines);
    }

    private List<SupplierDeliveryPerformance> GenerateSupplierDeliveryPerformances(List<PurchaseOrder> deliveredOrders)
    {
        var supplierGroups = deliveredOrders.GroupBy(po => po.Supplier);

        return supplierGroups.Select(group => 
        {
            var orders = group.ToList();
            var onTimeCount = orders.Count(o => IsOnTimeDelivery(o));
            var onTimeRate = orders.Count > 0 ? (double)onTimeCount / orders.Count * 100 : 0;

            return new SupplierDeliveryPerformance
            {
                SupplierId = group.Key.Id,
                SupplierName = group.Key.Name,
                TotalDeliveries = orders.Count,
                OnTimeDeliveries = onTimeCount,
                OnTimeRate = onTimeRate,
                AverageDeliveryDays = CalculateAverageDeliveryDays(orders),
                AverageDelayDays = CalculateAverageDelayDays(orders),
                PerformanceGrade = GetPerformanceGrade(onTimeRate)
            };
        }).OrderByDescending(p => p.OnTimeRate).ToList();
    }

    private List<DeliveryDelayAnalysis> GenerateDeliveryDelayAnalyses(List<PurchaseOrder> deliveredOrders)
    {
        var lateOrders = deliveredOrders.Where(o => !IsOnTimeDelivery(o)).ToList();
        var totalLate = lateOrders.Count;

        var analyses = new List<DeliveryDelayAnalysis>();

        if (totalLate > 0)
        {
            // Categorize delays
            var minorDelays = lateOrders.Where(o => GetDelayDays(o) <= 2).ToList();
            var moderateDelays = lateOrders.Where(o => GetDelayDays(o) > 2 && GetDelayDays(o) <= 7).ToList();
            var majorDelays = lateOrders.Where(o => GetDelayDays(o) > 7).ToList();

            analyses.Add(new DeliveryDelayAnalysis
            {
                DelayCategory = "Minor (1-2 days)",
                Count = minorDelays.Count,
                Percentage = (double)minorDelays.Count / totalLate * 100,
                AverageDelayDays = minorDelays.Count > 0 ? minorDelays.Average(o => GetDelayDays(o)) : 0,
                CommonReasons = new List<string> { "Processing delays", "Minor logistics issues" }
            });

            analyses.Add(new DeliveryDelayAnalysis
            {
                DelayCategory = "Moderate (3-7 days)",
                Count = moderateDelays.Count,
                Percentage = (double)moderateDelays.Count / totalLate * 100,
                AverageDelayDays = moderateDelays.Count > 0 ? moderateDelays.Average(o => GetDelayDays(o)) : 0,
                CommonReasons = new List<string> { "Supply chain issues", "Quality control delays" }
            });

            analyses.Add(new DeliveryDelayAnalysis
            {
                DelayCategory = "Major (8+ days)",
                Count = majorDelays.Count,
                Percentage = (double)majorDelays.Count / totalLate * 100,
                AverageDelayDays = majorDelays.Count > 0 ? majorDelays.Average(o => GetDelayDays(o)) : 0,
                CommonReasons = new List<string> { "Production issues", "Force majeure events" }
            });
        }

        return analyses;
    }

    private List<MonthlyDeliveryTrend> GenerateMonthlyDeliveryTrends(List<PurchaseOrder> deliveredOrders)
    {
        var monthlyGroups = deliveredOrders
            .GroupBy(o => new DateTime(o.UpdatedAt!.Value.Year, o.UpdatedAt.Value.Month, 1))
            .OrderBy(g => g.Key);

        var trends = new List<MonthlyDeliveryTrend>();
        double? previousOnTimeRate = null;

        foreach (var group in monthlyGroups)
        {
            var orders = group.ToList();
            var onTimeCount = orders.Count(o => IsOnTimeDelivery(o));
            var onTimeRate = orders.Count > 0 ? (double)onTimeCount / orders.Count * 100 : 0;

            trends.Add(new MonthlyDeliveryTrend
            {
                Month = group.Key,
                TotalDeliveries = orders.Count,
                OnTimeRate = onTimeRate,
                AverageDeliveryDays = CalculateAverageDeliveryDays(orders),
                ChangeFromPreviousMonth = previousOnTimeRate.HasValue ? onTimeRate - previousOnTimeRate.Value : 0
            });

            previousOnTimeRate = onTimeRate;
        }

        return trends;
    }

    // Utility methods
    private double CalculateCapacityUtilization(Guid supplierId, List<PurchaseOrder> orders)
    {
        // Simplified calculation - in reality, you'd consider time periods and capacity constraints
        return orders.Count * 10.0; // Placeholder calculation
    }

    private async Task<int> CalculateUsedCapacityAsync(Guid supplierId, ProductType productType, DateTime fromDate, DateTime toDate)
    {
        return await _context.PurchaseOrders
            .Where(po => po.SupplierId == supplierId &&
                        po.CustomerOrder.ProductType == productType &&
                        po.CreatedAt >= fromDate && po.CreatedAt <= toDate)
            .SumAsync(po => po.Items.Sum(i => i.AllocatedQuantity));
    }

    private bool IsOnTimeDelivery(PurchaseOrder order)
    {
        return order.UpdatedAt <= order.RequiredDeliveryDate;
    }

    private double GetDelayDays(PurchaseOrder order)
    {
        if (order.UpdatedAt.HasValue && order.UpdatedAt > order.RequiredDeliveryDate)
        {
            return (order.UpdatedAt.Value - order.RequiredDeliveryDate).TotalDays;
        }
        return 0;
    }

    private double CalculateOnTimeDeliveryRate(List<PurchaseOrder> orders)
    {
        if (orders.Count == 0) return 0;
        var onTimeCount = orders.Count(o => IsOnTimeDelivery(o));
        return (double)onTimeCount / orders.Count * 100;
    }

    private double CalculateAverageDeliveryDays(List<PurchaseOrder> orders)
    {
        if (orders.Count == 0) return 0;
        var deliveryDays = orders.Where(o => o.UpdatedAt.HasValue)
            .Select(o => (o.UpdatedAt!.Value - o.CreatedAt).TotalDays);
        return deliveryDays.Any() ? deliveryDays.Average() : 0;
    }

    private double CalculateAverageDelayDays(List<PurchaseOrder> orders)
    {
        var lateOrders = orders.Where(o => !IsOnTimeDelivery(o)).ToList();
        if (lateOrders.Count == 0) return 0;
        return lateOrders.Average(o => GetDelayDays(o));
    }

    private double CalculateAverageFulfillmentDays(List<CustomerOrder> orders)
    {
        var completedOrders = orders.Where(o => o.Status == OrderStatus.Delivered && o.UpdatedAt.HasValue).ToList();
        if (completedOrders.Count == 0) return 0;
        return completedOrders.Average(o => (o.UpdatedAt!.Value - o.CreatedAt).TotalDays);
    }

    private double CalculateAverageDaysInStatus(List<CustomerOrder> orders)
    {
        // Simplified calculation - in reality, you'd track status change timestamps
        return orders.Average(o => (DateTime.UtcNow - o.CreatedAt).TotalDays);
    }

    private string GetPerformanceGrade(double onTimeRate)
    {
        return onTimeRate switch
        {
            >= 95 => "A+",
            >= 90 => "A",
            >= 85 => "B+",
            >= 80 => "B",
            >= 75 => "C+",
            >= 70 => "C",
            _ => "D"
        };
    }

    private Task<byte[]> ExportToExcelAsync<T>(T report) where T : class
    {
        // For now, return JSON format as Excel implementation would require additional packages
        // In a real implementation, you would use a library like EPPlus or ClosedXML
        return ExportToJson(report);
    }

    private Task<byte[]> ExportToCsv<T>(T report) where T : class
    {
        // Simplified CSV export - in reality, you'd flatten the object structure appropriately
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(Encoding.UTF8.GetBytes($"Report Data (JSON format):\n{json}"));
    }

    private Task<byte[]> ExportToJson<T>(T report) where T : class
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }
}