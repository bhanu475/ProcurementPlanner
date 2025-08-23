using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface INotificationService
{
    Task SendEmailNotificationAsync(EmailNotificationRequest request);
    Task SendSmsNotificationAsync(SmsNotificationRequest request);
    Task SendOrderStatusChangeNotificationAsync(Guid orderId, string previousStatus, string newStatus);
    Task SendSupplierOrderNotificationAsync(Guid purchaseOrderId, Guid supplierId);
    Task SendCustomerOrderUpdateNotificationAsync(Guid orderId, string updateMessage);
    Task SendBulkNotificationAsync(BulkNotificationRequest request);
    Task<List<ProcurementPlanner.Core.Models.NotificationTemplate>> GetNotificationTemplatesAsync(NotificationType type);
    Task<ProcurementPlanner.Core.Models.NotificationTemplate?> GetNotificationTemplateAsync(string templateName);
    Task CreateNotificationTemplateAsync(CreateNotificationTemplateRequest request);
    Task UpdateNotificationTemplateAsync(Guid templateId, UpdateNotificationTemplateRequest request);
}