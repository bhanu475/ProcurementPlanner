using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class CustomerNotificationPreferences : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    public bool EmailNotifications { get; set; } = true;

    public bool SmsNotifications { get; set; } = false;

    public bool StatusChangeNotifications { get; set; } = true;

    public bool DeliveryReminders { get; set; } = true;

    public bool DelayNotifications { get; set; } = true;

    [MaxLength(200)]
    public string? EmailAddress { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // Business logic methods
    public bool ShouldSendNotification(string notificationType)
    {
        return notificationType switch
        {
            "StatusChange" => StatusChangeNotifications,
            "DeliveryReminder" => DeliveryReminders,
            "Delay" => DelayNotifications,
            _ => false
        };
    }

    public string GetPreferredContactMethod()
    {
        if (EmailNotifications && !string.IsNullOrEmpty(EmailAddress))
            return "Email";
        if (SmsNotifications && !string.IsNullOrEmpty(PhoneNumber))
            return "SMS";
        return "None";
    }
}