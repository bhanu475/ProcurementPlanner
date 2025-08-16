using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;
using ProcurementPlanner.Infrastructure.Services;
using Xunit;

namespace ProcurementPlanner.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationService _authenticationService;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        _jwtSettings = new JwtSettings
        {
            SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);
        _authenticationService = new AuthenticationService(_context, jwtOptions);
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            Role = "LMRPlanner",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var user = await _authenticationService.CreateUserAsync(request);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
        Assert.Equal(UserRole.LMRPlanner, user.Role);
        Assert.True(user.IsActive);
        Assert.NotEmpty(user.PasswordHash);
        Assert.NotEqual(request.Password, user.PasswordHash); // Password should be hashed
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsArgumentException()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existing",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.LMRPlanner
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Username = "newuser",
            Email = "test@example.com",
            Password = "password123",
            Role = "LMRPlanner"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authenticationService.CreateUserAsync(request));
        Assert.Contains("email already exists", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ThrowsArgumentException()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "testuser",
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.LMRPlanner
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Username = "testuser",
            Email = "new@example.com",
            Password = "password123",
            Role = "LMRPlanner"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authenticationService.CreateUserAsync(request));
        Assert.Contains("username already exists", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.LMRPlanner,
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var response = await _authenticationService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Token);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.Email, response.User.Email);
        Assert.Equal(user.Role.ToString(), response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = UserRole.LMRPlanner,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.LMRPlanner,
            IsActive = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authenticationService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            Role = UserRole.LMRPlanner,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        };

        // Act
        var result = await _authenticationService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.True(result);
        
        // Verify password was actually changed
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = UserRole.LMRPlanner,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123"
        };

        // Act
        var result = await _authenticationService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRole.LMRPlanner,
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var token = _authenticationService.GenerateJwtToken(user);

        // Assert
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT tokens contain dots
    }

    [Fact]
    public void GenerateRefreshToken_ValidIpAddress_ReturnsRefreshToken()
    {
        // Arrange
        var ipAddress = "127.0.0.1";

        // Act
        var refreshToken = _authenticationService.GenerateRefreshToken(ipAddress);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken.Token);
        Assert.Equal(ipAddress, refreshToken.CreatedByIp);
        Assert.True(refreshToken.ExpiresAt > DateTime.UtcNow);
        Assert.False(refreshToken.IsRevoked);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.LMRPlanner
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authenticationService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _authenticationService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}