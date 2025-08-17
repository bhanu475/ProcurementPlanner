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
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderManagementService _orderService;
    private readonly IDashboardService _dashboardService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderManagementService orderService,
        IDashboardService dashboardService,
        IMapper mapper,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _dashboardService = dashboardService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get orders with filtering and pagination
    /// </summary>
    [HttpGet]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<PagedOrderResponseDto>>> GetOrders([FromQuery] OrderFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Getting orders with filter - Page: {Page}, PageSize: {PageSize}", 
                filter.Page, filter.PageSize);

            var request = _mapper.Map<Core.Models.OrderFilterRequest>(filter);
            var result = await _orderService.GetOrdersAsync(request);
            var response = _mapper.Map<PagedOrderResponseDto>(result);

            return Ok(ApiResponse<PagedOrderResponseDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, ApiResponse<PagedOrderResponseDto>.ErrorResponse("An error occurred while retrieving orders"));
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator, UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> GetOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting order {OrderId}", id);

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderResponseDto>.ErrorResponse("Order not found"));
            }

            // Check if customer can only access their own orders
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == UserRole.Customer.ToString())
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (order.CustomerId != userId)
                {
                    return Forbid();
                }
            }

            var response = _mapper.Map<OrderResponseDto>(order);
            return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while retrieving the order"));
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator, UserRole.Customer)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse("Invalid order data", GetModelStateErrors().SelectMany(kvp => kvp.Value).ToList()));
            }

            _logger.LogInformation("Creating new order for customer {CustomerId}", dto.CustomerId);

            var request = _mapper.Map<Core.Models.CreateOrderRequest>(dto);
            
            // Set the created by from JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userId, out var createdBy))
            {
                request.CreatedBy = createdBy;
            }
            else
            {
                return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse("Invalid user ID"));
            }

            // If customer is creating order, ensure they can only create for themselves
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == UserRole.Customer.ToString())
            {
                if (dto.CustomerId != userId)
                {
                    return Forbid();
                }
            }

            var order = await _orderService.CreateOrderAsync(request);
            var response = _mapper.Map<OrderResponseDto>(order);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, 
                ApiResponse<OrderResponseDto>.SuccessResponse(response));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid order creation request");
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while creating the order"));
        }
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    [HttpPut("{id:guid}")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> UpdateOrder(Guid id, [FromBody] UpdateOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse("Invalid order data", GetModelStateErrors().SelectMany(kvp => kvp.Value).ToList()));
            }

            _logger.LogInformation("Updating order {OrderId}", id);

            var request = _mapper.Map<Core.Models.UpdateOrderRequest>(dto);
            var order = await _orderService.UpdateOrderAsync(id, request);
            var response = _mapper.Map<OrderResponseDto>(order);

            return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(response));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid order update request for order {OrderId}", id);
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for order {OrderId}", id);
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while updating the order"));
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse("Invalid status data", GetModelStateErrors().SelectMany(kvp => kvp.Value).ToList()));
            }

            _logger.LogInformation("Updating order {OrderId} status to {Status}", id, dto.Status);

            var order = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            var response = _mapper.Map<OrderResponseDto>(order);

            return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(response));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status update request for order {OrderId}", id);
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition for order {OrderId}", id);
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while updating the order status"));
        }
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    [HttpDelete("{id:guid}")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting order {OrderId}", id);

            await _orderService.DeleteOrderAsync(id);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Order deleted successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid delete request for order {OrderId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for order {OrderId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the order"));
        }
    }

    /// <summary>
    /// Get orders by delivery date range
    /// </summary>
    [HttpGet("by-delivery-date")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<List<OrderResponseDto>>>> GetOrdersByDeliveryDate(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Getting orders by delivery date range {StartDate} to {EndDate}", 
                startDate, endDate);

            var orders = await _orderService.GetOrdersByDeliveryDateAsync(startDate, endDate);
            var response = _mapper.Map<List<OrderResponseDto>>(orders);

            return Ok(ApiResponse<List<OrderResponseDto>>.SuccessResponse(response));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range request");
            return BadRequest(ApiResponse<List<OrderResponseDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by delivery date");
            return StatusCode(500, ApiResponse<List<OrderResponseDto>>.ErrorResponse("An error occurred while retrieving orders"));
        }
    }

    /// <summary>
    /// Get dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<OrderDashboardResponseDto>>> GetDashboard([FromQuery] DashboardFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Getting dashboard summary");

            var request = _mapper.Map<Core.Models.DashboardFilterRequest>(filter);
            var summary = await _dashboardService.GetDashboardSummaryAsync(request);
            var response = _mapper.Map<OrderDashboardResponseDto>(summary);

            return Ok(ApiResponse<OrderDashboardResponseDto>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, ApiResponse<OrderDashboardResponseDto>.ErrorResponse("An error occurred while retrieving dashboard data"));
        }
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