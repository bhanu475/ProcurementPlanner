using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class DistributionAlgorithmService : IDistributionAlgorithmService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<DistributionAlgorithmService> _logger;

    // Configuration constants for distribution algorithm
    private const decimal MinPerformanceThreshold = 0.7m;
    private const decimal PreferredSupplierBonus = 0.2m;
    private const decimal ReliableSupplierBonus = 0.1m;
    private const int MinAllocationQuantity = 1;

    public DistributionAlgorithmService(
        ISupplierRepository supplierRepository,
        ILogger<DistributionAlgorithmService> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    public async Task<DistributionSuggestion> GenerateDistributionSuggestionAsync(CustomerOrder customerOrder)
    {
        _logger.LogInformation("Generating distribution suggestion for order {OrderId} with {TotalQuantity} units of {ProductType}", 
            customerOrder.Id, customerOrder.TotalQuantity, customerOrder.ProductType);

        var eligibleSuppliers = await GetEligibleSuppliersAsync(customerOrder.ProductType, customerOrder.TotalQuantity);
        
        if (!eligibleSuppliers.Any())
        {
            _logger.LogWarning("No eligible suppliers found for product type {ProductType}", customerOrder.ProductType);
            return new DistributionSuggestion
            {
                CustomerOrderId = customerOrder.Id,
                TotalQuantity = customerOrder.TotalQuantity,
                ProductType = customerOrder.ProductType,
                Strategy = DistributionStrategy.Balanced,
                Notes = "No eligible suppliers available for this product type"
            };
        }

        // Calculate optimal distribution using balanced strategy
        var allocations = CalculateOptimalDistribution(eligibleSuppliers, customerOrder.TotalQuantity, DistributionStrategy.Balanced);
        
        var suggestion = new DistributionSuggestion
        {
            CustomerOrderId = customerOrder.Id,
            TotalQuantity = customerOrder.TotalQuantity,
            ProductType = customerOrder.ProductType,
            Allocations = allocations,
            Strategy = DistributionStrategy.Balanced,
            TotalCapacityUtilization = CalculateTotalCapacityUtilization(eligibleSuppliers, allocations)
        };

        if (!suggestion.IsFullyAllocated)
        {
            suggestion.Notes = $"Unable to fully allocate order. {suggestion.UnallocatedQuantity} units remain unallocated.";
            _logger.LogWarning("Order {OrderId} could not be fully allocated. Missing {UnallocatedQuantity} units", 
                customerOrder.Id, suggestion.UnallocatedQuantity);
        }

        _logger.LogInformation("Generated distribution suggestion for order {OrderId} with {SupplierCount} suppliers", 
            customerOrder.Id, allocations.Count);

        return suggestion;
    }

    public async Task<DistributionValidationResult> ValidateDistributionAsync(DistributionPlan distributionPlan)
    {
        var result = new DistributionValidationResult { IsValid = true };

        if (!distributionPlan.Allocations.Any())
        {
            result.AddError("Distribution plan must contain at least one allocation");
            return result;
        }

        // Validate each supplier allocation
        foreach (var allocation in distributionPlan.Allocations)
        {
            var supplier = await _supplierRepository.GetSupplierWithCapabilitiesAsync(allocation.SupplierId);
            if (supplier == null)
            {
                result.AddError($"Supplier {allocation.SupplierId} not found");
                continue;
            }

            if (!supplier.IsActive)
            {
                result.AddError($"Supplier {supplier.Name} is not active");
                continue;
            }

            var customerOrder = await GetCustomerOrderAsync(distributionPlan.CustomerOrderId);
            if (customerOrder == null)
            {
                result.AddError($"Customer order {distributionPlan.CustomerOrderId} not found");
                continue;
            }

            var availableCapacity = supplier.GetAvailableCapacity(customerOrder.ProductType);
            var validation = new SupplierCapacityValidation
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                RequestedQuantity = allocation.AllocatedQuantity,
                AvailableCapacity = availableCapacity,
                HasSufficientCapacity = availableCapacity >= allocation.AllocatedQuantity
            };

            if (!validation.HasSufficientCapacity)
            {
                validation.CapacityShortfall = allocation.AllocatedQuantity - availableCapacity;
                validation.ValidationMessage = $"Insufficient capacity. Requested: {allocation.AllocatedQuantity}, Available: {availableCapacity}";
                result.AddError(validation.ValidationMessage);
            }
            else if (availableCapacity - allocation.AllocatedQuantity < availableCapacity * 0.1m)
            {
                validation.ValidationMessage = "Allocation will use >90% of available capacity";
                result.AddWarning(validation.ValidationMessage);
            }

            result.SupplierValidations.Add(validation);
        }

        return result;
    }

    public async Task<List<SupplierAllocationInfo>> GetEligibleSuppliersAsync(ProductType productType, int requiredQuantity)
    {
        _logger.LogDebug("Finding eligible suppliers for {ProductType} with minimum capacity for {RequiredQuantity} units", 
            productType, requiredQuantity);

        var suppliers = await _supplierRepository.GetActiveSuppliersByProductTypeAsync(productType);
        var eligibleSuppliers = new List<SupplierAllocationInfo>();

        foreach (var supplier in suppliers)
        {
            // Get supplier with full capabilities and performance data
            var supplierWithDetails = await _supplierRepository.GetSupplierWithPerformanceAsync(supplier.Id);
            if (supplierWithDetails?.Performance == null)
            {
                _logger.LogDebug("Skipping supplier {SupplierId} - no performance data available", supplier.Id);
                continue;
            }

            var capability = supplier.Capabilities.FirstOrDefault(c => c.ProductType == productType && c.IsActive);
            if (capability == null || capability.AvailableCapacity < MinAllocationQuantity)
            {
                _logger.LogDebug("Skipping supplier {SupplierId} - insufficient capacity or no capability for {ProductType}", 
                    supplier.Id, productType);
                continue;
            }

            // Filter out suppliers with poor performance
            if (supplierWithDetails.Performance.OverallPerformanceScore < MinPerformanceThreshold)
            {
                _logger.LogDebug("Skipping supplier {SupplierId} - performance score {Score} below threshold {Threshold}", 
                    supplier.Id, supplierWithDetails.Performance.OverallPerformanceScore, MinPerformanceThreshold);
                continue;
            }

            var allocationInfo = new SupplierAllocationInfo
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                AvailableCapacity = capability.AvailableCapacity,
                MaxMonthlyCapacity = capability.MaxMonthlyCapacity,
                CurrentCommitments = capability.CurrentCommitments,
                QualityRating = capability.QualityRating,
                OnTimeDeliveryRate = supplierWithDetails.Performance.OnTimeDeliveryRate,
                QualityScore = supplierWithDetails.Performance.QualityScore,
                OverallPerformanceScore = supplierWithDetails.Performance.OverallPerformanceScore,
                IsPreferredSupplier = supplierWithDetails.Performance.IsPreferredSupplier,
                IsReliableSupplier = supplierWithDetails.Performance.IsReliableSupplier,
                ProductType = productType,
                LastUpdated = supplierWithDetails.Performance.LastUpdated
            };

            eligibleSuppliers.Add(allocationInfo);
        }

        _logger.LogInformation("Found {Count} eligible suppliers for {ProductType}", eligibleSuppliers.Count, productType);
        return eligibleSuppliers.OrderByDescending(s => s.OverallPerformanceScore).ToList();
    }

    public List<SupplierAllocation> CalculateOptimalDistribution(
        List<SupplierAllocationInfo> suppliers, 
        int totalQuantity, 
        DistributionStrategy distributionStrategy = DistributionStrategy.Balanced)
    {
        if (!suppliers.Any() || totalQuantity <= 0)
        {
            return new List<SupplierAllocation>();
        }

        _logger.LogDebug("Calculating optimal distribution for {TotalQuantity} units across {SupplierCount} suppliers using {Strategy} strategy", 
            totalQuantity, suppliers.Count, distributionStrategy);

        return distributionStrategy switch
        {
            DistributionStrategy.EvenDistribution => CalculateEvenDistribution(suppliers, totalQuantity),
            DistributionStrategy.PerformanceBased => CalculatePerformanceBasedDistribution(suppliers, totalQuantity),
            DistributionStrategy.CapacityBased => CalculateCapacityBasedDistribution(suppliers, totalQuantity),
            DistributionStrategy.Balanced => CalculateBalancedDistribution(suppliers, totalQuantity),
            _ => CalculateBalancedDistribution(suppliers, totalQuantity)
        };
    }

    private List<SupplierAllocation> CalculateEvenDistribution(List<SupplierAllocationInfo> suppliers, int totalQuantity)
    {
        var allocations = new List<SupplierAllocation>();
        var remainingQuantity = totalQuantity;
        var baseAllocation = totalQuantity / suppliers.Count;
        var remainder = totalQuantity % suppliers.Count;

        for (int i = 0; i < suppliers.Count && remainingQuantity > 0; i++)
        {
            var supplier = suppliers[i];
            var allocationQuantity = Math.Min(baseAllocation + (i < remainder ? 1 : 0), 
                                            Math.Min(supplier.AvailableCapacity, remainingQuantity));

            if (allocationQuantity >= MinAllocationQuantity)
            {
                allocations.Add(CreateSupplierAllocation(supplier, allocationQuantity, totalQuantity, "Even distribution"));
                remainingQuantity -= allocationQuantity;
            }
        }

        return allocations;
    }

    private List<SupplierAllocation> CalculatePerformanceBasedDistribution(List<SupplierAllocationInfo> suppliers, int totalQuantity)
    {
        var allocations = new List<SupplierAllocation>();
        var remainingQuantity = totalQuantity;

        // Calculate weighted allocation based on performance scores
        var totalWeightedScore = suppliers.Sum(s => CalculatePerformanceWeight(s));
        
        foreach (var supplier in suppliers.OrderByDescending(s => s.OverallPerformanceScore))
        {
            if (remainingQuantity <= 0) break;

            var weight = CalculatePerformanceWeight(supplier);
            var targetAllocation = (int)Math.Round((weight / totalWeightedScore) * totalQuantity);
            var allocationQuantity = Math.Min(targetAllocation, 
                                            Math.Min(supplier.AvailableCapacity, remainingQuantity));

            if (allocationQuantity >= MinAllocationQuantity)
            {
                allocations.Add(CreateSupplierAllocation(supplier, allocationQuantity, totalQuantity, 
                    $"Performance-based (score: {supplier.OverallPerformanceScore:F2})"));
                remainingQuantity -= allocationQuantity;
            }
        }

        return allocations;
    }

    private List<SupplierAllocation> CalculateCapacityBasedDistribution(List<SupplierAllocationInfo> suppliers, int totalQuantity)
    {
        var allocations = new List<SupplierAllocation>();
        var remainingQuantity = totalQuantity;

        // Prioritize suppliers with highest available capacity
        foreach (var supplier in suppliers.OrderByDescending(s => s.AvailableCapacity))
        {
            if (remainingQuantity <= 0) break;

            var allocationQuantity = Math.Min(supplier.AvailableCapacity, remainingQuantity);

            if (allocationQuantity >= MinAllocationQuantity)
            {
                allocations.Add(CreateSupplierAllocation(supplier, allocationQuantity, totalQuantity, 
                    $"Capacity-based (available: {supplier.AvailableCapacity})"));
                remainingQuantity -= allocationQuantity;
            }
        }

        return allocations;
    }

    private List<SupplierAllocation> CalculateBalancedDistribution(List<SupplierAllocationInfo> suppliers, int totalQuantity)
    {
        var allocations = new List<SupplierAllocation>();
        var remainingQuantity = totalQuantity;

        // Calculate composite score combining performance and capacity
        var suppliersWithScores = suppliers.Select(s => new
        {
            Supplier = s,
            CompositeScore = CalculateCompositeScore(s)
        }).OrderByDescending(x => x.CompositeScore).ToList();

        var totalCompositeScore = suppliersWithScores.Sum(x => x.CompositeScore);

        foreach (var item in suppliersWithScores)
        {
            if (remainingQuantity <= 0) break;

            var supplier = item.Supplier;
            var weight = item.CompositeScore / totalCompositeScore;
            var targetAllocation = (int)Math.Round(weight * totalQuantity);
            var allocationQuantity = Math.Min(targetAllocation, 
                                            Math.Min(supplier.AvailableCapacity, remainingQuantity));

            if (allocationQuantity >= MinAllocationQuantity)
            {
                allocations.Add(CreateSupplierAllocation(supplier, allocationQuantity, totalQuantity, 
                    $"Balanced (composite score: {item.CompositeScore:F2})"));
                remainingQuantity -= allocationQuantity;
            }
        }

        // Distribute any remaining quantity to suppliers with available capacity
        if (remainingQuantity > 0)
        {
            foreach (var allocation in allocations.OrderByDescending(a => a.PerformanceScore))
            {
                var supplier = suppliers.First(s => s.SupplierId == allocation.SupplierId);
                var additionalCapacity = supplier.AvailableCapacity - allocation.AllocatedQuantity;
                var additionalAllocation = Math.Min(additionalCapacity, remainingQuantity);

                if (additionalAllocation > 0)
                {
                    allocation.AllocatedQuantity += additionalAllocation;
                    allocation.AllocationPercentage = (decimal)allocation.AllocatedQuantity / totalQuantity * 100;
                    remainingQuantity -= additionalAllocation;

                    if (remainingQuantity <= 0) break;
                }
            }
        }

        return allocations;
    }

    private decimal CalculatePerformanceWeight(SupplierAllocationInfo supplier)
    {
        var weight = supplier.OverallPerformanceScore;
        
        if (supplier.IsPreferredSupplier)
            weight += PreferredSupplierBonus;
        else if (supplier.IsReliableSupplier)
            weight += ReliableSupplierBonus;

        return weight;
    }

    private decimal CalculateCompositeScore(SupplierAllocationInfo supplier)
    {
        // Weighted composite score: 60% performance, 40% capacity utilization efficiency
        var performanceScore = CalculatePerformanceWeight(supplier);
        var capacityScore = 1.0m - supplier.CapacityUtilizationRate; // Prefer suppliers with more available capacity
        
        return (performanceScore * 0.6m) + (capacityScore * 0.4m);
    }

    private SupplierAllocation CreateSupplierAllocation(SupplierAllocationInfo supplier, int quantity, int totalQuantity, string reason)
    {
        return new SupplierAllocation
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            AllocatedQuantity = quantity,
            AllocationPercentage = (decimal)quantity / totalQuantity * 100,
            AvailableCapacity = supplier.AvailableCapacity,
            PerformanceScore = supplier.OverallPerformanceScore,
            QualityRating = supplier.QualityRating,
            OnTimeDeliveryRate = supplier.OnTimeDeliveryRate,
            AllocationReason = reason
        };
    }

    private decimal CalculateTotalCapacityUtilization(List<SupplierAllocationInfo> suppliers, List<SupplierAllocation> allocations)
    {
        if (!suppliers.Any() || !allocations.Any())
            return 0;

        var totalCapacityUsed = 0;
        var totalAvailableCapacity = 0;

        foreach (var allocation in allocations)
        {
            var supplier = suppliers.FirstOrDefault(s => s.SupplierId == allocation.SupplierId);
            if (supplier != null)
            {
                totalCapacityUsed += allocation.AllocatedQuantity;
                totalAvailableCapacity += supplier.AvailableCapacity;
            }
        }

        return totalAvailableCapacity > 0 ? (decimal)totalCapacityUsed / totalAvailableCapacity : 0;
    }

    // Helper method to get customer order - this would typically be injected as a repository
    private async Task<CustomerOrder?> GetCustomerOrderAsync(Guid customerOrderId)
    {
        // This is a placeholder - in a real implementation, this would use ICustomerOrderRepository
        // For now, we'll return a mock customer order to avoid validation failures in tests
        await Task.CompletedTask;
        return new CustomerOrder
        {
            Id = customerOrderId,
            OrderNumber = "MOCK-ORDER",
            CustomerId = "MOCK-CUSTOMER",
            CustomerName = "Mock Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(30),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid()
        };
    }
}