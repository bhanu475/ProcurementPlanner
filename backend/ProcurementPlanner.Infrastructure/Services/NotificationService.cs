using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using System.Net;
using System.Net.Mail;

namespace ProcurementPlanner.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailNotificationAsync(EmailNotificationRequest request)
    {
        _logger.LogInformation("Sending email notification to {Recipient}", request.To);

        var notificationLog = new ProcurementPlanner.Core.Entities.NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = NotificationType.Email,
            Recipient = request.To,
            Subject = request.Subject,
            Message = request.Body,
            Priority = request.Priority,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationLogs.Add(notificationLog);

        try
        {
            // Use template if specified
            string subject = request.Subject;
            string body = request.Body;

            if (!string.IsNullOrEmpty(request.TemplateName) && request.TemplateData != null)
            {
                var templateEntity = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == request.TemplateName && t.IsActive);
                if (templateEntity != null)
                {
                    subject = templateEntity.RenderSubject(request.TemplateData);
                    body = templateEntity.RenderBody(request.TemplateData);
                }
            }

            // Send email using SMTP
            await SendEmailViaSMTPAsync(request.To, request.Cc, request.Bcc, subject, body, request.IsHtml, request.Attachments);

            notificationLog.MarkAsSent();
            _logger.LogInformation("Email notification sent successfully to {Recipient}", request.To);
        }
        catch (Exception ex)
        {
            notificationLog.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Failed to send email notification to {Recipient}", request.To);
            throw;
        }
        finally
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task SendSmsNotificationAsync(SmsNotificationRequest request)
    {
        _logger.LogInformation("Sending SMS notification to {PhoneNumber}", request.PhoneNumber);

        var notificationLog = new ProcurementPlanner.Core.Entities.NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = NotificationType.SMS,
            Recipient = request.PhoneNumber,
            Subject = "SMS Notification",
            Message = request.Message,
            Priority = request.Priority,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationLogs.Add(notificationLog);

        try
        {
            // Use template if specified
            string message = request.Message;

            if (!string.IsNullOrEmpty(request.TemplateName) && request.TemplateData != null)
            {
                var templateEntity = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == request.TemplateName && t.IsActive);
                if (templateEntity != null)
                {
                    message = templateEntity.RenderBody(request.TemplateData);
                }
            }

            // Send SMS (mock implementation - in production, integrate with SMS provider like Twilio)
            await SendSmsViaMockProviderAsync(request.PhoneNumber, message);

            notificationLog.MarkAsSent();
            _logger.LogInformation("SMS notification sent successfully to {PhoneNumber}", request.PhoneNumber);
        }
        catch (Exception ex)
        {
            notificationLog.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Failed to send SMS notification to {PhoneNumber}", request.PhoneNumber);
            throw;
        }
        finally
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task SendOrderStatusChangeNotificationAsync(Guid orderId, string previousStatus, string newStatus)
    {
        _logger.LogInformation("Sending order status change notification for order {OrderId}", orderId);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status change notification", orderId);
            return;
        }

        // Get customer contact information (assuming we have it in the system)
        var templateData = new Dictionary<string, string>
        {
            { "OrderNumber", order.OrderNumber },
            { "CustomerName", order.CustomerName },
            { "PreviousStatus", previousStatus },
            { "NewStatus", newStatus },
            { "DeliveryDate", order.RequestedDeliveryDate.ToString("yyyy-MM-dd") }
        };

        // Send email notification to customer
        var emailRequest = new EmailNotificationRequest
        {
            To = $"{order.CustomerId}@example.com", // In production, get actual email from customer data
            Subject = $"Order Status Update - {order.OrderNumber}",
            TemplateName = "OrderStatusChange",
            TemplateData = templateData,
            Priority = NotificationPriority.Normal
        };

        try
        {
            await SendEmailNotificationAsync(emailRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order status change notification for order {OrderId}", orderId);
        }
    }

    public async Task SendSupplierOrderNotificationAsync(Guid purchaseOrderId, Guid supplierId)
    {
        _logger.LogInformation("Sending supplier order notification for purchase order {PurchaseOrderId}", purchaseOrderId);

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.CustomerOrder)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        if (purchaseOrder == null)
        {
            _logger.LogWarning("Purchase order {PurchaseOrderId} not found for supplier notification", purchaseOrderId);
            return;
        }

        var templateData = new Dictionary<string, string>
        {
            { "PurchaseOrderNumber", purchaseOrder.PurchaseOrderNumber },
            { "SupplierName", purchaseOrder.Supplier.Name },
            { "CustomerOrderNumber", purchaseOrder.CustomerOrder.OrderNumber },
            { "DeliveryDate", purchaseOrder.RequiredDeliveryDate.ToString("yyyy-MM-dd") },
            { "TotalValue", purchaseOrder.TotalValue.ToString() }
        };

        // Send email notification to supplier
        var emailRequest = new EmailNotificationRequest
        {
            To = purchaseOrder.Supplier.ContactEmail,
            Subject = $"New Purchase Order - {purchaseOrder.PurchaseOrderNumber}",
            TemplateName = "SupplierNewOrder",
            TemplateData = templateData,
            Priority = NotificationPriority.High
        };

        try
        {
            await SendEmailNotificationAsync(emailRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send supplier order notification for purchase order {PurchaseOrderId}", purchaseOrderId);
        }
    }

    public async Task SendCustomerOrderUpdateNotificationAsync(Guid orderId, string updateMessage)
    {
        _logger.LogInformation("Sending customer order update notification for order {OrderId}", orderId);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for customer update notification", orderId);
            return;
        }

        var templateData = new Dictionary<string, string>
        {
            { "OrderNumber", order.OrderNumber },
            { "CustomerName", order.CustomerName },
            { "UpdateMessage", updateMessage },
            { "CurrentStatus", order.Status.ToString() }
        };

        var emailRequest = new EmailNotificationRequest
        {
            To = $"{order.CustomerId}@example.com", // In production, get actual email from customer data
            Subject = $"Order Update - {order.OrderNumber}",
            TemplateName = "CustomerOrderUpdate",
            TemplateData = templateData,
            Priority = NotificationPriority.Normal
        };

        try
        {
            await SendEmailNotificationAsync(emailRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send customer order update notification for order {OrderId}", orderId);
        }
    }

    public async Task SendBulkNotificationAsync(BulkNotificationRequest request)
    {
        _logger.LogInformation("Sending bulk notification to {RecipientCount} recipients", request.Recipients.Count);

        // Send notifications sequentially to avoid DbContext concurrency issues
        foreach (var recipient in request.Recipients)
        {
            try
            {
                if (request.Type == NotificationType.Email)
                {
                    var emailRequest = new EmailNotificationRequest
                    {
                        To = recipient,
                        Subject = request.Subject,
                        Body = request.Message,
                        TemplateName = request.TemplateName,
                        TemplateData = request.TemplateData,
                        Priority = request.Priority
                    };
                    await SendEmailNotificationAsync(emailRequest);
                }
                else if (request.Type == NotificationType.SMS)
                {
                    var smsRequest = new SmsNotificationRequest
                    {
                        PhoneNumber = recipient,
                        Message = request.Message,
                        TemplateName = request.TemplateName,
                        TemplateData = request.TemplateData,
                        Priority = request.Priority
                    };
                    await SendSmsNotificationAsync(smsRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification to recipient {Recipient}", recipient);
                // Continue with other recipients
            }
        }

        _logger.LogInformation("Bulk notification sent to {RecipientCount} recipients", request.Recipients.Count);
    }

    public async Task<List<ProcurementPlanner.Core.Models.NotificationTemplate>> GetNotificationTemplatesAsync(NotificationType type)
    {
        return await _context.NotificationTemplates
            .Where(t => t.Type == type && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new ProcurementPlanner.Core.Models.NotificationTemplate
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Type = t.Type,
                Subject = t.Subject,
                Body = t.Body,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt,
                RequiredParameters = t.RequiredParameters
            })
            .ToListAsync();
    }

    public async Task<ProcurementPlanner.Core.Models.NotificationTemplate?> GetNotificationTemplateAsync(string templateName)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive);

        if (template == null) return null;

        return new ProcurementPlanner.Core.Models.NotificationTemplate
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Type = template.Type,
            Subject = template.Subject,
            Body = template.Body,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt ?? template.CreatedAt,
            RequiredParameters = template.RequiredParameters
        };
    }

    public async Task CreateNotificationTemplateAsync(CreateNotificationTemplateRequest request)
    {
        _logger.LogInformation("Creating notification template {TemplateName}", request.Name);

        var template = new ProcurementPlanner.Core.Entities.NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Subject = request.Subject,
            Body = request.Body,
            IsActive = true,
            RequiredParameters = request.RequiredParameters,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification template {TemplateName} created successfully", request.Name);
    }

    public async Task UpdateNotificationTemplateAsync(Guid templateId, UpdateNotificationTemplateRequest request)
    {
        _logger.LogInformation("Updating notification template {TemplateId}", templateId);

        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException($"Notification template with ID {templateId} not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
            template.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            template.Description = request.Description;

        if (!string.IsNullOrEmpty(request.Subject))
            template.Subject = request.Subject;

        if (!string.IsNullOrEmpty(request.Body))
            template.Body = request.Body;

        if (request.IsActive.HasValue)
            template.IsActive = request.IsActive.Value;

        if (request.RequiredParameters != null)
            template.RequiredParameters = request.RequiredParameters;

        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification template {TemplateId} updated successfully", templateId);
    }

    private async Task SendEmailViaSMTPAsync(string to, string? cc, string? bcc, string subject, string body, bool isHtml, List<NotificationAttachment>? attachments)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:Username"] ?? "";
        var smtpPassword = _configuration["Email:Password"] ?? "";
        var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@procurementplanner.com";
        var fromName = _configuration["Email:FromName"] ?? "Procurement Planner";

        using var client = new SmtpClient(smtpHost, smtpPort);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

        using var message = new MailMessage();
        message.From = new MailAddress(fromEmail, fromName);
        message.To.Add(to);

        if (!string.IsNullOrEmpty(cc))
            message.CC.Add(cc);

        if (!string.IsNullOrEmpty(bcc))
            message.Bcc.Add(bcc);

        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = isHtml;

        // Add attachments if any
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                message.Attachments.Add(mailAttachment);
            }
        }

        // In development, just log the email instead of sending
        if (_configuration["Environment"] == "Development")
        {
            _logger.LogInformation("Email would be sent to {To} with subject: {Subject}", to, subject);
            await Task.Delay(100); // Simulate sending delay
        }
        else
        {
            await client.SendMailAsync(message);
        }
    }

    private async Task SendSmsViaMockProviderAsync(string phoneNumber, string message)
    {
        // Mock SMS implementation - in production, integrate with SMS provider like Twilio
        _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", phoneNumber, message);
        await Task.Delay(100); // Simulate sending delay
    }
}