using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProcurementPlanner.API.Authorization;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;

namespace ProcurementPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierManagementService _supplierService;
    private readonly IMapper _mapper;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(
        ISupplierManagementService supplierService,
        IMapper mapper,
        ILogger<SupplierController> logger)
    {
        _supplierService = supplierService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner)]
    public async Task<ActionResult<ApiResponse<List<SupplierResponse>>>> GetSuppliers([FromQuery] SupplierFilterRequest filter)
    {
        try
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            var response = _mapper.Map<List<SupplierResponse>>(suppliers);
            return Ok(ApiResponse<List<SupplierResponse>>.SuccessResponse(response, "Suppliers retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers");
            return StatusCode(500, ApiResponse<List<SupplierResponse>>.ErrorResponse("An error occurred while retrieving suppliers"));
        }
    }

    [HttpGet("{id:guid}")]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner, UserRole.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> GetSupplier(Guid id)
    {
        try
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return NotFound(ApiResponse<SupplierResponse>.ErrorResponse("Supplier not found"));
            }

            var response = _mapper.Map<SupplierResponse>(supplier);
            return Ok(ApiResponse<SupplierResponse>.SuccessResponse(response, "Supplier retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier {SupplierId}", id);
            return StatusCode(500, ApiResponse<SupplierResponse>.ErrorResponse("An error occurred while retrieving the supplier"));
        }
    }

    [HttpPost]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner)]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        try
        {
            var supplier = _mapper.Map<Supplier>(request);
            var createdSupplier = await _supplierService.CreateSupplierAsync(supplier);
            var response = _mapper.Map<SupplierResponse>(createdSupplier);
            
            return CreatedAtAction(
                nameof(GetSupplier), 
                new { id = createdSupplier.Id }, 
                ApiResponse<SupplierResponse>.SuccessResponse(response, "Supplier created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SupplierResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return StatusCode(500, ApiResponse<SupplierResponse>.ErrorResponse("An error occurred while creating the supplier"));
        }
    }

    [HttpPut("{id:guid}")]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner, UserRole.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        try
        {
            var existingSupplier = await _supplierService.GetSupplierByIdAsync(id);
            if (existingSupplier == null)
            {
                return NotFound(ApiResponse<SupplierResponse>.ErrorResponse("Supplier not found"));
            }

            _mapper.Map(request, existingSupplier);
            existingSupplier.Id = id;

            var updatedSupplier = await _supplierService.UpdateSupplierAsync(existingSupplier);
            var response = _mapper.Map<SupplierResponse>(updatedSupplier);

            return Ok(ApiResponse<SupplierResponse>.SuccessResponse(response, "Supplier updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SupplierResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
            return StatusCode(500, ApiResponse<SupplierResponse>.ErrorResponse("An error occurred while updating the supplier"));
        }
    }

    [HttpPut("{id:guid}/capacity/{productType}")]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner, UserRole.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> UpdateSupplierCapacity(
        Guid id, 
        ProductType productType, 
        [FromBody] UpdateSupplierCapacityRequest request)
    {
        try
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return NotFound(ApiResponse<SupplierResponse>.ErrorResponse("Supplier not found"));
            }

            var updatedSupplier = await _supplierService.UpdateSupplierCapacityAsync(
                id, 
                productType, 
                request.MaxMonthlyCapacity, 
                request.CurrentCommitments);

            var response = _mapper.Map<SupplierResponse>(updatedSupplier);
            return Ok(ApiResponse<SupplierResponse>.SuccessResponse(response, "Supplier capacity updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SupplierResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier capacity");
            return StatusCode(500, ApiResponse<SupplierResponse>.ErrorResponse("An error occurred while updating supplier capacity"));
        }
    }

    [HttpGet("{id:guid}/performance")]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner, UserRole.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplierPerformanceResponse>>> GetSupplierPerformance(Guid id)
    {
        try
        {
            var performance = await _supplierService.GetSupplierPerformanceAsync(id);
            if (performance == null)
            {
                return NotFound(ApiResponse<SupplierPerformanceResponse>.ErrorResponse("Performance metrics not found for this supplier"));
            }

            var response = _mapper.Map<SupplierPerformanceResponse>(performance);
            return Ok(ApiResponse<SupplierPerformanceResponse>.SuccessResponse(response, "Performance metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier performance {SupplierId}", id);
            return StatusCode(500, ApiResponse<SupplierPerformanceResponse>.ErrorResponse("An error occurred while retrieving performance metrics"));
        }
    }

    [HttpPost("available")]
    [AuthorizeRole(UserRole.Administrator, UserRole.LMRPlanner)]
    public async Task<ActionResult<ApiResponse<List<SupplierResponse>>>> GetAvailableSuppliers([FromBody] AvailableSuppliersRequest request)
    {
        try
        {
            var suppliers = await _supplierService.GetAvailableSuppliersAsync(request.ProductType, request.RequiredCapacity);
            var response = _mapper.Map<List<SupplierResponse>>(suppliers);

            return Ok(ApiResponse<List<SupplierResponse>>.SuccessResponse(response, "Available suppliers retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available suppliers");
            return StatusCode(500, ApiResponse<List<SupplierResponse>>.ErrorResponse("An error occurred while retrieving available suppliers"));
        }
    }
}