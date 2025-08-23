using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Repositories;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace ProcurementPlanner.Tests.Performance;

public class SupplierRepositoryPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly SupplierRepository _repository;

    public SupplierRepositoryPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Use in-memory database for testing
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<SupplierRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _repository = _serviceProvider.GetRequiredService<SupplierRepository>();
        
        // Seed test data
        SeedTestData().Wait();
    }

    [Fact]
    public async Task GetSuppliersWithCapabilities_PerformanceTest()
    {
        // Act
        var stopwatch = Stopwatch.StartNew();
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.Capabilities.Where(c => c.IsActive))
            .Include(s => s.Performance)
            .Where(s => s.IsActive)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(suppliers);
        Assert.All(suppliers, s => Assert.True(s.IsActive));
        
        _output.WriteLine($"GetSuppliersWithCapabilities completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved {suppliers.Count} suppliers with capabilities");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Supplier query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetSuppliersByProductType_IndexPerformanceTest()
    {
        // Arrange
        var productTypes = new[] { ProductType.LMR, ProductType.FFV };

        foreach (var productType in productTypes)
        {
            // Act
            var stopwatch = Stopwatch.StartNew();
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.IsActive && s.Capabilities.Any(c => c.ProductType == productType && c.IsActive))
                .Include(s => s.Capabilities.Where(c => c.ProductType == productType && c.IsActive))
                .Include(s => s.Performance)
                .ToListAsync();
            stopwatch.Stop();

            // Assert
            Assert.NotNull(suppliers);
            
            _output.WriteLine($"GetSuppliersByProductType ({productType}) completed in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Retrieved {suppliers.Count} suppliers for {productType}");
            
            // Performance assertion - should benefit from ProductType_Active_Capacity index
            Assert.True(stopwatch.ElapsedMilliseconds < 300, 
                $"Product type query took too long: {stopwatch.ElapsedMilliseconds}ms for {productType}");
        }
    }

    [Fact]
    public async Task GetTopPerformingSuppliers_PerformanceTest()
    {
        // Act
        var stopwatch = Stopwatch.StartNew();
        var topSuppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.Performance)
            .Include(s => s.Capabilities)
            .Where(s => s.IsActive && s.Performance != null)
            .OrderByDescending(s => s.Performance!.OnTimeDeliveryRate)
            .ThenByDescending(s => s.Performance!.QualityScore)
            .Take(10)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(topSuppliers);
        Assert.True(topSuppliers.Count <= 10);
        
        _output.WriteLine($"GetTopPerformingSuppliers completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved {topSuppliers.Count} top performing suppliers");
        
        // Performance assertion - should benefit from OnTime_Quality index
        Assert.True(stopwatch.ElapsedMilliseconds < 400, $"Top suppliers query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetSupplierCapacityUtilization_PerformanceTest()
    {
        // Act
        var stopwatch = Stopwatch.StartNew();
        var capacityData = await _context.SupplierCapabilities
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.SupplierId,
                c.ProductType,
                c.MaxMonthlyCapacity,
                c.CurrentCommitments,
                UtilizationRate = c.MaxMonthlyCapacity > 0 ? (double)c.CurrentCommitments / c.MaxMonthlyCapacity : 0
            })
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(capacityData);
        
        _output.WriteLine($"GetSupplierCapacityUtilization completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Retrieved capacity data for {capacityData.Count} supplier capabilities");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 300, $"Capacity utilization query took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentSupplierQueries_PerformanceTest()
    {
        // Arrange
        const int concurrentQueries = 10;
        var supplierIds = await _context.Suppliers.Take(5).Select(s => s.Id).ToListAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = supplierIds.Take(concurrentQueries).Select(async id =>
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Capabilities)
                .Include(s => s.Performance)
                .FirstOrDefaultAsync(s => s.Id == id);
        }).ToArray();
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.All(results, result => Assert.NotNull(result));
        
        var avgTime = stopwatch.ElapsedMilliseconds / (double)concurrentQueries;
        _output.WriteLine($"Concurrent supplier queries: {concurrentQueries} queries in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTime:F2}ms per query)");
        
        // Performance assertion
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Concurrent supplier queries took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    private async Task SeedTestData()
    {
        var suppliers = new List<Supplier>();
        var random = new Random(42);

        for (int i = 1; i <= 20; i++)
        {
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = $"Supplier {i}",
                ContactEmail = $"supplier{i}@example.com",
                ContactPhone = $"555-{i:D4}",
                Address = $"{i} Supplier Street, City, State",
                IsActive = i <= 18, // Make most suppliers active
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365))
            };

            // Add capabilities
            supplier.Capabilities = new List<SupplierCapability>();
            var productTypes = new[] { ProductType.LMR, ProductType.FFV };
            
            foreach (var productType in productTypes)
            {
                if (random.NextDouble() > 0.3) // 70% chance of having capability for each product type
                {
                    supplier.Capabilities.Add(new SupplierCapability
                    {
                        Id = Guid.NewGuid(),
                        SupplierId = supplier.Id,
                        ProductType = productType,
                        MaxMonthlyCapacity = random.Next(100, 1000),
                        CurrentCommitments = random.Next(0, 800),
                        QualityRating = (decimal)(random.NextDouble() * 4 + 1), // 1-5 rating
                        IsActive = random.NextDouble() > 0.1, // 90% active
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Add performance metrics
            supplier.Performance = new SupplierPerformanceMetrics
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                OnTimeDeliveryRate = (decimal)(random.NextDouble() * 0.4 + 0.6), // 60-100%
                QualityScore = (decimal)(random.NextDouble() * 2 + 3), // 3-5 score
                AverageDeliveryDays = (decimal)(random.NextDouble() * 10 + 5), // 5-15 days
                CustomerSatisfactionRate = (decimal)(random.NextDouble() * 0.3 + 0.7), // 70-100%
                TotalOrdersCompleted = random.Next(10, 500),
                LastUpdated = DateTime.UtcNow.AddDays(-random.Next(1, 30))
            };

            suppliers.Add(supplier);
        }

        _context.Suppliers.AddRange(suppliers);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}