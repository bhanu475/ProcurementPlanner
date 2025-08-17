using ProcurementPlanner.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.API.Models;

public class CreateOrderDto
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
    [MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
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

public class UpdateOrderDto
{
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    public DateTime? RequestedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<UpdateOrderItemDto>? Items { get; set; }
}

public class UpdateOrderItemDto
{
    public Guid? Id { get; set; }

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

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}

public class OrderFilterDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
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

public class DashboardFilterDto
{
    public ProductType? ProductType { get; set; }
    public DateTime? DeliveryDateFrom { get; set; }
    public DateTime? DeliveryDateTo { get; set; }
    public string? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
}

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsOverdue { get; set; }
}

public class OrderItemResponseDto
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Specifications { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PagedOrderResponseDto
{
    public List<OrderResponseDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class OrderDashboardResponseDto
{
    public int TotalOrders { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> ProductTypeCounts { get; set; } = new();
    public int OverdueOrders { get; set; }
    public List<OrdersByDeliveryDateDto> OrdersByDeliveryDate { get; set; } = new();
    public List<OrdersByCustomerDto> TopCustomers { get; set; } = new();
    public decimal TotalValue { get; set; }
}

public class OrdersByDeliveryDateDto
{
    public DateTime DeliveryDate { get; set; }
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
}

public class OrdersByCustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}