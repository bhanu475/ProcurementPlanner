using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Repositories;

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Supplier>> GetActiveSuppliersByProductTypeAsync(ProductType productType)
    {
        return await _context.Suppliers
            .Include(s => s.Capabilities)
            .Include(s => s.Performance)
            .Where(s => s.IsActive && s.Capabilities.Any(c => c.ProductType == productType && c.IsActive))
            .ToListAsync();
    }

    public async Task<List<Supplier>> GetSuppliersByCapacityAsync(ProductType productType, int minCapacity)
    {
        return await _context.Suppliers
            .Include(s => s.Capabilities)
            .Include(s => s.Performance)
            .Where(s => s.IsActive && 
                   s.Capabilities.Any(c => c.ProductType == productType && 
                                         c.IsActive && 
                                         c.AvailableCapacity >= minCapacity))
            .ToListAsync();
    }

    public async Task<Supplier?> GetSupplierWithCapabilitiesAsync(Guid supplierId)
    {
        return await _context.Suppliers
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.Id == supplierId);
    }

    public async Task<Supplier?> GetSupplierWithPerformanceAsync(Guid supplierId)
    {
        return await _context.Suppliers
            .Include(s => s.Performance)
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.Id == supplierId);
    }

    public async Task<List<Supplier>> GetSuppliersByPerformanceThresholdAsync(ProductType productType, decimal minOnTimeRate, decimal minQualityScore)
    {
        return await _context.Suppliers
            .Include(s => s.Capabilities)
            .Include(s => s.Performance)
            .Where(s => s.IsActive && 
                   s.Capabilities.Any(c => c.ProductType == productType && c.IsActive) &&
                   s.Performance != null &&
                   s.Performance.OnTimeDeliveryRate >= minOnTimeRate &&
                   s.Performance.QualityScore >= minQualityScore)
            .ToListAsync();
    }

    public async Task<int> GetTotalCapacityByProductTypeAsync(ProductType productType)
    {
        return await _context.SupplierCapabilities
            .Where(c => c.ProductType == productType && c.IsActive && c.Supplier.IsActive)
            .SumAsync(c => c.AvailableCapacity);
    }

    public async Task<List<SupplierCapability>> GetCapabilitiesBySupplierAsync(Guid supplierId)
    {
        return await _context.SupplierCapabilities
            .Include(c => c.Supplier)
            .Where(c => c.SupplierId == supplierId)
            .ToListAsync();
    }

    public async Task<SupplierCapability?> GetSupplierCapabilityAsync(Guid supplierId, ProductType productType)
    {
        return await _context.SupplierCapabilities
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.SupplierId == supplierId && c.ProductType == productType);
    }

    public async Task<SupplierCapability> UpdateCapabilityAsync(SupplierCapability capability)
    {
        _context.SupplierCapabilities.Update(capability);
        await _context.SaveChangesAsync();
        return capability;
    }

    public async Task<SupplierPerformanceMetrics?> GetPerformanceMetricsAsync(Guid supplierId)
    {
        return await _context.SupplierPerformanceMetrics
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.SupplierId == supplierId);
    }

    public async Task<SupplierPerformanceMetrics> UpdatePerformanceMetricsAsync(SupplierPerformanceMetrics metrics)
    {
        var existing = await _context.SupplierPerformanceMetrics
            .FirstOrDefaultAsync(p => p.SupplierId == metrics.SupplierId);

        if (existing == null)
        {
            _context.SupplierPerformanceMetrics.Add(metrics);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(metrics);
        }

        await _context.SaveChangesAsync();
        return metrics;
    }
}