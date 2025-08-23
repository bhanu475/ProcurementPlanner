using Microsoft.EntityFrameworkCore;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace ProcurementPlanner.Tests.Integration;

/// <summary>
/// Manages test data setup and cleanup for integration tests
/// </summary>
public class TestDataManager
{
    private readonly ApplicationDbContext _context;
    private readonly List<Guid> _createdEntityIds;

    public TestDataManager(ApplicationDbContext context)
    {
        _context = context;
        _createdEntityIds = new List<Guid>();
    }

    /// <summary>
    /// Seeds comprehensive test data for integration testing
    /// </summary>
    public async Task SeedComprehensiveTestDataAsync()
    {
        await SeedUsersAsync();
        await SeedSuppliersAsync();
        await SeedCustomerOrdersAsync();
        await SeedNotificationTemplatesAsync();
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds minimal test data for basic testing scenarios
    /// </summary>
    public async Task SeedMinimalTestDataAsync()
    {
        await SeedBasicUsersAsync();
        await SeedBasicSuppliersAsync();
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds performance test data with large datasets
    /// </summary>
    public async Task SeedPerformanceTestDataAsync(int orderCount = 1000, int supplierCount = 50)
    {
        await SeedBasicUsersAsync();
        await SeedLargeSuppliersDatasetAsync(supplierCount);
        await SeedLargeOrdersDatasetAsync(orderCount);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Cleans up all test data created by this manager
    /// </summary>
    public async Task CleanupTestDataAsync()
    {
        // Clean up in reverse dependency order
        await CleanupAuditLogsAsync();
        await CleanupPurchaseOrdersAsync();
        await CleanupCustomerOrdersAsync();
        await CleanupSuppliersAsync();
        await CleanupUsersAsync();
        await CleanupNotificationTemplatesAsync();
        
        await _context.SaveChangesAsync();
        _createdEntityIds.Clear();
    }

    /// <summary>
    /// Creates a test user with specified role
    /// </summary>
    public async Task<User> CreateTestUserAsync(string email, UserRole role, string username = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username ?? email,
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        _createdEntityIds.Add(user.Id);
        return user;
    }

    /// <summary>
    /// Creates a test supplier with specified capabilities
    /// </summary>
    public async Task<Supplier> CreateTestSupplierAsync(string name, List<(ProductType type, int capacity, decimal rating)> capabilities)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactEmail = $"{name.ToLower().Replace(" ", "")}@test.com",
            ContactPhone = GeneratePhoneNumber(),
            Address = $"{Random.Shared.Next(100, 999)} Test St, Test City, TC {Random.Shared.Next(10000, 99999)}",
            IsActive = true,
            Capabilities = capabilities.Select(c => new SupplierCapability
            {
                Id = Guid.NewGuid(),
                ProductType = c.type,
                MaxMonthlyCapacity = c.capacity,
                CurrentCommitments = Random.Shared.Next(0, c.capacity / 2),
                QualityRating = c.rating
            }).ToList(),
            Performance = new SupplierPerformanceMetrics
            {
                OnTimeDeliveryRate = (decimal)(Random.Shared.NextDouble() * 0.3 + 0.7), // 70-100%
                QualityScore = capabilities.Average(c => (double)c.rating),
                TotalOrdersCompleted = Random.Shared.Next(10, 100),
                LastUpdated = DateTime.UtcNow
            }
        };

        await _context.Suppliers.AddAsync(supplier);
        _createdEntityIds.Add(supplier.Id);
        return supplier;
    }

    /// <summary>
    /// Creates a test customer order with specified parameters
    /// </summary>
    public async Task<CustomerOrder> CreateTestCustomerOrderAsync(
        string customerId, 
        ProductType productType, 
        DateTime deliveryDate,
        List<(string productCode, string description, int quantity, decimal price)> items)
    {
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            CustomerName = $"Test Customer {customerId}",
            ProductType = productType,
            RequestedDeliveryDate = deliveryDate,
            Status = OrderStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Items = items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductCode = item.productCode,
                Description = item.description,
                Quantity = item.quantity,
                Unit = "EA",
                UnitPrice = item.price,
                Specifications = "Standard specifications"
            }).ToList()
        };

        await _context.CustomerOrders.AddAsync(order);
        _createdEntityIds.Add(order.Id);
        return order;
    }

    /// <summary>
    /// Creates a test purchase order linked to a customer order and supplier
    /// </summary>
    public async Task<PurchaseOrder> CreateTestPurchaseOrderAsync(
        Guid customerOrderId, 
        Guid supplierId, 
        List<(Guid orderItemId, int quantity)> itemAllocations)
    {
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = GeneratePurchaseOrderNumber(),
            CustomerOrderId = customerOrderId,
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Created,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(), // Test user ID
            Items = itemAllocations.Select(allocation => new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                OrderItemId = allocation.orderItemId,
                AllocatedQuantity = allocation.quantity,
                PackagingDetails = "Standard packaging",
                DeliveryMethod = "Standard delivery"
            }).ToList()
        };

        await _context.PurchaseOrders.AddAsync(purchaseOrder);
        _createdEntityIds.Add(purchaseOrder.Id);
        return purchaseOrder;
    }

    /// <summary>
    /// Creates realistic test scenario with complete workflow data
    /// </summary>
    public async Task<TestScenario> CreateCompleteTestScenarioAsync()
    {
        // Create users
        var planner = await CreateTestUserAsync("planner@test.com", UserRole.LMRPlanner);
        var supplier1 = await CreateTestUserAsync("supplier1@test.com", UserRole.Supplier);
        var supplier2 = await CreateTestUserAsync("supplier2@test.com", UserRole.Supplier);

        // Create suppliers
        var testSupplier1 = await CreateTestSupplierAsync("Test Supplier 1", new List<(ProductType, int, decimal)>
        {
            (ProductType.LMR, 500, 4.5m),
            (ProductType.FFV, 300, 4.2m)
        });

        var testSupplier2 = await CreateTestSupplierAsync("Test Supplier 2", new List<(ProductType, int, decimal)>
        {
            (ProductType.LMR, 300, 4.0m),
            (ProductType.FFV, 400, 4.7m)
        });

        // Create customer orders
        var order1 = await CreateTestCustomerOrderAsync(
            "DODAAC001", 
            ProductType.LMR, 
            DateTime.UtcNow.AddDays(10),
            new List<(string, string, int, decimal)>
            {
                ("PROD001", "Fresh Vegetables", 100, 2.50m),
                ("PROD002", "Fresh Fruits", 50, 3.00m)
            });

        var order2 = await CreateTestCustomerOrderAsync(
            "DODAAC002", 
            ProductType.FFV, 
            DateTime.UtcNow.AddDays(14),
            new List<(string, string, int, decimal)>
            {
                ("PROD003", "Organic Produce", 75, 4.00m)
            });

        await _context.SaveChangesAsync();

        return new TestScenario
        {
            Planner = planner,
            Suppliers = new List<User> { supplier1, supplier2 },
            TestSuppliers = new List<Supplier> { testSupplier1, testSupplier2 },
            CustomerOrders = new List<CustomerOrder> { order1, order2 }
        };
    }

    // Private helper methods
    private async Task SeedUsersAsync()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "admin@test.com",
                Email = "admin@test.com",
                Role = UserRole.Administrator,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "planner@test.com",
                Email = "planner@test.com",
                Role = UserRole.LMRPlanner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "supplier@test.com",
                Email = "supplier@test.com",
                Role = UserRole.Supplier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "customer@test.com",
                Email = "customer@test.com",
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        _createdEntityIds.AddRange(users.Select(u => u.Id));
    }

    private async Task SeedBasicUsersAsync()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "planner@test.com",
                Email = "planner@test.com",
                Role = UserRole.LMRPlanner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "supplier@test.com",
                Email = "supplier@test.com",
                Role = UserRole.Supplier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        _createdEntityIds.AddRange(users.Select(u => u.Id));
    }

    private async Task SeedSuppliersAsync()
    {
        var suppliers = new List<Supplier>();
        
        for (int i = 1; i <= 10; i++)
        {
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = $"Test Supplier {i}",
                ContactEmail = $"supplier{i}@test.com",
                ContactPhone = GeneratePhoneNumber(),
                Address = $"{Random.Shared.Next(100, 999)} Test St {i}, Test City, TC {Random.Shared.Next(10000, 99999)}",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = Random.Shared.Next(200, 800),
                        CurrentCommitments = Random.Shared.Next(50, 200),
                        QualityRating = (decimal)(Random.Shared.NextDouble() * 2 + 3) // 3.0-5.0
                    }
                },
                Performance = new SupplierPerformanceMetrics
                {
                    OnTimeDeliveryRate = (decimal)(Random.Shared.NextDouble() * 0.3 + 0.7), // 70-100%
                    QualityScore = Random.Shared.NextDouble() * 2 + 3, // 3.0-5.0
                    TotalOrdersCompleted = Random.Shared.Next(10, 100),
                    LastUpdated = DateTime.UtcNow
                }
            };

            // Add FFV capability to some suppliers
            if (i % 2 == 0)
            {
                supplier.Capabilities.Add(new SupplierCapability
                {
                    Id = Guid.NewGuid(),
                    ProductType = ProductType.FFV,
                    MaxMonthlyCapacity = Random.Shared.Next(150, 600),
                    CurrentCommitments = Random.Shared.Next(25, 150),
                    QualityRating = (decimal)(Random.Shared.NextDouble() * 2 + 3)
                });
            }

            suppliers.Add(supplier);
        }

        await _context.Suppliers.AddRangeAsync(suppliers);
        _createdEntityIds.AddRange(suppliers.Select(s => s.Id));
    }

    private async Task SeedBasicSuppliersAsync()
    {
        var suppliers = new List<Supplier>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Basic Test Supplier 1",
                ContactEmail = "basic1@test.com",
                ContactPhone = "555-0001",
                Address = "123 Basic St, Test City, TC 12345",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.LMR,
                        MaxMonthlyCapacity = 500,
                        CurrentCommitments = 100,
                        QualityRating = 4.5m
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Basic Test Supplier 2",
                ContactEmail = "basic2@test.com",
                ContactPhone = "555-0002",
                Address = "456 Basic Ave, Test City, TC 12345",
                IsActive = true,
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = ProductType.FFV,
                        MaxMonthlyCapacity = 300,
                        CurrentCommitments = 50,
                        QualityRating = 4.2m
                    }
                }
            }
        };

        await _context.Suppliers.AddRangeAsync(suppliers);
        _createdEntityIds.AddRange(suppliers.Select(s => s.Id));
    }

    private async Task SeedLargeSuppliersDatasetAsync(int count)
    {
        var suppliers = new List<Supplier>();
        
        for (int i = 1; i <= count; i++)
        {
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = $"Performance Test Supplier {i}",
                ContactEmail = $"perf{i}@test.com",
                ContactPhone = GeneratePhoneNumber(),
                Address = $"{Random.Shared.Next(100, 999)} Perf St {i}, Test City, TC {Random.Shared.Next(10000, 99999)}",
                IsActive = Random.Shared.NextDouble() > 0.1, // 90% active
                Capabilities = new List<SupplierCapability>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductType = Random.Shared.NextDouble() > 0.5 ? ProductType.LMR : ProductType.FFV,
                        MaxMonthlyCapacity = Random.Shared.Next(100, 1000),
                        CurrentCommitments = Random.Shared.Next(10, 300),
                        QualityRating = (decimal)(Random.Shared.NextDouble() * 2 + 3)
                    }
                }
            };

            suppliers.Add(supplier);
        }

        await _context.Suppliers.AddRangeAsync(suppliers);
        _createdEntityIds.AddRange(suppliers.Select(s => s.Id));
    }

    private async Task SeedCustomerOrdersAsync()
    {
        var orders = new List<CustomerOrder>();
        
        for (int i = 1; i <= 20; i++)
        {
            var order = new CustomerOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                CustomerId = $"DODAAC{i:D3}",
                CustomerName = $"Test Customer {i}",
                ProductType = Random.Shared.NextDouble() > 0.5 ? ProductType.LMR : ProductType.FFV,
                RequestedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)),
                Status = (OrderStatus)Random.Shared.Next(0, Enum.GetValues<OrderStatus>().Length),
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 10)),
                CreatedBy = "test-user",
                Items = new List<OrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductCode = $"PROD{Random.Shared.Next(1, 100):D3}",
                        Description = $"Test Product {i}",
                        Quantity = Random.Shared.Next(10, 200),
                        Unit = "EA",
                        UnitPrice = (decimal)(Random.Shared.NextDouble() * 20 + 1),
                        Specifications = "Standard test specifications"
                    }
                }
            };

            orders.Add(order);
        }

        await _context.CustomerOrders.AddRangeAsync(orders);
        _createdEntityIds.AddRange(orders.Select(o => o.Id));
    }

    private async Task SeedLargeOrdersDatasetAsync(int count)
    {
        var orders = new List<CustomerOrder>();
        
        for (int i = 1; i <= count; i++)
        {
            var itemCount = Random.Shared.Next(1, 5);
            var items = new List<OrderItem>();
            
            for (int j = 1; j <= itemCount; j++)
            {
                items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductCode = $"PROD{Random.Shared.Next(1, 1000):D4}",
                    Description = $"Performance Test Product {i}-{j}",
                    Quantity = Random.Shared.Next(1, 500),
                    Unit = "EA",
                    UnitPrice = (decimal)(Random.Shared.NextDouble() * 50 + 1),
                    Specifications = "Performance test specifications"
                });
            }

            var order = new CustomerOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                CustomerId = $"PERF{i:D4}",
                CustomerName = $"Performance Test Customer {i}",
                ProductType = Random.Shared.NextDouble() > 0.5 ? ProductType.LMR : ProductType.FFV,
                RequestedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 60)),
                Status = (OrderStatus)Random.Shared.Next(0, Enum.GetValues<OrderStatus>().Length),
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)),
                CreatedBy = "perf-test-user",
                Items = items
            };

            orders.Add(order);
        }

        await _context.CustomerOrders.AddRangeAsync(orders);
        _createdEntityIds.AddRange(orders.Select(o => o.Id));
    }

    private async Task SeedNotificationTemplatesAsync()
    {
        var templates = new List<NotificationTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Order Created",
                Subject = "New Order Created: {OrderNumber}",
                Body = "A new order has been created with order number {OrderNumber}.",
                NotificationType = NotificationType.Email,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Purchase Order Confirmed",
                Subject = "Purchase Order Confirmed: {PurchaseOrderNumber}",
                Body = "Purchase order {PurchaseOrderNumber} has been confirmed by the supplier.",
                NotificationType = NotificationType.Email,
                IsActive = true
            }
        };

        await _context.NotificationTemplates.AddRangeAsync(templates);
        _createdEntityIds.AddRange(templates.Select(t => t.Id));
    }

    // Cleanup methods
    private async Task CleanupAuditLogsAsync()
    {
        var auditLogs = await _context.AuditLogs
            .Where(al => _createdEntityIds.Contains(Guid.Parse(al.EntityId)))
            .ToListAsync();
        
        _context.AuditLogs.RemoveRange(auditLogs);
    }

    private async Task CleanupPurchaseOrdersAsync()
    {
        var purchaseOrders = await _context.PurchaseOrders
            .Where(po => _createdEntityIds.Contains(po.Id) || _createdEntityIds.Contains(po.CustomerOrderId))
            .ToListAsync();
        
        _context.PurchaseOrders.RemoveRange(purchaseOrders);
    }

    private async Task CleanupCustomerOrdersAsync()
    {
        var orders = await _context.CustomerOrders
            .Where(o => _createdEntityIds.Contains(o.Id))
            .ToListAsync();
        
        _context.CustomerOrders.RemoveRange(orders);
    }

    private async Task CleanupSuppliersAsync()
    {
        var suppliers = await _context.Suppliers
            .Where(s => _createdEntityIds.Contains(s.Id))
            .ToListAsync();
        
        _context.Suppliers.RemoveRange(suppliers);
    }

    private async Task CleanupUsersAsync()
    {
        var users = await _context.Users
            .Where(u => _createdEntityIds.Contains(u.Id))
            .ToListAsync();
        
        _context.Users.RemoveRange(users);
    }

    private async Task CleanupNotificationTemplatesAsync()
    {
        var templates = await _context.NotificationTemplates
            .Where(nt => _createdEntityIds.Contains(nt.Id))
            .ToListAsync();
        
        _context.NotificationTemplates.RemoveRange(templates);
    }

    // Utility methods
    private static string GenerateOrderNumber()
    {
        return $"ORD{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    }

    private static string GeneratePurchaseOrderNumber()
    {
        return $"PO{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    }

    private static string GeneratePhoneNumber()
    {
        return $"555-{Random.Shared.Next(1000, 9999)}";
    }
}

/// <summary>
/// Represents a complete test scenario with all related entities
/// </summary>
public class TestScenario
{
    public User Planner { get; set; }
    public List<User> Suppliers { get; set; } = new();
    public List<Supplier> TestSuppliers { get; set; } = new();
    public List<CustomerOrder> CustomerOrders { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}