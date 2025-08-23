using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class OrderStatusHistory : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public OrderStatus FromStatus { get; set; }

    [Required]
    public OrderStatus ToStatus { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; }

    public Guid? ChangedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }

    // Navigation properties
    public CustomerOrder Order { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}