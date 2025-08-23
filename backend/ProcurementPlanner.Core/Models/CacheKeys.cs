namespace ProcurementPlanner.Core.Models;

public class CachedValue<T>
{
    public T Value { get; set; } = default!;
    
    public CachedValue() { }
    
    public CachedValue(T value)
    {
        Value = value;
    }
}

public static class CacheKeys
{
    // Dashboard cache keys
    public const string DashboardSummary = "dashboard:summary";
    public const string DashboardOrdersByStatus = "dashboard:orders:status";
    public const string DashboardOrdersByProductType = "dashboard:orders:product-type";
    public const string DashboardOrdersByDeliveryDate = "dashboard:orders:delivery-date:{0}"; // {0} = date range

    // Supplier cache keys
    public const string SupplierPerformance = "supplier:performance:{0}"; // {0} = supplier id
    public const string SupplierCapabilities = "supplier:capabilities:{0}"; // {0} = supplier id
    public const string SupplierList = "supplier:list";
    public const string SupplierAvailable = "supplier:available:{0}:{1}"; // {0} = product type, {1} = capacity

    // Order cache keys
    public const string OrderDetails = "order:details:{0}"; // {0} = order id
    public const string OrdersByCustomer = "orders:customer:{0}"; // {0} = customer id
    public const string OrdersByStatus = "orders:status:{0}"; // {0} = status

    // Purchase Order cache keys
    public const string PurchaseOrdersBySupplier = "po:supplier:{0}"; // {0} = supplier id
    public const string PurchaseOrderDetails = "po:details:{0}"; // {0} = purchase order id

    // User session cache keys
    public const string UserSession = "session:user:{0}"; // {0} = user id
    public const string UserPermissions = "user:permissions:{0}"; // {0} = user id

    // Report cache keys
    public const string SupplierDistributionReport = "report:supplier-distribution:{0}"; // {0} = date range hash
    public const string PerformanceReport = "report:performance:{0}"; // {0} = date range hash

    // Cache expiration times
    public static class Expiration
    {
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan Long = TimeSpan.FromHours(2);
        public static readonly TimeSpan VeryLong = TimeSpan.FromHours(24);
        public static readonly TimeSpan Session = TimeSpan.FromHours(8);
    }

    // Cache patterns for bulk operations
    public static class Patterns
    {
        public const string AllDashboard = "dashboard:*";
        public const string AllSupplier = "supplier:*";
        public const string AllOrders = "order*";
        public const string AllPurchaseOrders = "po:*";
        public const string AllReports = "report:*";
        public const string AllSessions = "session:*";
    }
}