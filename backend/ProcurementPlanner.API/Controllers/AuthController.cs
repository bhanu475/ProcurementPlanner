using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementPlanner.API.Authorization;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using System.Security.Claims;

namespace ProcurementPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authenticationService.LoginAsync(request, ipAddress);
            
            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Failed login attempt for {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid email or password"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("An error occurred during login"));
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authenticationService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Invalid refresh token attempt: {Message}", ex.Message);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid refresh token"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("An error occurred during token refresh"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            await _authenticationService.RevokeTokenAsync(request.RefreshToken, ipAddress);
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} logged out successfully", userId);
            
            return Ok(ApiResponse<object>.SuccessResponse(null, "Logged out successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid logout attempt: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid refresh token"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred during logout"));
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user"));
            }

            var success = await _authenticationService.ChangePasswordAsync(userId, request);
            
            if (!success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Current password is incorrect"));
            }

            _logger.LogInformation("User {UserId} changed password successfully", userId);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Password changed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while changing password"));
        }
    }

    [HttpPost("create-user")]
    [AuthorizeRole(UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _authenticationService.CreateUserAsync(request);
            
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName
            };

            _logger.LogInformation("User {Username} created successfully by {AdminId}", 
                user.Username, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo, "User created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Failed to create user {Username}: {Message}", request.Username, ex.Message);
            return BadRequest(ApiResponse<UserInfo>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", request.Username);
            return StatusCode(500, ApiResponse<UserInfo>.ErrorResponse("An error occurred while creating user"));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<UserInfo>.ErrorResponse("Invalid user"));
            }

            var user = await _authenticationService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserInfo>.ErrorResponse("User not found"));
            }

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName
            };

            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, ApiResponse<UserInfo>.ErrorResponse("An error occurred while getting user information"));
        }
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}