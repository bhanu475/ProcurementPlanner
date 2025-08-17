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
[Route("api/[controller]")]
[Authorize]
public class ProcurementController : ControllerBase
{
    private readonly IProcurementPlanningService _procurementPlanningService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProcurementController> _logger;

    public ProcurementController(
        IProcurementPlanningService procurementPlanningService,
        IMapper mapper,
        ILogger<ProcurementController> logger)
    {
        _procurementPlanningService = procurementPlanningService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Generate distribution suggestion for a customer order
    /// </summary>
    /// <param name="customerOrderId">Customer order ID</param>
    /// <returns>Distribution suggestion with supplier allocations</returns>
    [HttpGet("suggestions/{customerOrderId:guid}")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<DistributionSuggestionResponse>>> GetDistributionSuggestion(Guid customerOrderId)
    {
        try
        {
            _logger.LogInformation("Getting distribution suggestion for customer order {CustomerOrderId}", customerOrderId);

            var suggestion = await _procurementPlanningService.SuggestSupplierDistributionAsync(customerOrderId);
            var response = _mapper.Map<DistributionSuggestionResponse>(suggestion);

            return Ok(ApiResponse<DistributionSuggestionResponse>.SuccessResponse(response));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid customer order ID: {CustomerOrderId}", customerOrderId);
            return BadRequest(ApiResponse<DistributionSuggestionResponse>.ErrorResponse("Invalid customer order ID", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distribution suggestion for customer order {CustomerOrderId}", customerOrderId);
            return StatusCode(500, ApiResponse<DistributionSuggestionResponse>.ErrorResponse("Internal server error", new List<string> { "An error occurred while generating distribution suggestion" }));
        }
    }

    /// <summary>
    /// Create purchase orders from customer order using distribution plan
    /// </summary>
    /// <param name="request">Purchase order creation request</param>
    /// <returns>Created purchase orders</returns>
    [HttpPost("purchase-orders")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderResponse>>>> CreatePurchaseOrders([FromBody] CreatePurchaseOrdersRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Validation failed", errors));
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating purchase orders for customer order {CustomerOrderId} by user {UserId}", 
                request.CustomerOrderId, userId);

            // Map the distribution plan
            var distributionPlan = _mapper.Map<DistributionPlan>(request.DistributionPlan);
            distributionPlan.CustomerOrderId = request.CustomerOrderId;
            distributionPlan.CreatedBy = userId;
            distributionPlan.Notes = request.Notes;

            var purchaseOrders = await _procurementPlanningService.CreatePurchaseOrdersAsync(request.CustomerOrderId, distributionPlan);
            var response = _mapper.Map<List<PurchaseOrderResponse>>(purchaseOrders);

            _logger.LogInformation("Successfully created {Count} purchase orders for customer order {CustomerOrderId}", 
                response.Count, request.CustomerOrderId);

            return Ok(ApiResponse<List<PurchaseOrderResponse>>.SuccessResponse(response, $"Successfully created {response.Count} purchase orders"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating purchase orders");
            return BadRequest(ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Invalid request", new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for creating purchase orders");
            return BadRequest(ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Invalid operation", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase orders for customer order {CustomerOrderId}", request.CustomerOrderId);
            return StatusCode(500, ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Internal server error", new List<string> { "An error occurred while creating purchase orders" }));
        }
    }

    /// <summary>
    /// Validate distribution plan for a customer order
    /// </summary>
    /// <param name="customerOrderId">Customer order ID</param>
    /// <param name="distributionPlan">Distribution plan to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate/{customerOrderId:guid}")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<DistributionValidationResponse>>> ValidateDistributionPlan(
        Guid customerOrderId, 
        [FromBody] DistributionPlanDto distributionPlan)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<DistributionValidationResponse>.ErrorResponse("Validation failed", errors));
            }

            _logger.LogInformation("Validating distribution plan for customer order {CustomerOrderId}", customerOrderId);

            var mappedPlan = _mapper.Map<DistributionPlan>(distributionPlan);
            var validationResult = await _procurementPlanningService.ValidateDistributionPlanAsync(customerOrderId, mappedPlan);
            var response = _mapper.Map<DistributionValidationResponse>(validationResult);

            return Ok(ApiResponse<DistributionValidationResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating distribution plan for customer order {CustomerOrderId}", customerOrderId);
            return StatusCode(500, ApiResponse<DistributionValidationResponse>.ErrorResponse("Internal server error", new List<string> { "An error occurred while validating distribution plan" }));
        }
    }

    /// <summary>
    /// Get purchase orders for a specific supplier
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of purchase orders for the supplier</returns>
    [HttpGet("supplier/{supplierId:guid}/purchase-orders")]
    [AuthorizeRole(UserRole.Supplier, UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderResponse>>>> GetPurchaseOrdersBySupplier(
        Guid supplierId, 
        [FromQuery] PurchaseOrderStatus? status = null)
    {
        try
        {
            // If user is a supplier, ensure they can only access their own purchase orders
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            if (userRole == UserRole.Supplier)
            {
                // TODO: Implement supplier user to supplier ID mapping
                // For now, we'll allow access but this should be restricted in production
                _logger.LogInformation("Supplier user {UserId} accessing purchase orders for supplier {SupplierId}", 
                    currentUserId, supplierId);
            }

            _logger.LogInformation("Getting purchase orders for supplier {SupplierId} with status filter: {Status}", 
                supplierId, status);

            var purchaseOrders = await _procurementPlanningService.GetPurchaseOrdersBySupplierAsync(supplierId, status);
            var response = _mapper.Map<List<PurchaseOrderResponse>>(purchaseOrders);

            return Ok(ApiResponse<List<PurchaseOrderResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders for supplier {SupplierId}", supplierId);
            return StatusCode(500, ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Internal server error", new List<string> { "An error occurred while retrieving purchase orders" }));
        }
    }

    /// <summary>
    /// Get purchase orders for a specific customer order
    /// </summary>
    /// <param name="customerOrderId">Customer order ID</param>
    /// <returns>List of purchase orders for the customer order</returns>
    [HttpGet("customer-order/{customerOrderId:guid}/purchase-orders")]
    [AuthorizeRole(UserRole.LMRPlanner, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderResponse>>>> GetPurchaseOrdersByCustomerOrder(Guid customerOrderId)
    {
        try
        {
            _logger.LogInformation("Getting purchase orders for customer order {CustomerOrderId}", customerOrderId);

            var purchaseOrders = await _procurementPlanningService.GetPurchaseOrdersByCustomerOrderAsync(customerOrderId);
            var response = _mapper.Map<List<PurchaseOrderResponse>>(purchaseOrders);

            return Ok(ApiResponse<List<PurchaseOrderResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders for customer order {CustomerOrderId}", customerOrderId);
            return StatusCode(500, ApiResponse<List<PurchaseOrderResponse>>.ErrorResponse("Internal server error", new List<string> { "An error occurred while retrieving purchase orders" }));
        }
    }

    /// <summary>
    /// Confirm a purchase order (supplier endpoint)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="confirmation">Supplier confirmation details</param>
    /// <returns>Updated purchase order</returns>
    [HttpPost("{purchaseOrderId:guid}/confirm")]
    [AuthorizeRole(UserRole.Supplier, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> ConfirmPurchaseOrder(
        Guid purchaseOrderId, 
        [FromBody] SupplierConfirmationRequest confirmation)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Validation failed", errors));
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Confirming purchase order {PurchaseOrderId} by user {UserId}", purchaseOrderId, userId);

            var supplierConfirmation = _mapper.Map<SupplierConfirmation>(confirmation);
            supplierConfirmation.ConfirmedBy = userId;

            var purchaseOrder = await _procurementPlanningService.ConfirmPurchaseOrderAsync(purchaseOrderId, supplierConfirmation);
            var response = _mapper.Map<PurchaseOrderResponse>(purchaseOrder);

            _logger.LogInformation("Purchase order {PurchaseOrderId} confirmed successfully", purchaseOrderId);

            return Ok(ApiResponse<PurchaseOrderResponse>.SuccessResponse(response, "Purchase order confirmed successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid purchase order ID: {PurchaseOrderId}", purchaseOrderId);
            return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Invalid purchase order ID", new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for confirming purchase order {PurchaseOrderId}", purchaseOrderId);
            return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Invalid operation", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, ApiResponse<PurchaseOrderResponse>.ErrorResponse("Internal server error", new List<string> { "An error occurred while confirming purchase order" }));
        }
    }

    /// <summary>
    /// Reject a purchase order (supplier endpoint)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="request">Rejection request with reason</param>
    /// <returns>Updated purchase order</returns>
    [HttpPost("{purchaseOrderId:guid}/reject")]
    [AuthorizeRole(UserRole.Supplier, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderResponse>>> RejectPurchaseOrder(
        Guid purchaseOrderId, 
        [FromBody] RejectPurchaseOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Validation failed", errors));
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Rejecting purchase order {PurchaseOrderId} by user {UserId} with reason: {Reason}", 
                purchaseOrderId, userId, request.RejectionReason);

            var purchaseOrder = await _procurementPlanningService.RejectPurchaseOrderAsync(purchaseOrderId, request.RejectionReason);
            var response = _mapper.Map<PurchaseOrderResponse>(purchaseOrder);

            _logger.LogInformation("Purchase order {PurchaseOrderId} rejected successfully", purchaseOrderId);

            return Ok(ApiResponse<PurchaseOrderResponse>.SuccessResponse(response, "Purchase order rejected successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid purchase order ID: {PurchaseOrderId}", purchaseOrderId);
            return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Invalid purchase order ID", new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for rejecting purchase order {PurchaseOrderId}", purchaseOrderId);
            return BadRequest(ApiResponse<PurchaseOrderResponse>.ErrorResponse("Invalid operation", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, ApiResponse<PurchaseOrderResponse>.ErrorResponse("Internal server error", new List<string> { "An error occurred while rejecting purchase order" }));
        }
    }

    /// <summary>
    /// Update purchase order item details (supplier endpoint)
    /// </summary>
    /// <param name="purchaseOrderItemId">Purchase order item ID</param>
    /// <param name="request">Item update request</param>
    /// <returns>Updated purchase order item</returns>
    [HttpPut("items/{purchaseOrderItemId:guid}")]
    [AuthorizeRole(UserRole.Supplier, UserRole.Administrator)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderItemResponse>>> UpdatePurchaseOrderItem(
        Guid purchaseOrderItemId, 
        [FromBody] PurchaseOrderItemUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<PurchaseOrderItemResponse>.ErrorResponse("Validation failed", errors));
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Updating purchase order item {PurchaseOrderItemId} by user {UserId}", 
                purchaseOrderItemId, userId);

            var itemUpdate = _mapper.Map<PurchaseOrderItemUpdate>(request);
            var purchaseOrderItem = await _procurementPlanningService.UpdatePurchaseOrderItemAsync(purchaseOrderItemId, itemUpdate);
            var response = _mapper.Map<PurchaseOrderItemResponse>(purchaseOrderItem);

            _logger.LogInformation("Purchase order item {PurchaseOrderItemId} updated successfully", purchaseOrderItemId);

            return Ok(ApiResponse<PurchaseOrderItemResponse>.SuccessResponse(response, "Purchase order item updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid purchase order item ID: {PurchaseOrderItemId}", purchaseOrderItemId);
            return BadRequest(ApiResponse<PurchaseOrderItemResponse>.ErrorResponse("Invalid purchase order item ID", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order item {PurchaseOrderItemId}", purchaseOrderItemId);
            return StatusCode(500, ApiResponse<PurchaseOrderItemResponse>.ErrorResponse("Internal server error", new List<string> { "An error occurred while updating purchase order item" }));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Customer;
    }
}