using ProcurementPlanner.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Models;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    public DateTime RequestedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public List<CreateOrderItemRequest> Items { get; set; } = new();

    [Required]
    public Guid CreatedBy { get; set; }
}

public class CreateOrderItemRequest
{
    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Specifications { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }
}

public class UpdateOrderRequest
{
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    public DateTime? RequestedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<UpdateOrderItemRequest>? Items { get; set; }
}

public class UpdateOrderItemRequest
{
    public Guid? Id { get; set; } // Null for new items

    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Specifications { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }
}

public class OrderFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public ProductType? ProductType { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? DeliveryDateFrom { get; set; }
    public DateTime? DeliveryDateTo { get; set; }
    public DateTime? CreatedDateFrom { get; set; }
    public DateTime? CreatedDateTo { get; set; }
    public string? OrderNumber { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class DashboardFilterRequest
{
    public ProductType? ProductType { get; set; }
    public DateTime? DeliveryDateFrom { get; set; }
    public DateTime? DeliveryDateTo { get; set; }
    public string? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
}

public class OrderDashboardSummary
{
    public int TotalOrders { get; set; }
    public int OrdersByStatus { get; set; }
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();
    public Dictionary<ProductType, int> ProductTypeCounts { get; set; } = new();
    public int OverdueOrders { get; set; }
    public List<OrdersByDeliveryDate> OrdersByDeliveryDate { get; set; } = new();
    public List<OrdersByCustomer> TopCustomers { get; set; } = new();
    public decimal TotalValue { get; set; }
}

public class OrdersByDeliveryDate
{
    public DateTime DeliveryDate { get; set; }
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
}

public class OrdersByCustomer
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}