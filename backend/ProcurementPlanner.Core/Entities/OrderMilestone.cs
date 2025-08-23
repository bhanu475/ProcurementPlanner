using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Entities;

public class OrderMilestone : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime? TargetDate { get; set; }

    public DateTime? ActualDate { get; set; }

    [Required]
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

    // Navigation properties
    public CustomerOrder Order { get; set; } = null!;

    // Business logic methods
    public bool IsOverdue => Status != MilestoneStatus.Completed && 
                           Status != MilestoneStatus.Cancelled && 
                           TargetDate.HasValue && 
                           TargetDate.Value < DateTime.UtcNow;

    public void MarkCompleted()
    {
        Status = MilestoneStatus.Completed;
        ActualDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = MilestoneStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public int DaysUntilTarget => TargetDate.HasValue 
        ? (int)(TargetDate.Value - DateTime.UtcNow).TotalDays 
        : 0;
}