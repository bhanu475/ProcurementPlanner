using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementPlanner.API.Authorization;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using System.Security.Claims;

namespace ProcurementPlanner.API.Controllers;

[ApiController]
[Route("api/customer/orders")]
[Authorize]
public class CustomerOrderTrackingController : ControllerBase
{
    private readonly ICustomerOrderTrackingService _trackingService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerOrderTrackingController> _logger;

    public CustomerOrderTrackingController(
        ICustomerOrderTrackingService trackingService,
        IMapper mapper,
        ILogger<CustomerOrderTrackingController> logger)
    {
        _trackingService = trackingService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get customer's orders with filtering and pagination
    /// </summary>
    [HttpGet]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<List<CustomerOrderSummaryDto>>>> GetCustomerOrders([FromQuery] OrderTrackingFilterDto filter)
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<List<CustomerOrderSummaryDto>>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting orders for customer {CustomerId}", customerId);

            var trackingFilter = _mapper.Map<Core.Interfaces.OrderTrackingFilter>(filter);
            var orders = await _trackingService.GetCustomerOrdersAsync(customerId, trackingFilter);
            var response = _mapper.Map<List<CustomerOrderSummaryDto>>(orders);

            return Ok(ApiResponse<List<CustomerOrderSummaryDto>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer orders");
            return StatusCode(500, ApiResponse<List<CustomerOrderSummaryDto>>.ErrorResponse("An error occurred while retrieving orders"));
        }
    }

    /// <summary>
    /// Get specific order details for customer
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<OrderTrackingDto>>> GetCustomerOrder(Guid orderId)
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<OrderTrackingDto>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting order {OrderId} for customer {CustomerId}", orderId, customerId);

            var order = await _trackingService.GetCustomerOrderAsync(orderId, customerId);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderTrackingDto>.ErrorResponse("Order not found"));
            }

            var response = _mapper.Map<OrderTrackingDto>(order);
            return Ok(ApiResponse<OrderTrackingDto>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<OrderTrackingDto>.ErrorResponse("An error occurred while retrieving the order"));
        }
    }

    /// <summary>
    /// Get order status history for customer
    /// </summary>
    [HttpGet("{orderId:guid}/history")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<List<OrderStatusHistoryDto>>>> GetOrderStatusHistory(Guid orderId)
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<List<OrderStatusHistoryDto>>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting status history for order {OrderId} and customer {CustomerId}", orderId, customerId);

            var history = await _trackingService.GetOrderStatusHistoryAsync(orderId, customerId);
            var response = _mapper.Map<List<OrderStatusHistoryDto>>(history);

            return Ok(ApiResponse<List<OrderStatusHistoryDto>>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order status history for {OrderId}", orderId);
            return StatusCode(500, ApiResponse<List<OrderStatusHistoryDto>>.ErrorResponse("An error occurred while retrieving order history"));
        }
    }

    /// <summary>
    /// Get order milestones and timeline for customer
    /// </summary>
    [HttpGet("{orderId:guid}/timeline")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<List<OrderMilestoneDto>>>> GetOrderTimeline(Guid orderId)
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<List<OrderMilestoneDto>>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting timeline for order {OrderId} and customer {CustomerId}", orderId, customerId);

            var milestones = await _trackingService.GetOrderMilestonesAsync(orderId, customerId);
            var response = _mapper.Map<List<OrderMilestoneDto>>(milestones);

            return Ok(ApiResponse<List<OrderMilestoneDto>>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order timeline for {OrderId}", orderId);
            return StatusCode(500, ApiResponse<List<OrderMilestoneDto>>.ErrorResponse("An error occurred while retrieving order timeline"));
        }
    }

    /// <summary>
    /// Get customer's recent orders
    /// </summary>
    [HttpGet("recent")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<List<CustomerOrderSummaryDto>>>> GetRecentOrders([FromQuery] int count = 5)
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<List<CustomerOrderSummaryDto>>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting {Count} recent orders for customer {CustomerId}", count, customerId);

            var orders = await _trackingService.GetRecentOrdersAsync(customerId, count);
            var response = _mapper.Map<List<CustomerOrderSummaryDto>>(orders);

            return Ok(ApiResponse<List<CustomerOrderSummaryDto>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent orders for customer");
            return StatusCode(500, ApiResponse<List<CustomerOrderSummaryDto>>.ErrorResponse("An error occurred while retrieving recent orders"));
        }
    }

    /// <summary>
    /// Get customer order tracking summary
    /// </summary>
    [HttpGet("summary")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<CustomerOrderTrackingSummaryDto>>> GetOrderTrackingSummary()
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<CustomerOrderTrackingSummaryDto>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting order tracking summary for customer {CustomerId}", customerId);

            var summary = await _trackingService.GetOrderTrackingSummaryAsync(customerId);
            var response = _mapper.Map<CustomerOrderTrackingSummaryDto>(summary);

            return Ok(ApiResponse<CustomerOrderTrackingSummaryDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking summary for customer");
            return StatusCode(500, ApiResponse<CustomerOrderTrackingSummaryDto>.ErrorResponse("An error occurred while retrieving order summary"));
        }
    }

    /// <summary>
    /// Get customer notification preferences
    /// </summary>
    [HttpGet("notifications/preferences")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<CustomerNotificationPreferencesDto>>> GetNotificationPreferences()
    {
        try
        {
            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<CustomerNotificationPreferencesDto>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Getting notification preferences for customer {CustomerId}", customerId);

            var preferences = await _trackingService.GetNotificationPreferencesAsync(customerId);
            if (preferences == null)
            {
                // Return default preferences if none exist
                var defaultPreferences = new CustomerNotificationPreferencesDto
                {
                    CustomerId = customerId,
                    CustomerName = customerId,
                    EmailNotifications = true,
                    SmsNotifications = false,
                    StatusChangeNotifications = true,
                    DeliveryReminders = true,
                    DelayNotifications = true
                };
                return Ok(ApiResponse<CustomerNotificationPreferencesDto>.SuccessResponse(defaultPreferences));
            }

            var response = _mapper.Map<CustomerNotificationPreferencesDto>(preferences);
            return Ok(ApiResponse<CustomerNotificationPreferencesDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for customer");
            return StatusCode(500, ApiResponse<CustomerNotificationPreferencesDto>.ErrorResponse("An error occurred while retrieving notification preferences"));
        }
    }

    /// <summary>
    /// Update customer notification preferences
    /// </summary>
    [HttpPut("notifications/preferences")]
    [AuthorizeRole(UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<CustomerNotificationPreferencesDto>>> UpdateNotificationPreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CustomerNotificationPreferencesDto>.ErrorResponse("Invalid notification preferences data", GetModelStateErrors().SelectMany(kvp => kvp.Value).ToList()));
            }

            var customerId = GetCustomerIdFromToken();
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(ApiResponse<CustomerNotificationPreferencesDto>.ErrorResponse("Invalid customer ID"));
            }

            _logger.LogInformation("Updating notification preferences for customer {CustomerId}", customerId);

            var updateRequest = _mapper.Map<Core.Interfaces.UpdateCustomerNotificationPreferencesRequest>(request);
            await _trackingService.UpdateNotificationPreferencesAsync(customerId, updateRequest);

            // Get updated preferences
            var updatedPreferences = await _trackingService.GetNotificationPreferencesAsync(customerId);
            var response = _mapper.Map<CustomerNotificationPreferencesDto>(updatedPreferences);

            return Ok(ApiResponse<CustomerNotificationPreferencesDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for customer");
            return StatusCode(500, ApiResponse<CustomerNotificationPreferencesDto>.ErrorResponse("An error occurred while updating notification preferences"));
        }
    }

    private string? GetCustomerIdFromToken()
    {
        // For customers, use the Username (which is the business customer ID) instead of the internal User.Id
        return User.FindFirst(ClaimTypes.Name)?.Value;
    }

    private Dictionary<string, List<string>> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
            );
    }
}