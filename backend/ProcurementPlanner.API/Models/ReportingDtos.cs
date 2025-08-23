using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.API.Models;

// Request DTOs
public class PerformanceMetricsReportRequestDto
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public Guid? SupplierId { get; set; }
    public string? ProductType { get; set; }
    public bool IncludeOrderMetrics { get; set; } = true;
    public bool IncludeSupplierMetrics { get; set; } = true;
    public bool IncludeDeliveryMetrics { get; set; } = true;
}

public class SupplierDistributionReportRequestDto
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public string? ProductType { get; set; }
    public bool GroupByProductType { get; set; } = true;
    public bool GroupByMonth { get; set; } = false;
    public bool IncludeCapacityUtilization { get; set; } = true;
}

public class OrderFulfillmentReportRequestDto
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public string? CustomerId { get; set; }
    public string? ProductType { get; set; }
    public bool GroupByStatus { get; set; } = true;
    public bool GroupByCustomer { get; set; } = false;
    public bool IncludeTimelines { get; set; } = true;
}

public class DeliveryPerformanceReportRequestDto
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public Guid? SupplierId { get; set; }
    public string? ProductType { get; set; }
    public bool GroupBySupplier { get; set; } = true;
    public bool GroupByMonth { get; set; } = false;
    public bool IncludeDelayAnalysis { get; set; } = true;
}

// Response DTOs
public class PerformanceMetricsReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public OrderMetricsDto OrderMetrics { get; set; } = new();
    public List<SupplierMetricsDto> SupplierMetrics { get; set; } = new();
    public DeliveryMetricsDto DeliveryMetrics { get; set; } = new();
}

public class SupplierDistributionReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalValue { get; set; }
    public List<SupplierDistributionDto> Distributions { get; set; } = new();
}

public class OrderFulfillmentReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public double AverageFulfillmentDays { get; set; }
    public List<OrderStatusSummaryDto> StatusSummaries { get; set; } = new();
}

public class DeliveryPerformanceReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalDeliveries { get; set; }
    public double OnTimeDeliveryRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public List<SupplierDeliveryPerformanceDto> SupplierPerformances { get; set; } = new();
}

// Supporting DTOs
public class OrderMetricsDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalValue { get; set; }
    public double CompletionRate { get; set; }
    public double AverageOrderValue { get; set; }
}

public class SupplierMetricsDto
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

public class DeliveryMetricsDto
{
    public int TotalDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int LateDeliveries { get; set; }
    public double OnTimeRate { get; set; }
    public double AverageDeliveryDays { get; set; }
    public double AverageDelayDays { get; set; }
}

public class SupplierDistributionDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalValue { get; set; }
    public double Percentage { get; set; }
    public double CapacityUtilization { get; set; }
}

public class OrderStatusSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public decimal TotalValue { get; set; }
    public double AverageDaysInStatus { get; set; }
}

public class SupplierDeliveryPerformanceDto
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