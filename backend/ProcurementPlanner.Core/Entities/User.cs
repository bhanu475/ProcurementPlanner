using System.ComponentModel.DataAnnotations;

namespace ProcurementPlanner.Core.Entities;

public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public enum UserRole
{
    Administrator = 1,
    LMRPlanner = 2,
    Supplier = 3,
    Customer = 4
}