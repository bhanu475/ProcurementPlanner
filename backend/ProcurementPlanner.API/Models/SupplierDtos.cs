using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Models;

public class CreateSupplierRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactPersonName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public List<CreateSupplierCapabilityRequest> Capabilities { get; set; } = new();
}

public class UpdateSupplierRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactPersonName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CreateSupplierCapabilityRequest
{
    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max monthly capacity must be greater than 0")]
    public int MaxMonthlyCapacity { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Current commitments cannot be negative")]
    public int CurrentCommitments { get; set; }

    [Required]
    [Range(0, 5, ErrorMessage = "Quality rating must be between 0 and 5")]
    public decimal QualityRating { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateSupplierCapacityRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max monthly capacity must be greater than 0")]
    public int MaxMonthlyCapacity { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Current commitments cannot be negative")]
    public int CurrentCommitments { get; set; }

    [Range(0, 5, ErrorMessage = "Quality rating must be between 0 and 5")]
    public decimal? QualityRating { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateSupplierPerformanceRequest
{
    [Required]
    public bool WasOnTime { get; set; }

    [Required]
    [Range(0, 5, ErrorMessage = "Quality score must be between 0 and 5")]
    public decimal QualityScore { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Delivery days must be greater than 0")]
    public int DeliveryDays { get; set; }
}

public class SupplierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SupplierCapabilityResponse> Capabilities { get; set; } = new();
    public SupplierPerformanceResponse? Performance { get; set; }
}

public class SupplierCapabilityResponse
{
    public Guid Id { get; set; }
    public ProductType ProductType { get; set; }
    public int MaxMonthlyCapacity { get; set; }
    public int CurrentCommitments { get; set; }
    public int AvailableCapacity { get; set; }
    public decimal QualityRating { get; set; }
    public decimal CapacityUtilizationRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsOverCommitted { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SupplierPerformanceResponse
{
    public Guid Id { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public int TotalOrdersOnTime { get; set; }
    public int TotalOrdersLate { get; set; }
    public int TotalOrdersCancelled { get; set; }
    public decimal? AverageDeliveryDays { get; set; }
    public decimal? CustomerSatisfactionRate { get; set; }
    public decimal OverallPerformanceScore { get; set; }
    public decimal CancellationRate { get; set; }
    public bool IsReliableSupplier { get; set; }
    public bool IsPreferredSupplier { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SupplierFilterRequest
{
    public ProductType? ProductType { get; set; }
    public int? MinCapacity { get; set; }
    public decimal? MinOnTimeRate { get; set; }
    public decimal? MinQualityScore { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AvailableSuppliersRequest
{
    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Required capacity must be greater than 0")]
    public int RequiredCapacity { get; set; }
}

public class SupplierSummaryResponse
{
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int InactiveSuppliers { get; set; }
    public Dictionary<ProductType, int> SuppliersByProductType { get; set; } = new();
    public Dictionary<ProductType, int> TotalCapacityByProductType { get; set; } = new();
    public decimal AveragePerformanceScore { get; set; }
    public int ReliableSuppliers { get; set; }
    public int PreferredSuppliers { get; set; }
}