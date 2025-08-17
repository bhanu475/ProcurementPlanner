using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;

namespace ProcurementPlanner.Infrastructure.Services;

public class SupplierManagementService : ISupplierManagementService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<SupplierManagementService> _logger;

    public SupplierManagementService(
        ISupplierRepository supplierRepository,
        ILogger<SupplierManagementService> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    public async Task<List<Supplier>> GetAvailableSuppliersAsync(ProductType productType, int requiredCapacity)
    {
        _logger.LogInformation("Getting available suppliers for product type {ProductType} with capacity {RequiredCapacity}", 
            productType, requiredCapacity);

        var suppliers = await _supplierRepository.GetSuppliersByCapacityAsync(productType, requiredCapacity);
        
        // Filter suppliers that are eligible and have sufficient capacity
        var availableSuppliers = suppliers
            .Where(s => s.IsActive && s.HasCapacityFor(productType, requiredCapacity))
            .OrderByDescending(s => s.Performance?.OverallPerformanceScore ?? 0)
            .ThenByDescending(s => s.GetAvailableCapacity(productType))
            .ToList();

        _logger.LogInformation("Found {Count} available suppliers for product type {ProductType}", 
            availableSuppliers.Count, productType);

        return availableSuppliers;
    }

    public async Task<Supplier?> GetSupplierByIdAsync(Guid supplierId)
    {
        _logger.LogInformation("Getting supplier by ID {SupplierId}", supplierId);
        return await _supplierRepository.GetSupplierWithCapabilitiesAsync(supplierId);
    }

    public async Task<List<Supplier>> GetAllSuppliersAsync()
    {
        _logger.LogInformation("Getting all suppliers");
        return (await _supplierRepository.GetAllAsync()).ToList();
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        _logger.LogInformation("Creating new supplier {SupplierName}", supplier.Name);

        // Validate supplier data
        supplier.ValidateContactInformation();

        // Validate capabilities
        foreach (var capability in supplier.Capabilities)
        {
            capability.ValidateCapacity();
        }

        // Set creation timestamp
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;

        var createdSupplier = await _supplierRepository.AddAsync(supplier);
        
        _logger.LogInformation("Successfully created supplier {SupplierId} - {SupplierName}", 
            createdSupplier.Id, createdSupplier.Name);

        return createdSupplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        _logger.LogInformation("Updating supplier {SupplierId}", supplier.Id);

        // Validate supplier data
        supplier.ValidateContactInformation();

        // Set update timestamp
        supplier.UpdatedAt = DateTime.UtcNow;

        var updatedSupplier = await _supplierRepository.UpdateAsync(supplier);
        
        _logger.LogInformation("Successfully updated supplier {SupplierId}", supplier.Id);

        return updatedSupplier;
    }

    public async Task<Supplier> UpdateSupplierCapacityAsync(Guid supplierId, ProductType productType, int maxCapacity, int currentCommitments)
    {
        _logger.LogInformation("Updating capacity for supplier {SupplierId}, product type {ProductType}", 
            supplierId, productType);

        var supplier = await _supplierRepository.GetSupplierWithCapabilitiesAsync(supplierId);
        if (supplier == null)
        {
            throw new ArgumentException($"Supplier with ID {supplierId} not found", nameof(supplierId));
        }

        var capability = supplier.Capabilities.FirstOrDefault(c => c.ProductType == productType);
        if (capability == null)
        {
            // Create new capability
            capability = new SupplierCapability
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                ProductType = productType,
                MaxMonthlyCapacity = maxCapacity,
                CurrentCommitments = currentCommitments,
                QualityRating = 3.0m, // Default rating
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            supplier.Capabilities.Add(capability);
        }
        else
        {
            capability.MaxMonthlyCapacity = maxCapacity;
            capability.CurrentCommitments = currentCommitments;
            capability.UpdatedAt = DateTime.UtcNow;
        }

        // Validate the updated capability
        capability.ValidateCapacity();

        await _supplierRepository.UpdateCapabilityAsync(capability);
        
        _logger.LogInformation("Successfully updated capacity for supplier {SupplierId}, product type {ProductType}. " +
                             "Max: {MaxCapacity}, Current: {CurrentCommitments}, Available: {AvailableCapacity}", 
            supplierId, productType, maxCapacity, currentCommitments, capability.AvailableCapacity);

        return supplier;
    }

    public async Task<SupplierPerformanceMetrics?> GetSupplierPerformanceAsync(Guid supplierId)
    {
        _logger.LogInformation("Getting performance metrics for supplier {SupplierId}", supplierId);
        return await _supplierRepository.GetPerformanceMetricsAsync(supplierId);
    }

    public async Task UpdateSupplierPerformanceAsync(Guid supplierId, bool wasOnTime, decimal qualityScore, int deliveryDays)
    {
        _logger.LogInformation("Updating performance for supplier {SupplierId}. OnTime: {OnTime}, Quality: {Quality}, Days: {Days}", 
            supplierId, wasOnTime, qualityScore, deliveryDays);

        var metrics = await _supplierRepository.GetPerformanceMetricsAsync(supplierId);
        
        if (metrics == null)
        {
            // Create new performance metrics
            metrics = new SupplierPerformanceMetrics
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                OnTimeDeliveryRate = wasOnTime ? 1.0m : 0.0m,
                QualityScore = qualityScore,
                TotalOrdersCompleted = 0,
                TotalOrdersOnTime = 0,
                TotalOrdersLate = 0,
                TotalOrdersCancelled = 0,
                LastUpdated = DateTime.UtcNow,
                AverageDeliveryDays = deliveryDays,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Update metrics
        metrics.UpdateOnTimeDeliveryMetrics(wasOnTime);
        metrics.UpdateQualityScore(qualityScore);
        metrics.UpdateAverageDeliveryDays(deliveryDays);
        metrics.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdatePerformanceMetricsAsync(metrics);
        
        _logger.LogInformation("Successfully updated performance for supplier {SupplierId}. " +
                             "New OnTime Rate: {OnTimeRate}, Quality Score: {QualityScore}, Overall Score: {OverallScore}", 
            supplierId, metrics.OnTimeDeliveryRate, metrics.QualityScore, metrics.OverallPerformanceScore);
    }

    public async Task<bool> ValidateSupplierEligibilityAsync(Guid supplierId, ProductType productType, int requiredQuantity)
    {
        _logger.LogInformation("Validating eligibility for supplier {SupplierId}, product type {ProductType}, quantity {Quantity}", 
            supplierId, productType, requiredQuantity);

        var supplier = await _supplierRepository.GetSupplierWithPerformanceAsync(supplierId);
        
        if (supplier == null)
        {
            _logger.LogWarning("Supplier {SupplierId} not found", supplierId);
            return false;
        }

        if (!supplier.IsActive)
        {
            _logger.LogWarning("Supplier {SupplierId} is not active", supplierId);
            return false;
        }

        if (!supplier.CanHandleProductType(productType))
        {
            _logger.LogWarning("Supplier {SupplierId} cannot handle product type {ProductType}", supplierId, productType);
            return false;
        }

        if (!supplier.HasCapacityFor(productType, requiredQuantity))
        {
            _logger.LogWarning("Supplier {SupplierId} does not have sufficient capacity for {Quantity} units of {ProductType}", 
                supplierId, requiredQuantity, productType);
            return false;
        }

        // Check performance thresholds
        if (supplier.Performance != null)
        {
            const decimal minOnTimeRate = 0.7m; // 70% minimum on-time delivery
            const decimal minQualityScore = 2.5m; // 2.5/5 minimum quality score

            if (supplier.Performance.OnTimeDeliveryRate < minOnTimeRate)
            {
                _logger.LogWarning("Supplier {SupplierId} has low on-time delivery rate: {Rate}", 
                    supplierId, supplier.Performance.OnTimeDeliveryRate);
                return false;
            }

            if (supplier.Performance.QualityScore < minQualityScore)
            {
                _logger.LogWarning("Supplier {SupplierId} has low quality score: {Score}", 
                    supplierId, supplier.Performance.QualityScore);
                return false;
            }
        }

        _logger.LogInformation("Supplier {SupplierId} is eligible for {Quantity} units of {ProductType}", 
            supplierId, requiredQuantity, productType);
        
        return true;
    }

    public async Task<List<Supplier>> GetSuppliersByPerformanceAsync(ProductType productType, decimal minOnTimeRate = 0.8m, decimal minQualityScore = 3.0m)
    {
        _logger.LogInformation("Getting suppliers by performance for product type {ProductType}. " +
                             "Min OnTime Rate: {MinOnTimeRate}, Min Quality Score: {MinQualityScore}", 
            productType, minOnTimeRate, minQualityScore);

        var suppliers = await _supplierRepository.GetSuppliersByPerformanceThresholdAsync(productType, minOnTimeRate, minQualityScore);
        
        // Sort by overall performance score
        var sortedSuppliers = suppliers
            .OrderByDescending(s => s.Performance?.OverallPerformanceScore ?? 0)
            .ThenByDescending(s => s.GetAvailableCapacity(productType))
            .ToList();

        _logger.LogInformation("Found {Count} suppliers meeting performance criteria for product type {ProductType}", 
            sortedSuppliers.Count, productType);

        return sortedSuppliers;
    }

    public async Task DeactivateSupplierAsync(Guid supplierId)
    {
        _logger.LogInformation("Deactivating supplier {SupplierId}", supplierId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
        {
            throw new ArgumentException($"Supplier with ID {supplierId} not found", nameof(supplierId));
        }

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
        
        _logger.LogInformation("Successfully deactivated supplier {SupplierId}", supplierId);
    }

    public async Task ActivateSupplierAsync(Guid supplierId)
    {
        _logger.LogInformation("Activating supplier {SupplierId}", supplierId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
        {
            throw new ArgumentException($"Supplier with ID {supplierId} not found", nameof(supplierId));
        }

        supplier.IsActive = true;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
        
        _logger.LogInformation("Successfully activated supplier {SupplierId}", supplierId);
    }

    public async Task<int> GetTotalAvailableCapacityAsync(ProductType productType)
    {
        _logger.LogInformation("Getting total available capacity for product type {ProductType}", productType);
        
        var totalCapacity = await _supplierRepository.GetTotalCapacityByProductTypeAsync(productType);
        
        _logger.LogInformation("Total available capacity for product type {ProductType}: {Capacity}", 
            productType, totalCapacity);
        
        return totalCapacity;
    }

    public async Task<List<SupplierCapability>> GetSupplierCapabilitiesAsync(Guid supplierId)
    {
        _logger.LogInformation("Getting capabilities for supplier {SupplierId}", supplierId);
        return await _supplierRepository.GetCapabilitiesBySupplierAsync(supplierId);
    }

    public async Task<SupplierCapability> UpdateSupplierCapabilityAsync(Guid supplierId, ProductType productType, SupplierCapability capability)
    {
        _logger.LogInformation("Updating capability for supplier {SupplierId}, product type {ProductType}", 
            supplierId, productType);

        // Validate the capability
        capability.ValidateCapacity();
        capability.UpdatedAt = DateTime.UtcNow;

        var updatedCapability = await _supplierRepository.UpdateCapabilityAsync(capability);
        
        _logger.LogInformation("Successfully updated capability for supplier {SupplierId}, product type {ProductType}", 
            supplierId, productType);

        return updatedCapability;
    }
}