using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace ProcurementPlanner.Tests.Performance;

/// <summary>
/// Tests to validate that database indexes and query optimizations are working effectively
/// </summary>
public class QueryOptimizationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public QueryOptimizationTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Use in-memory database for testing
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Seed test data
        SeedTestData().Wait();
    }

    [Fact]
    public async Task CustomerOrder_StatusDeliveryDate_IndexTest()
    {
        // Test the IX_CustomerOrders_Status_DeliveryDate index
        var targetDate = DateTime.UtcNow.Date.AddDays(7);
        
        var stopwatch = Stopwatch.StartNew();
        var orders = await _context.CustomerOrders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Submitted && o.RequestedDeliveryDate <= targetDate)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Status + DeliveryDate query: {orders.Count} orders in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Status + DeliveryDate index query should be fast");
    }

    [Fact]
    public async Task CustomerOrder_ProductTypeStatus_IndexTest()
    {
        // Test the IX_CustomerOrders_ProductType_Status index
        var stopwatch = Stopwatch.StartNew();
        var orders = await _context.CustomerOrders
            .AsNoTracking()
            .Where(o => o.ProductType == ProductType.LMR && o.Status == OrderStatus.UnderReview)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"ProductType + Status query: {orders.Count} orders in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "ProductType + Status index query should be fast");
    }

    [Fact]
    public async Task CustomerOrder_OverdueQuery_IndexTest()
    {
        // Test the IX_CustomerOrders_OverdueQuery index (RequestedDeliveryDate, Status, ProductType)
        var today = DateTime.UtcNow.Date;
        
        var stopwatch = Stopwatch.StartNew();
        var overdueOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Where(o => o.RequestedDeliveryDate < today && 
                       o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled &&
                       o.ProductType == ProductType.LMR)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Overdue query: {overdueOrders.Count} orders in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Overdue query should benefit from composite index");
    }

    [Fact]
    public async Task CustomerOrder_DashboardQuery_IndexTest()
    {
        // Test the IX_CustomerOrders_Dashboard index (ProductType, RequestedDeliveryDate, Status)
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);
        
        var stopwatch = Stopwatch.StartNew();
        var dashboardData = await _context.CustomerOrders
            .AsNoTracking()
            .Where(o => o.ProductType == ProductType.FFV && 
                       o.RequestedDeliveryDate >= startDate && 
                       o.RequestedDeliveryDate <= endDate &&
                       o.Status == OrderStatus.Submitted)
            .GroupBy(o => o.RequestedDeliveryDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Dashboard query: {dashboardData.Count} groups in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 300, "Dashboard query should benefit from composite index");
    }

    [Fact]
    public async Task PurchaseOrder_SupplierStatus_IndexTest()
    {
        // Test the IX_PurchaseOrders_SupplierId_Status index
        var supplierId = await _context.Suppliers.Select(s => s.Id).FirstAsync();
        
        var stopwatch = Stopwatch.StartNew();
        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(po => po.SupplierId == supplierId && po.Status == PurchaseOrderStatus.Created)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Supplier + Status query: {purchaseOrders.Count} purchase orders in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Supplier + Status index query should be fast");
    }

    [Fact]
    public async Task SupplierCapability_ProductTypeActiveCapacity_IndexTest()
    {
        // Test the IX_SupplierCapabilities_ProductType_Active_Capacity index
        var stopwatch = Stopwatch.StartNew();
        var capabilities = await _context.SupplierCapabilities
            .AsNoTracking()
            .Where(c => c.ProductType == ProductType.LMR && 
                       c.IsActive && 
                       c.MaxMonthlyCapacity >= 500)
            .OrderByDescending(c => c.MaxMonthlyCapacity)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Capability query: {capabilities.Count} capabilities in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Capability index query should be fast");
    }

    [Fact]
    public async Task SupplierPerformance_OnTimeQuality_IndexTest()
    {
        // Test the IX_SupplierPerformance_OnTime_Quality index
        var stopwatch = Stopwatch.StartNew();
        var topPerformers = await _context.SupplierPerformanceMetrics
            .AsNoTracking()
            .Where(p => p.OnTimeDeliveryRate >= 0.8m && p.QualityScore >= 4.0m)
            .OrderByDescending(p => p.OnTimeDeliveryRate)
            .ThenByDescending(p => p.QualityScore)
            .Take(10)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Performance query: {topPerformers.Count} performers in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Performance index query should be fast");
    }

    [Fact]
    public async Task AuditLog_EntityTimestamp_IndexTest()
    {
        // Test the IX_AuditLogs_Entity_Timestamp index
        var entityId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-7);
        
        var stopwatch = Stopwatch.StartNew();
        var auditLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == "CustomerOrder" && 
                       a.EntityId == entityId && 
                       a.Timestamp >= startDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Audit query: {auditLogs.Count} logs in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Audit entity + timestamp index query should be fast");
    }

    [Fact]
    public async Task NotificationLog_StatusPriorityCreated_IndexTest()
    {
        // Test the IX_NotificationLogs_Status_Priority_Created index
        var startDate = DateTime.UtcNow.AddHours(-1);
        
        var stopwatch = Stopwatch.StartNew();
        var notifications = await _context.NotificationLogs
            .AsNoTracking()
            .Where(n => n.Status == NotificationStatus.Pending && 
                       n.Priority == NotificationPriority.High && 
                       n.CreatedAt >= startDate)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Notification query: {notifications.Count} notifications in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 200, "Notification index query should be fast");
    }

    [Fact]
    public async Task ComplexJoinQuery_PerformanceTest()
    {
        // Test a complex query that should benefit from multiple indexes
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);
        
        var stopwatch = Stopwatch.StartNew();
        var complexQuery = await (from order in _context.CustomerOrders
                                 join po in _context.PurchaseOrders on order.Id equals po.CustomerOrderId
                                 join supplier in _context.Suppliers on po.SupplierId equals supplier.Id
                                 where order.RequestedDeliveryDate >= startDate &&
                                       order.RequestedDeliveryDate <= endDate &&
                                       order.Status == OrderStatus.PlanningInProgress &&
                                       po.Status == PurchaseOrderStatus.Created &&
                                       supplier.IsActive
                                 select new
                                 {
                                     OrderNumber = order.OrderNumber,
                                     SupplierName = supplier.Name,
                                     DeliveryDate = order.RequestedDeliveryDate,
                                     PONumber = po.PurchaseOrderNumber
                                 }).AsNoTracking().ToListAsync();
        stopwatch.Stop();

        _output.WriteLine($"Complex join query: {complexQuery.Count} results in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Complex join query should benefit from multiple indexes");
    }

    private async Task SeedTestData()
    {
        var random = new Random(42);
        
        // Create suppliers
        var suppliers = Enumerable.Range(1, 10).Select(i => new Supplier
        {
            Id = Guid.NewGuid(),
            Name = $"Supplier {i}",
            ContactEmail = $"supplier{i}@example.com",
            ContactPhone = $"555-{i:D4}",
            Address = $"{i} Supplier Street",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 100))
        }).ToList();

        _context.Suppliers.AddRange(suppliers);

        // Create supplier capabilities
        var capabilities = new List<SupplierCapability>();
        foreach (var supplier in suppliers)
        {
            capabilities.Add(new SupplierCapability
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                ProductType = ProductType.LMR,
                MaxMonthlyCapacity = random.Next(100, 1000),
                CurrentCommitments = random.Next(0, 500),
                QualityRating = (decimal)(random.NextDouble() * 4 + 1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            capabilities.Add(new SupplierCapability
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                ProductType = ProductType.FFV,
                MaxMonthlyCapacity = random.Next(100, 1000),
                CurrentCommitments = random.Next(0, 500),
                QualityRating = (decimal)(random.NextDouble() * 4 + 1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.SupplierCapabilities.AddRange(capabilities);

        // Create supplier performance metrics
        var performanceMetrics = suppliers.Select(s => new SupplierPerformanceMetrics
        {
            Id = Guid.NewGuid(),
            SupplierId = s.Id,
            OnTimeDeliveryRate = (decimal)(random.NextDouble() * 0.4 + 0.6),
            QualityScore = (decimal)(random.NextDouble() * 2 + 3),
            AverageDeliveryDays = (decimal)(random.NextDouble() * 10 + 5),
            CustomerSatisfactionRate = (decimal)(random.NextDouble() * 0.3 + 0.7),
            TotalOrdersCompleted = random.Next(10, 500),
            LastUpdated = DateTime.UtcNow.AddDays(-random.Next(1, 30))
        }).ToList();

        _context.SupplierPerformanceMetrics.AddRange(performanceMetrics);

        // Create customer orders
        var orders = Enumerable.Range(1, 100).Select(i => new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{i:D6}",
            CustomerId = $"CUST{i % 5 + 1:D3}",
            CustomerName = $"Customer {i % 5 + 1}",
            ProductType = i % 2 == 0 ? ProductType.LMR : ProductType.FFV,
            Status = (OrderStatus)(i % 8 + 1),
            RequestedDeliveryDate = DateTime.UtcNow.Date.AddDays(random.Next(-10, 30)),
            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60)),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = $"PROD-{i:D3}",
                    Description = $"Product {i}",
                    Quantity = random.Next(1, 50),
                    Unit = "EA",
                    UnitPrice = random.Next(10, 500),
                    CreatedAt = DateTime.UtcNow
                }
            }
        }).ToList();

        _context.CustomerOrders.AddRange(orders);

        // Create purchase orders
        var purchaseOrders = orders.Take(50).SelectMany((order, index) =>
        {
            var supplier = suppliers[index % suppliers.Count];
            return new[]
            {
                new PurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderNumber = $"PO-{index + 1:D6}",
                    CustomerOrderId = order.Id,
                    SupplierId = supplier.Id,
                    Status = (PurchaseOrderStatus)(index % 6 + 1),
                    RequiredDeliveryDate = order.RequestedDeliveryDate,
                    TotalValue = random.Next(100, 5000),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                    CreatedBy = Guid.NewGuid()
                }
            };
        }).ToList();

        _context.PurchaseOrders.AddRange(purchaseOrders);

        // Create audit logs
        var auditLogs = Enumerable.Range(1, 200).Select(i => new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = i % 2 == 0 ? "Create" : "Update",
            EntityType = i % 3 == 0 ? "CustomerOrder" : "PurchaseOrder",
            EntityId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Username = $"user{i % 10}",
            UserRole = "LMRPlanner",
            Result = AuditResult.Success,
            Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(1, 10080)) // Last week
        }).ToList();

        _context.AuditLogs.AddRange(auditLogs);

        // Create notification logs
        var notificationLogs = Enumerable.Range(1, 100).Select(i => new Core.Entities.NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = i % 2 == 0 ? NotificationType.Email : NotificationType.SMS,
            Status = (NotificationStatus)(i % 3 + 1),
            Priority = (NotificationPriority)(i % 3 + 1),
            Recipient = $"user{i % 10}@example.com",
            Subject = $"Test notification {i}",
            Message = $"Test notification body {i}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 1440)), // Last day
            SentAt = i % 3 == 0 ? DateTime.UtcNow.AddMinutes(-random.Next(1, 1440)) : null
        }).ToList();

        _context.NotificationLogs.AddRange(notificationLogs);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}