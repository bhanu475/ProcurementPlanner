using AutoMapper;
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
[Route("api/supplier-portal")]
[Authorize]
[AuthorizeRole(UserRole.Supplier)]
public class SupplierPortalController : ControllerBase
{
    private readonly ISupplierOrderConfirmationService _supplierOrderService;
    private readonly IMapper _mapper;
    private readonly ILogger<SupplierPortalController> _logger;

    public SupplierPortalController(
        ISupplierOrderConfirmationService supplierOrderService,
        IMapper mapper,
        ILogger<SupplierPortalController> logger)
    {
        _supplierOrderService = supplierOrderService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the supplier dashboard with summary metrics
    /// </summary>
    /// <returns>Supplier dashboard summary</returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<SupplierDashboardDto>>> GetDashboard()
    {
        try
        {
            var supplierId = GetCurrentSupplierId();
            var summary = await _supplierOrderService.GetSupplierDashboardSummaryAsync(supplierId);
            var dashboardDto = _mapper.Map<SupplierDashboardDto>(summary);

            return Ok(new ApiResponse<SupplierDashboardDto>
            {
                Success = true,
                Data = dashboardDto,
                Message = "Dashboard retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier dashboard");
            return StatusCode(500, new ApiResponse<SupplierDashboardDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the dashboard"
            });
        }
    }

    /// <summary>
    /// Gets all purchase orders for the current supplier
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of purchase orders</returns>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderSummaryDto>>>> GetPurchaseOrders([FromQuery] PurchaseOrderStatus? status = null)
    {
        try
        {
            var supplierId = GetCurrentSupplierId();
            var orders = await _supplierOrderService.GetSupplierPurchaseOrdersAsync(supplierId, status);
            var orderDtos = _mapper.Map<List<PurchaseOrderSummaryDto>>(orders);

            return Ok(new ApiResponse<List<PurchaseOrderSummaryDto>>
            {
                Success = true,
                Data = orderDtos,
                Message = $"Retrieved {orderDtos.Count} purchase orders"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase orders for supplier");
            return StatusCode(500, new ApiResponse<List<PurchaseOrderSummaryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving purchase orders"
            });
        }
    }

    /// <summary>
    /// Gets a specific purchase order by ID
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <returns>Detailed purchase order information</returns>
    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> GetPurchaseOrder(Guid id)
    {
        try
        {
            var supplierId = GetCurrentSupplierId();
            var order = await _supplierOrderService.GetSupplierPurchaseOrderAsync(id, supplierId);
            var orderDto = _mapper.Map<PurchaseOrderDetailDto>(order);

            return Ok(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = true,
                Data = orderDto,
                Message = "Purchase order retrieved successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order {OrderId}", id);
            return StatusCode(500, new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the purchase order"
            });
        }
    }

    /// <summary>
    /// Confirms a purchase order with packaging and delivery details
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="confirmationDto">Confirmation details</param>
    /// <returns>Updated purchase order</returns>
    [HttpPost("orders/{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> ConfirmPurchaseOrder(
        Guid id, 
        [FromBody] SupplierOrderConfirmationDto confirmationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
                {
                    Success = false,
                    Message = "Invalid confirmation data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var supplierId = GetCurrentSupplierId();
            var confirmation = _mapper.Map<SupplierOrderConfirmation>(confirmationDto);
            
            var updatedOrder = await _supplierOrderService.ConfirmPurchaseOrderAsync(id, supplierId, confirmation);
            var orderDto = _mapper.Map<PurchaseOrderDetailDto>(updatedOrder);

            return Ok(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = true,
                Data = orderDto,
                Message = "Purchase order confirmed successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming purchase order {OrderId}", id);
            return StatusCode(500, new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = "An error occurred while confirming the purchase order"
            });
        }
    }

    /// <summary>
    /// Rejects a purchase order with reason
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="rejectionDto">Rejection details</param>
    /// <returns>Updated purchase order</returns>
    [HttpPost("orders/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> RejectPurchaseOrder(
        Guid id, 
        [FromBody] PurchaseOrderRejectionDto rejectionDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
                {
                    Success = false,
                    Message = "Invalid rejection data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var supplierId = GetCurrentSupplierId();
            var updatedOrder = await _supplierOrderService.RejectPurchaseOrderAsync(id, supplierId, rejectionDto.RejectionReason);
            var orderDto = _mapper.Map<PurchaseOrderDetailDto>(updatedOrder);

            return Ok(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = true,
                Data = orderDto,
                Message = "Purchase order rejected successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting purchase order {OrderId}", id);
            return StatusCode(500, new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = "An error occurred while rejecting the purchase order"
            });
        }
    }

    /// <summary>
    /// Updates packaging and delivery details for purchase order items
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="itemUpdates">List of item updates</param>
    /// <returns>Updated purchase order</returns>
    [HttpPut("orders/{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> UpdatePurchaseOrderItems(
        Guid id, 
        [FromBody] List<SupplierItemUpdateDto> itemUpdates)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
                {
                    Success = false,
                    Message = "Invalid item update data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var supplierId = GetCurrentSupplierId();
            var updates = _mapper.Map<List<SupplierItemUpdate>>(itemUpdates);
            
            var updatedOrder = await _supplierOrderService.UpdatePurchaseOrderItemsAsync(id, supplierId, updates);
            var orderDto = _mapper.Map<PurchaseOrderDetailDto>(updatedOrder);

            return Ok(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = true,
                Data = orderDto,
                Message = "Purchase order items updated successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order items for {OrderId}", id);
            return StatusCode(500, new ApiResponse<PurchaseOrderDetailDto>
            {
                Success = false,
                Message = "An error occurred while updating purchase order items"
            });
        }
    }

    /// <summary>
    /// Validates delivery dates for purchase order items
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="deliveryDates">Dictionary of item ID to estimated delivery date</param>
    /// <returns>Validation result</returns>
    [HttpPost("orders/{id:guid}/validate-delivery-dates")]
    public async Task<ActionResult<ApiResponse<DeliveryDateValidationDto>>> ValidateDeliveryDates(
        Guid id, 
        [FromBody] Dictionary<Guid, DateTime> deliveryDates)
    {
        try
        {
            var validationResult = await _supplierOrderService.ValidateDeliveryDatesAsync(id, deliveryDates);
            var validationDto = _mapper.Map<DeliveryDateValidationDto>(validationResult);

            return Ok(new ApiResponse<DeliveryDateValidationDto>
            {
                Success = true,
                Data = validationDto,
                Message = "Delivery dates validated successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<DeliveryDateValidationDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating delivery dates for purchase order {OrderId}", id);
            return StatusCode(500, new ApiResponse<DeliveryDateValidationDto>
            {
                Success = false,
                Message = "An error occurred while validating delivery dates"
            });
        }
    }

    /// <summary>
    /// Gets supplier order history with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of historical orders</returns>
    [HttpGet("orders/history")]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderSummaryDto>>>> GetOrderHistory([FromQuery] SupplierOrderHistoryFilterDto filter)
    {
        try
        {
            var supplierId = GetCurrentSupplierId();
            var historyFilter = _mapper.Map<SupplierOrderHistoryFilter>(filter);
            
            var result = await _supplierOrderService.GetSupplierOrderHistoryAsync(supplierId, historyFilter);
            
            var pagedResult = new PagedResult<PurchaseOrderSummaryDto>
            {
                Items = _mapper.Map<List<PurchaseOrderSummaryDto>>(result.Items),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

            return Ok(new ApiResponse<PagedResult<PurchaseOrderSummaryDto>>
            {
                Success = true,
                Data = pagedResult,
                Message = $"Retrieved {pagedResult.Items.Count} orders from history"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for supplier");
            return StatusCode(500, new ApiResponse<PagedResult<PurchaseOrderSummaryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving order history"
            });
        }
    }

    /// <summary>
    /// Gets the current supplier ID from the JWT token
    /// </summary>
    /// <returns>Supplier ID</returns>
    private Guid GetCurrentSupplierId()
    {
        var supplierIdClaim = User.FindFirst("SupplierId")?.Value;
        if (string.IsNullOrEmpty(supplierIdClaim) || !Guid.TryParse(supplierIdClaim, out var supplierId))
        {
            throw new UnauthorizedAccessException("Supplier ID not found in token");
        }
        return supplierId;
    }
}