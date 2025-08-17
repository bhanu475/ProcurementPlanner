using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.Core.Interfaces;

public interface ISupplierManagementService
{
    Task<List<Supplier>> GetAvailableSuppliersAsync(ProductType productType, int requiredCapacity);
    Task<Supplier?> GetSupplierByIdAsync(Guid supplierId);
    Task<List<Supplier>> GetAllSuppliersAsync();
    Task<Supplier> CreateSupplierAsync(Supplier supplier);
    Task<Supplier> UpdateSupplierAsync(Supplier supplier);
    Task<Supplier> UpdateSupplierCapacityAsync(Guid supplierId, ProductType productType, int maxCapacity, int currentCommitments);
    Task<SupplierPerformanceMetrics?> GetSupplierPerformanceAsync(Guid supplierId);
    Task UpdateSupplierPerformanceAsync(Guid supplierId, bool wasOnTime, decimal qualityScore, int deliveryDays);
    Task<bool> ValidateSupplierEligibilityAsync(Guid supplierId, ProductType productType, int requiredQuantity);
    Task<List<Supplier>> GetSuppliersByPerformanceAsync(ProductType productType, decimal minOnTimeRate = 0.8m, decimal minQualityScore = 3.0m);
    Task DeactivateSupplierAsync(Guid supplierId);
    Task ActivateSupplierAsync(Guid supplierId);
    Task<int> GetTotalAvailableCapacityAsync(ProductType productType);
    Task<List<SupplierCapability>> GetSupplierCapabilitiesAsync(Guid supplierId);
    Task<SupplierCapability> UpdateSupplierCapabilityAsync(Guid supplierId, ProductType productType, SupplierCapability capability);
}