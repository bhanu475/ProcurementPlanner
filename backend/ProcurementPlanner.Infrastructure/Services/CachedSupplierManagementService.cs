using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class CachedSupplierManagementService : ISupplierManagementService
{
    private readonly ISupplierManagementService _supplierService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedSupplierManagementService> _logger;

    public CachedSupplierManagementService(
        ISupplierManagementService supplierService,
        ICacheService cacheService,
        ILogger<CachedSupplierManagementService> logger)
    {
        _supplierService = supplierService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<Supplier>> GetAvailableSuppliersAsync(ProductType productType, int requiredCapacity)
    {
        var cacheKey = string.Format(CacheKeys.SupplierAvailable, productType, requiredCapacity);
        
        var cachedSuppliers = await _cacheService.GetAsync<List<Supplier>>(cacheKey);
        if (cachedSuppliers != null)
        {
            _logger.LogDebug("Available suppliers for {ProductType} with capacity {Capacity} retrieved from cache", 
                productType, requiredCapacity);
            return cachedSuppliers;
        }

        _logger.LogDebug("Available suppliers for {ProductType} with capacity {Capacity} not found in cache, fetching from service", 
            productType, requiredCapacity);
        var suppliers = await _supplierService.GetAvailableSuppliersAsync(productType, requiredCapacity);
        
        await _cacheService.SetAsync(cacheKey, suppliers, CacheKeys.Expiration.Medium);
        _logger.LogDebug("Available suppliers cached for {Expiration} minutes", CacheKeys.Expiration.Medium.TotalMinutes);
        
        return suppliers;
    }

    public async Task<Supplier?> GetSupplierByIdAsync(Guid supplierId)
    {
        return await _supplierService.GetSupplierByIdAsync(supplierId);
    }

    public async Task<List<Supplier>> GetAllSuppliersAsync()
    {
        const string cacheKey = CacheKeys.SupplierList;
        
        var cachedSuppliers = await _cacheService.GetAsync<List<Supplier>>(cacheKey);
        if (cachedSuppliers != null)
        {
            _logger.LogDebug("All suppliers retrieved from cache");
            return cachedSuppliers;
        }

        _logger.LogDebug("All suppliers not found in cache, fetching from service");
        var suppliers = await _supplierService.GetAllSuppliersAsync();
        
        await _cacheService.SetAsync(cacheKey, suppliers, CacheKeys.Expiration.Long);
        _logger.LogDebug("All suppliers cached for {Expiration} hours", CacheKeys.Expiration.Long.TotalHours);
        
        return suppliers;
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        var result = await _supplierService.CreateSupplierAsync(supplier);
        
        // Invalidate supplier list cache
        await _cacheService.RemoveAsync(CacheKeys.SupplierList);
        await _cacheService.RemoveByPatternAsync(CacheKeys.SupplierAvailable.Replace("{0}", "*").Replace("{1}", "*"));
        
        return result;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        var result = await _supplierService.UpdateSupplierAsync(supplier);
        
        await InvalidateSupplierCacheAsync(supplier.Id);
        
        return result;
    }

    public async Task<Supplier> UpdateSupplierCapacityAsync(Guid supplierId, ProductType productType, int maxCapacity, int currentCommitments)
    {
        _logger.LogDebug("Updating supplier capacity for {SupplierId}", supplierId);
        var supplier = await _supplierService.UpdateSupplierCapacityAsync(supplierId, productType, maxCapacity, currentCommitments);
        
        // Invalidate related cache entries
        await InvalidateSupplierCacheAsync(supplierId);
        
        return supplier;
    }

    public async Task<SupplierPerformanceMetrics?> GetSupplierPerformanceAsync(Guid supplierId)
    {
        var cacheKey = string.Format(CacheKeys.SupplierPerformance, supplierId);
        
        var cachedMetrics = await _cacheService.GetAsync<SupplierPerformanceMetrics>(cacheKey);
        if (cachedMetrics != null)
        {
            _logger.LogDebug("Supplier performance metrics for {SupplierId} retrieved from cache", supplierId);
            return cachedMetrics;
        }

        _logger.LogDebug("Supplier performance metrics for {SupplierId} not found in cache, fetching from service", supplierId);
        var metrics = await _supplierService.GetSupplierPerformanceAsync(supplierId);
        
        if (metrics != null)
        {
            await _cacheService.SetAsync(cacheKey, metrics, CacheKeys.Expiration.Long);
            _logger.LogDebug("Supplier performance metrics cached for {Expiration} hours", CacheKeys.Expiration.Long.TotalHours);
        }
        
        return metrics;
    }

    public async Task UpdateSupplierPerformanceAsync(Guid supplierId, bool wasOnTime, decimal qualityScore, int deliveryDays)
    {
        _logger.LogDebug("Updating supplier performance for {SupplierId}", supplierId);
        await _supplierService.UpdateSupplierPerformanceAsync(supplierId, wasOnTime, qualityScore, deliveryDays);
        
        // Invalidate performance cache
        var performanceCacheKey = string.Format(CacheKeys.SupplierPerformance, supplierId);
        await _cacheService.RemoveAsync(performanceCacheKey);
        
        // Also invalidate available suppliers cache as performance affects availability
        await _cacheService.RemoveByPatternAsync(CacheKeys.SupplierAvailable.Replace("{0}", "*").Replace("{1}", "*"));
    }

    public async Task<bool> ValidateSupplierEligibilityAsync(Guid supplierId, ProductType productType, int requiredQuantity)
    {
        return await _supplierService.ValidateSupplierEligibilityAsync(supplierId, productType, requiredQuantity);
    }

    public async Task<List<Supplier>> GetSuppliersByPerformanceAsync(ProductType productType, decimal minOnTimeRate = 0.8m, decimal minQualityScore = 3.0m)
    {
        var cacheKey = $"suppliers:performance:{productType}:{minOnTimeRate}:{minQualityScore}";
        
        var cachedSuppliers = await _cacheService.GetAsync<List<Supplier>>(cacheKey);
        if (cachedSuppliers != null)
        {
            _logger.LogDebug("Suppliers by performance retrieved from cache");
            return cachedSuppliers;
        }

        _logger.LogDebug("Suppliers by performance not found in cache, fetching from service");
        var suppliers = await _supplierService.GetSuppliersByPerformanceAsync(productType, minOnTimeRate, minQualityScore);
        
        await _cacheService.SetAsync(cacheKey, suppliers, CacheKeys.Expiration.Long);
        _logger.LogDebug("Suppliers by performance cached for {Expiration} hours", CacheKeys.Expiration.Long.TotalHours);
        
        return suppliers;
    }

    public async Task DeactivateSupplierAsync(Guid supplierId)
    {
        await _supplierService.DeactivateSupplierAsync(supplierId);
        await InvalidateSupplierCacheAsync(supplierId);
    }

    public async Task ActivateSupplierAsync(Guid supplierId)
    {
        await _supplierService.ActivateSupplierAsync(supplierId);
        await InvalidateSupplierCacheAsync(supplierId);
    }

    public async Task<int> GetTotalAvailableCapacityAsync(ProductType productType)
    {
        var cacheKey = $"suppliers:total-capacity:{productType}";
        
        var cachedCapacity = await _cacheService.GetAsync<CachedValue<int>>(cacheKey);
        if (cachedCapacity != null)
        {
            _logger.LogDebug("Total available capacity for {ProductType} retrieved from cache", productType);
            return cachedCapacity.Value;
        }

        _logger.LogDebug("Total available capacity for {ProductType} not found in cache, fetching from service", productType);
        var capacity = await _supplierService.GetTotalAvailableCapacityAsync(productType);
        
        await _cacheService.SetAsync(cacheKey, new CachedValue<int>(capacity), CacheKeys.Expiration.Medium);
        _logger.LogDebug("Total available capacity cached for {Expiration} minutes", CacheKeys.Expiration.Medium.TotalMinutes);
        
        return capacity;
    }

    public async Task<List<SupplierCapability>> GetSupplierCapabilitiesAsync(Guid supplierId)
    {
        var cacheKey = string.Format(CacheKeys.SupplierCapabilities, supplierId);
        
        var cachedCapabilities = await _cacheService.GetAsync<List<SupplierCapability>>(cacheKey);
        if (cachedCapabilities != null)
        {
            _logger.LogDebug("Supplier capabilities for {SupplierId} retrieved from cache", supplierId);
            return cachedCapabilities;
        }

        _logger.LogDebug("Supplier capabilities for {SupplierId} not found in cache, fetching from service", supplierId);
        var capabilities = await _supplierService.GetSupplierCapabilitiesAsync(supplierId);
        
        await _cacheService.SetAsync(cacheKey, capabilities, CacheKeys.Expiration.Long);
        _logger.LogDebug("Supplier capabilities cached for {Expiration} hours", CacheKeys.Expiration.Long.TotalHours);
        
        return capabilities;
    }

    public async Task<SupplierCapability> UpdateSupplierCapabilityAsync(Guid supplierId, ProductType productType, SupplierCapability capability)
    {
        var result = await _supplierService.UpdateSupplierCapabilityAsync(supplierId, productType, capability);
        
        // Invalidate capabilities cache
        var capabilitiesCacheKey = string.Format(CacheKeys.SupplierCapabilities, supplierId);
        await _cacheService.RemoveAsync(capabilitiesCacheKey);
        
        // Also invalidate available suppliers cache
        await _cacheService.RemoveByPatternAsync(CacheKeys.SupplierAvailable.Replace("{0}", "*").Replace("{1}", "*"));
        
        return result;
    }

    private async Task InvalidateSupplierCacheAsync(Guid supplierId)
    {
        _logger.LogDebug("Invalidating cache for supplier {SupplierId}", supplierId);
        
        // Remove specific supplier caches
        var performanceCacheKey = string.Format(CacheKeys.SupplierPerformance, supplierId);
        var capabilitiesCacheKey = string.Format(CacheKeys.SupplierCapabilities, supplierId);
        
        await _cacheService.RemoveAsync(performanceCacheKey);
        await _cacheService.RemoveAsync(capabilitiesCacheKey);
        
        // Remove general supplier caches
        await _cacheService.RemoveAsync(CacheKeys.SupplierList);
        await _cacheService.RemoveByPatternAsync(CacheKeys.SupplierAvailable.Replace("{0}", "*").Replace("{1}", "*"));
    }
}