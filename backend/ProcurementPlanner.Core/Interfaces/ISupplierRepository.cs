using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<List<Supplier>> GetActiveSuppliersByProductTypeAsync(ProductType productType);
    Task<List<Supplier>> GetSuppliersByCapacityAsync(ProductType productType, int minCapacity);
    Task<Supplier?> GetSupplierWithCapabilitiesAsync(Guid supplierId);
    Task<Supplier?> GetSupplierWithPerformanceAsync(Guid supplierId);
    Task<List<Supplier>> GetSuppliersByPerformanceThresholdAsync(ProductType productType, decimal minOnTimeRate, decimal minQualityScore);
    Task<int> GetTotalCapacityByProductTypeAsync(ProductType productType);
    Task<List<SupplierCapability>> GetCapabilitiesBySupplierAsync(Guid supplierId);
    Task<SupplierCapability?> GetSupplierCapabilityAsync(Guid supplierId, ProductType productType);
    Task<SupplierCapability> UpdateCapabilityAsync(SupplierCapability capability);
    Task<SupplierPerformanceMetrics?> GetPerformanceMetricsAsync(Guid supplierId);
    Task<SupplierPerformanceMetrics> UpdatePerformanceMetricsAsync(SupplierPerformanceMetrics metrics);
}