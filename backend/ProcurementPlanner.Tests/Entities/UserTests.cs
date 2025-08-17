using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Role = UserRole.LMRPlanner,
            IsActive = true,
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_WithInvalidUsername_ShouldFailValidation(string username)
    {
        // Arrange
        var user = new User
        {
            Username = username,
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Role = UserRole.LMRPlanner
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Username"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void User_WithInvalidEmail_ShouldFailValidation(string email)
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = email,
            PasswordHash = "hashedpassword123",
            Role = UserRole.LMRPlanner
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_WithInvalidPasswordHash_ShouldFailValidation(string passwordHash)
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = UserRole.LMRPlanner
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("PasswordHash"));
    }

    [Fact]
    public void User_WithTooLongUsername_ShouldFailValidation()
    {
        // Arrange
        var user = new User
        {
            Username = new string('a', 101), // Exceeds 100 character limit
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Role = UserRole.LMRPlanner
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Username"));
    }

    [Fact]
    public void User_WithTooLongEmail_ShouldFailValidation()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // Exceeds 255 character limit
        var user = new User
        {
            Username = "testuser",
            Email = longEmail,
            PasswordHash = "hashedpassword123",
            Role = UserRole.LMRPlanner
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Email"));
    }

    [Fact]
    public void User_FullName_ShouldConcatenateFirstAndLastName()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void User_FullName_WithOnlyFirstName_ShouldReturnFirstName()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = null
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.Equal("John", fullName);
    }

    [Fact]
    public void User_FullName_WithOnlyLastName_ShouldReturnLastName()
    {
        // Arrange
        var user = new User
        {
            FirstName = null,
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.Equal("Doe", fullName);
    }

    [Fact]
    public void User_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.True(user.IsActive);
        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.LMRPlanner)]
    [InlineData(UserRole.Supplier)]
    [InlineData(UserRole.Customer)]
    public void User_WithValidRole_ShouldAcceptAllRoles(UserRole role)
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Role = role
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.Empty(validationResults);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}