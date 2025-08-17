using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IDistributionAlgorithmService
{
    /// <summary>
    /// Generates a fair distribution suggestion for a customer order across available suppliers
    /// </summary>
    /// <param name="customerOrder">The customer order to distribute</param>
    /// <returns>Distribution suggestion with supplier allocations</returns>
    Task<DistributionSuggestion> GenerateDistributionSuggestionAsync(CustomerOrder customerOrder);

    /// <summary>
    /// Validates that a distribution plan doesn't exceed supplier capacities
    /// </summary>
    /// <param name="distributionPlan">The distribution plan to validate</param>
    /// <returns>Validation result with any errors</returns>
    Task<DistributionValidationResult> ValidateDistributionAsync(DistributionPlan distributionPlan);

    /// <summary>
    /// Gets available suppliers for a specific product type with their capacity information
    /// </summary>
    /// <param name="productType">The product type to find suppliers for</param>
    /// <param name="requiredQuantity">The total quantity needed</param>
    /// <returns>List of eligible suppliers with capacity details</returns>
    Task<List<SupplierAllocationInfo>> GetEligibleSuppliersAsync(ProductType productType, int requiredQuantity);

    /// <summary>
    /// Calculates optimal distribution based on supplier performance and capacity
    /// </summary>
    /// <param name="suppliers">Available suppliers</param>
    /// <param name="totalQuantity">Total quantity to distribute</param>
    /// <param name="distributionStrategy">Strategy to use for distribution</param>
    /// <returns>Optimized supplier allocations</returns>
    List<SupplierAllocation> CalculateOptimalDistribution(
        List<SupplierAllocationInfo> suppliers, 
        int totalQuantity, 
        DistributionStrategy distributionStrategy = DistributionStrategy.Balanced);
}