using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        // Setup configuration
        _mockConfiguration.Setup(c => c["Environment"]).Returns("Development");
        _mockConfiguration.Setup(c => c["Email:SmtpHost"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Email:SmtpPort"]).Returns("587");
        _mockConfiguration.Setup(c => c["Email:Username"]).Returns("test@example.com");
        _mockConfiguration.Setup(c => c["Email:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Email:FromEmail"]).Returns("noreply@procurementplanner.com");
        _mockConfiguration.Setup(c => c["Email:FromName"]).Returns("Procurement Planner");

        _service = new NotificationService(_context, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendEmailNotificationAsync_ValidRequest_CreatesNotificationLog()
    {
        // Arrange
        var request = new EmailNotificationRequest
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = NotificationPriority.Normal
        };

        // Act
        await _service.SendEmailNotificationAsync(request);

        // Assert
        var notificationLog = await _context.NotificationLogs
            .FirstOrDefaultAsync(n => n.Recipient == request.To);

        Assert.NotNull(notificationLog);
        Assert.Equal(NotificationType.Email, notificationLog.Type);
        Assert.Equal(request.Subject, notificationLog.Subject);
        Assert.Equal(request.Body, notificationLog.Message);
        Assert.Equal(NotificationStatus.Sent, notificationLog.Status);
    }

    [Fact]
    public async Task SendSmsNotificationAsync_ValidRequest_CreatesNotificationLog()
    {
        // Arrange
        var request = new SmsNotificationRequest
        {
            PhoneNumber = "+1234567890",
            Message = "Test SMS Message",
            Priority = NotificationPriority.High
        };

        // Act
        await _service.SendSmsNotificationAsync(request);

        // Assert
        var notificationLog = await _context.NotificationLogs
            .FirstOrDefaultAsync(n => n.Recipient == request.PhoneNumber);

        Assert.NotNull(notificationLog);
        Assert.Equal(NotificationType.SMS, notificationLog.Type);
        Assert.Equal("SMS Notification", notificationLog.Subject);
        Assert.Equal(request.Message, notificationLog.Message);
        Assert.Equal(NotificationStatus.Sent, notificationLog.Status);
    }

    [Fact]
    public async Task SendOrderStatusChangeNotificationAsync_ValidOrder_SendsNotification()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1001",
            CustomerId = "CUST001",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Status = OrderStatus.UnderReview,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        _context.CustomerOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        await _service.SendOrderStatusChangeNotificationAsync(order.Id, "Submitted", "UnderReview");

        // Assert
        var notificationLog = await _context.NotificationLogs
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Email);

        Assert.NotNull(notificationLog);
        Assert.Contains(order.OrderNumber, notificationLog.Subject);
    }

    [Fact]
    public async Task SendSupplierOrderNotificationAsync_ValidPurchaseOrder_SendsNotification()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            ContactEmail = "supplier@example.com",
            ContactPhone = "+1234567890",
            Address = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-20250823-1002",
            CustomerId = "CUST002",
            CustomerName = "Test Customer 2",
            ProductType = ProductType.FFV,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(5),
            Status = OrderStatus.PlanningInProgress,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-20250823-1001",
            CustomerOrderId = order.Id,
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Created,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(5),
            TotalValue = 1000.00m,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<PurchaseOrderItem>()
        };

        _context.Suppliers.Add(supplier);
        _context.CustomerOrders.Add(order);
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        // Act
        await _service.SendSupplierOrderNotificationAsync(purchaseOrder.Id, supplier.Id);

        // Assert
        var notificationLog = await _context.NotificationLogs
            .FirstOrDefaultAsync(n => n.Recipient == supplier.ContactEmail);

        Assert.NotNull(notificationLog);
        Assert.Contains(purchaseOrder.PurchaseOrderNumber, notificationLog.Subject);
    }

    [Fact]
    public async Task CreateNotificationTemplateAsync_ValidRequest_CreatesTemplate()
    {
        // Arrange
        var request = new CreateNotificationTemplateRequest
        {
            Name = "TestTemplate",
            Description = "Test template description",
            Type = NotificationType.Email,
            Subject = "Test Subject - {OrderNumber}",
            Body = "Hello {CustomerName}, your order {OrderNumber} status has changed.",
            RequiredParameters = new List<string> { "OrderNumber", "CustomerName" }
        };

        // Act
        await _service.CreateNotificationTemplateAsync(request);

        // Assert
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == request.Name);

        Assert.NotNull(template);
        Assert.Equal(request.Description, template.Description);
        Assert.Equal(request.Type, template.Type);
        Assert.Equal(request.Subject, template.Subject);
        Assert.Equal(request.Body, template.Body);
        Assert.True(template.IsActive);
        Assert.Equal(request.RequiredParameters, template.RequiredParameters);
    }

    [Fact]
    public async Task GetNotificationTemplateAsync_ExistingTemplate_ReturnsTemplate()
    {
        // Arrange
        var template = new ProcurementPlanner.Core.Entities.NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = "OrderStatusChange",
            Description = "Template for order status changes",
            Type = NotificationType.Email,
            Subject = "Order Status Update - {OrderNumber}",
            Body = "Dear {CustomerName}, your order {OrderNumber} status has changed from {PreviousStatus} to {NewStatus}.",
            IsActive = true,
            RequiredParameters = new List<string> { "OrderNumber", "CustomerName", "PreviousStatus", "NewStatus" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationTemplateAsync("OrderStatusChange");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template.Name, result.Name);
        Assert.Equal(template.Description, result.Description);
        Assert.Equal(template.Type, result.Type);
        Assert.Equal(template.Subject, result.Subject);
        Assert.Equal(template.Body, result.Body);
        Assert.Equal(template.RequiredParameters, result.RequiredParameters);
    }

    [Fact]
    public async Task GetNotificationTemplatesAsync_FilterByType_ReturnsFilteredTemplates()
    {
        // Arrange
        var emailTemplate = new ProcurementPlanner.Core.Entities.NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = "EmailTemplate",
            Description = "Email template",
            Type = NotificationType.Email,
            Subject = "Email Subject",
            Body = "Email Body",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var smsTemplate = new ProcurementPlanner.Core.Entities.NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = "SmsTemplate",
            Description = "SMS template",
            Type = NotificationType.SMS,
            Subject = "SMS Subject",
            Body = "SMS Body",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplates.AddRange(emailTemplate, smsTemplate);
        await _context.SaveChangesAsync();

        // Act
        var emailTemplates = await _service.GetNotificationTemplatesAsync(NotificationType.Email);

        // Assert
        Assert.Single(emailTemplates);
        Assert.Equal("EmailTemplate", emailTemplates[0].Name);
        Assert.Equal(NotificationType.Email, emailTemplates[0].Type);
    }

    [Fact]
    public async Task UpdateNotificationTemplateAsync_ValidRequest_UpdatesTemplate()
    {
        // Arrange
        var template = new ProcurementPlanner.Core.Entities.NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = "OriginalTemplate",
            Description = "Original description",
            Type = NotificationType.Email,
            Subject = "Original Subject",
            Body = "Original Body",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateNotificationTemplateRequest
        {
            Name = "UpdatedTemplate",
            Description = "Updated description",
            Subject = "Updated Subject",
            Body = "Updated Body",
            IsActive = false
        };

        // Act
        await _service.UpdateNotificationTemplateAsync(template.Id, updateRequest);

        // Assert
        var updatedTemplate = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        Assert.NotNull(updatedTemplate);
        Assert.Equal(updateRequest.Name, updatedTemplate.Name);
        Assert.Equal(updateRequest.Description, updatedTemplate.Description);
        Assert.Equal(updateRequest.Subject, updatedTemplate.Subject);
        Assert.Equal(updateRequest.Body, updatedTemplate.Body);
        Assert.Equal(updateRequest.IsActive, updatedTemplate.IsActive);
    }

    [Fact]
    public async Task SendBulkNotificationAsync_EmailType_SendsToAllRecipients()
    {
        // Arrange
        var request = new BulkNotificationRequest
        {
            Recipients = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" },
            Type = NotificationType.Email,
            Subject = "Bulk Email Subject",
            Message = "Bulk Email Message",
            Priority = NotificationPriority.Normal
        };

        // Act
        await _service.SendBulkNotificationAsync(request);

        // Assert
        var notificationLogs = await _context.NotificationLogs
            .Where(n => n.Type == NotificationType.Email)
            .ToListAsync();

        Assert.Equal(3, notificationLogs.Count);
        Assert.All(notificationLogs, log => Assert.Equal(NotificationStatus.Sent, log.Status));
        Assert.Contains(notificationLogs, log => log.Recipient == "user1@example.com");
        Assert.Contains(notificationLogs, log => log.Recipient == "user2@example.com");
        Assert.Contains(notificationLogs, log => log.Recipient == "user3@example.com");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}