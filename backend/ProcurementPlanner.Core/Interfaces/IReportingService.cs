using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IReportingService
{
    Task<PerformanceMetricsReport> GeneratePerformanceMetricsReportAsync(PerformanceMetricsReportRequest request);
    Task<SupplierDistributionReport> GenerateSupplierDistributionReportAsync(SupplierDistributionReportRequest request);
    Task<OrderFulfillmentReport> GenerateOrderFulfillmentReportAsync(OrderFulfillmentReportRequest request);
    Task<DeliveryPerformanceReport> GenerateDeliveryPerformanceReportAsync(DeliveryPerformanceReportRequest request);
    Task<byte[]> ExportReportAsync<T>(T report, string format = "excel") where T : class;
}

public class PerformanceMetricsReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? ProductType { get; set; }
    public bool IncludeOrderMetrics { get; set; } = true;
    public bool IncludeSupplierMetrics { get; set; } = true;
    public bool IncludeDeliveryMetrics { get; set; } = true;
}

public class SupplierDistributionReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? ProductType { get; set; }
    public bool GroupByProductType { get; set; } = true;
    public bool GroupByMonth { get; set; } = false;
    public bool IncludeCapacityUtilization { get; set; } = true;
}

public class OrderFulfillmentReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? CustomerId { get; set; }
    public string? ProductType { get; set; }
    public bool GroupByStatus { get; set; } = true;
    public bool GroupByCustomer { get; set; } = false;
    public bool IncludeTimelines { get; set; } = true;
}

public class DeliveryPerformanceReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? ProductType { get; set; }
    public bool GroupBySupplier { get; set; } = true;
    public bool GroupByMonth { get; set; } = false;
    public bool IncludeDelayAnalysis { get; set; } = true;
}

public class PerformanceMetricsReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public OrderMetrics OrderMetrics { get; set; } = new();
    public List<SupplierMetrics> SupplierMetrics { get; set; } = new();
    public DeliveryMetrics DeliveryMetrics { get; set; } = new();
    public List<PerformanceTrend> Trends { get; set; } = new();
}

public class SupplierDistributionReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalValue { get; set; }
    public List<SupplierDistribution> Distributions { get; set; } = new();
    public List<ProductTypeDistribution> ProductTypeDistributions { get; set; } = new();
    public List<CapacityUtilization> CapacityUtilizations { get; set; } = new();
}

public class OrderFulfillmentReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public List<OrderStatusSummary> StatusSummaries { get; set; } = new();
    public List<CustomerOrderSummary> CustomerSummaries { get; set; } = new();
    public List<FulfillmentTimeline> Timelines { get; set; } = new();
    public double AverageFulfillmentDays { get; set; }
}

public class DeliveryPerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalDeliveries { get; set; }
    public double OnTimeDeliveryRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public List<SupplierDeliveryPerformance> SupplierPerformances { get; set; } = new();
    public List<DeliveryDelayAnalysis> DelayAnalyses { get; set; } = new();
    public List<MonthlyDeliveryTrend> MonthlyTrends { get; set; } = new();
}

// Supporting classes
public class OrderMetrics
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalValue { get; set; }
    public double CompletionRate { get; set; }
    public double AverageOrderValue { get; set; }
}

public class SupplierMetrics
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrdersAssigned { get; set; }
    public int OrdersCompleted { get; set; }
    public decimal TotalValue { get; set; }
    public double OnTimeDeliveryRate { get; set; }
    public double QualityScore { get; set; }
    public double CapacityUtilization { get; set; }
}

public class DeliveryMetrics
{
    public int TotalDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int LateDeliveries { get; set; }
    public double OnTimeRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public double AverageDelayDays { get; set; }
}

public class PerformanceTrend
{
    public DateTime Period { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public double ChangeFromPrevious { get; set; }
}

public class SupplierDistribution
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalValue { get; set; }
    public double Percentage { get; set; }
    public double CapacityUtilization { get; set; }
}

public class ProductTypeDistribution
{
    public string ProductType { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalValue { get; set; }
    public double Percentage { get; set; }
    public List<SupplierDistribution> SupplierBreakdown { get; set; } = new();
}

public class CapacityUtilization
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int UsedCapacity { get; set; }
    public double UtilizationRate { get; set; }
    public int AvailableCapacity { get; set; }
}

public class OrderStatusSummary
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public decimal TotalValue { get; set; }
    public double AverageDaysInStatus { get; set; }
}

public class CustomerOrderSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalValue { get; set; }
    public double CompletionRate { get; set; }
    public double AverageFulfillmentDays { get; set; }
}

public class FulfillmentTimeline
{
    public string Stage { get; set; } = string.Empty;
    public double AverageDays { get; set; }
    public double MinDays { get; set; }
    public double MaxDays { get; set; }
    public int OrderCount { get; set; }
}

public class SupplierDeliveryPerformance
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public double OnTimeRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public double AverageDelayDays { get; set; }
    public string PerformanceGrade { get; set; } = string.Empty;
}

public class DeliveryDelayAnalysis
{
    public string DelayCategory { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public double AverageDelayDays { get; set; }
    public List<string> CommonReasons { get; set; } = new();
}

public class MonthlyDeliveryTrend
{
    public DateTime Month { get; set; }
    public int TotalDeliveries { get; set; }
    public double OnTimeRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public double ChangeFromPreviousMonth { get; set; }
}