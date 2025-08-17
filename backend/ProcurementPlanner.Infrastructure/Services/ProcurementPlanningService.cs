using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Infrastructure.Services;

public class ProcurementPlanningService : IProcurementPlanningService
{
    private readonly ICustomerOrderRepository _customerOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
    private readonly IDistributionAlgorithmService _distributionAlgorithmService;
    private readonly ILogger<ProcurementPlanningService> _logger;

    public ProcurementPlanningService(
        ICustomerOrderRepository customerOrderRepository,
        ISupplierRepository supplierRepository,
        IRepository<PurchaseOrder> purchaseOrderRepository,
        IRepository<PurchaseOrderItem> purchaseOrderItemRepository,
        IDistributionAlgorithmService distributionAlgorithmService,
        ILogger<ProcurementPlanningService> logger)
    {
        _customerOrderRepository = customerOrderRepository;
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderItemRepository = purchaseOrderItemRepository;
        _distributionAlgorithmService = distributionAlgorithmService;
        _logger = logger;
    }

    public async Task<List<PurchaseOrder>> CreatePurchaseOrdersAsync(Guid customerOrderId, DistributionPlan distributionPlan)
    {
        _logger.LogInformation("Creating purchase orders for customer order {CustomerOrderId} with {AllocationCount} supplier allocations", 
            customerOrderId, distributionPlan.Allocations.Count);

        // Validate the customer order exists and is in the correct state
        var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);
        if (customerOrder == null)
        {
            throw new ArgumentException($"Customer order {customerOrderId} not found", nameof(customerOrderId));
        }

        if (customerOrder.Status != OrderStatus.PlanningInProgress)
        {
            throw new InvalidOperationException($"Customer order {customerOrderId} is not in planning state. Current status: {customerOrder.Status}");
        }

        // Validate the distribution plan
        var validationResult = await _distributionAlgorithmService.ValidateDistributionAsync(distributionPlan);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Distribution plan validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        var createdPurchaseOrders = new List<PurchaseOrder>();

        try
        {
            // Create purchase orders for each supplier allocation
            foreach (var allocation in distributionPlan.Allocations)
            {
                var supplier = await _supplierRepository.GetByIdAsync(allocation.SupplierId);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier {SupplierId} not found, skipping allocation", allocation.SupplierId);
                    continue;
                }

                var purchaseOrder = await CreatePurchaseOrderForSupplierAsync(customerOrder, supplier, allocation, distributionPlan.CreatedBy);
                createdPurchaseOrders.Add(purchaseOrder);

                _logger.LogDebug("Created purchase order {PurchaseOrderNumber} for supplier {SupplierName} with {Quantity} units", 
                    purchaseOrder.PurchaseOrderNumber, supplier.Name, allocation.AllocatedQuantity);
            }

            // Update customer order status
            customerOrder.TransitionTo(OrderStatus.PurchaseOrdersCreated);
            await _customerOrderRepository.UpdateAsync(customerOrder);

            // Create audit trail
            await CreateAuditTrailAsync(customerOrderId, "PurchaseOrdersCreated", 
                $"Created {createdPurchaseOrders.Count} purchase orders", distributionPlan.CreatedBy);

            _logger.LogInformation("Successfully created {Count} purchase orders for customer order {CustomerOrderId}", 
                createdPurchaseOrders.Count, customerOrderId);

            return createdPurchaseOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase orders for customer order {CustomerOrderId}", customerOrderId);
            
            // Cleanup any partially created purchase orders
            foreach (var purchaseOrder in createdPurchaseOrders)
            {
                try
                {
                    await _purchaseOrderRepository.DeleteAsync(purchaseOrder.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up purchase order {PurchaseOrderId}", purchaseOrder.Id);
                }
            }

            throw;
        }
    }

    public async Task<DistributionSuggestion> SuggestSupplierDistributionAsync(Guid customerOrderId)
    {
        _logger.LogInformation("Generating supplier distribution suggestion for customer order {CustomerOrderId}", customerOrderId);

        var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);
        if (customerOrder == null)
        {
            throw new ArgumentException($"Customer order {customerOrderId} not found", nameof(customerOrderId));
        }

        return await _distributionAlgorithmService.GenerateDistributionSuggestionAsync(customerOrder);
    }

    public async Task<PurchaseOrder> ConfirmPurchaseOrderAsync(Guid purchaseOrderId, SupplierConfirmation confirmation)
    {
        _logger.LogInformation("Confirming purchase order {PurchaseOrderId}", purchaseOrderId);

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId);
        if (purchaseOrder == null)
        {
            throw new ArgumentException($"Purchase order {purchaseOrderId} not found", nameof(purchaseOrderId));
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.SentToSupplier)
        {
            throw new InvalidOperationException($"Purchase order {purchaseOrderId} cannot be confirmed. Current status: {purchaseOrder.Status}");
        }

        // Update purchase order status and details
        purchaseOrder.ConfirmOrder(confirmation.SupplierNotes);

        // Update purchase order items with supplier details
        foreach (var itemConfirmation in confirmation.ItemConfirmations)
        {
            var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == itemConfirmation.PurchaseOrderItemId);
            if (purchaseOrderItem != null)
            {
                if (!string.IsNullOrEmpty(itemConfirmation.PackagingDetails))
                {
                    purchaseOrderItem.SetPackagingDetails(itemConfirmation.PackagingDetails, itemConfirmation.DeliveryMethod);
                }

                if (itemConfirmation.EstimatedDeliveryDate.HasValue)
                {
                    purchaseOrderItem.SetEstimatedDeliveryDate(itemConfirmation.EstimatedDeliveryDate.Value);
                }

                if (itemConfirmation.UnitPrice.HasValue)
                {
                    purchaseOrderItem.UpdateUnitPrice(itemConfirmation.UnitPrice.Value);
                }

                if (!string.IsNullOrEmpty(itemConfirmation.SupplierNotes))
                {
                    purchaseOrderItem.AddSupplierNotes(itemConfirmation.SupplierNotes);
                }

                await _purchaseOrderItemRepository.UpdateAsync(purchaseOrderItem);
            }
        }

        // Recalculate total value
        purchaseOrder.CalculateTotalValue();
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder);

        // Create audit trail
        await CreateAuditTrailAsync(purchaseOrder.CustomerOrderId, "PurchaseOrderConfirmed", 
            $"Purchase order {purchaseOrder.PurchaseOrderNumber} confirmed by supplier", confirmation.ConfirmedBy, purchaseOrder.Id);

        // Check if all purchase orders for the customer order are confirmed
        await CheckAndUpdateCustomerOrderStatusAsync(purchaseOrder.CustomerOrderId);

        _logger.LogInformation("Purchase order {PurchaseOrderId} confirmed successfully", purchaseOrderId);
        return purchaseOrder;
    }

    public async Task<PurchaseOrder> RejectPurchaseOrderAsync(Guid purchaseOrderId, string rejectionReason)
    {
        _logger.LogInformation("Rejecting purchase order {PurchaseOrderId} with reason: {Reason}", purchaseOrderId, rejectionReason);

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId);
        if (purchaseOrder == null)
        {
            throw new ArgumentException($"Purchase order {purchaseOrderId} not found", nameof(purchaseOrderId));
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.SentToSupplier)
        {
            throw new InvalidOperationException($"Purchase order {purchaseOrderId} cannot be rejected. Current status: {purchaseOrder.Status}");
        }

        purchaseOrder.RejectOrder(rejectionReason);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder);

        // Create audit trail
        await CreateAuditTrailAsync(purchaseOrder.CustomerOrderId, "PurchaseOrderRejected", 
            $"Purchase order {purchaseOrder.PurchaseOrderNumber} rejected: {rejectionReason}", Guid.Empty, purchaseOrder.Id);

        _logger.LogInformation("Purchase order {PurchaseOrderId} rejected successfully", purchaseOrderId);
        return purchaseOrder;
    }

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(Guid supplierId, PurchaseOrderStatus? status = null)
    {
        _logger.LogDebug("Getting purchase orders for supplier {SupplierId} with status filter: {Status}", supplierId, status);

        var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
        var supplierPurchaseOrders = allPurchaseOrders.Where(po => po.SupplierId == supplierId);

        if (status.HasValue)
        {
            supplierPurchaseOrders = supplierPurchaseOrders.Where(po => po.Status == status.Value);
        }

        return supplierPurchaseOrders.OrderByDescending(po => po.CreatedAt).ToList();
    }

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersByCustomerOrderAsync(Guid customerOrderId)
    {
        _logger.LogDebug("Getting purchase orders for customer order {CustomerOrderId}", customerOrderId);

        var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
        return allPurchaseOrders
            .Where(po => po.CustomerOrderId == customerOrderId)
            .OrderByDescending(po => po.CreatedAt)
            .ToList();
    }

    public async Task<PurchaseOrderItem> UpdatePurchaseOrderItemAsync(Guid purchaseOrderItemId, PurchaseOrderItemUpdate itemDetails)
    {
        _logger.LogInformation("Updating purchase order item {PurchaseOrderItemId}", purchaseOrderItemId);

        var purchaseOrderItem = await _purchaseOrderItemRepository.GetByIdAsync(purchaseOrderItemId);
        if (purchaseOrderItem == null)
        {
            throw new ArgumentException($"Purchase order item {purchaseOrderItemId} not found", nameof(purchaseOrderItemId));
        }

        // Update item details
        if (!string.IsNullOrEmpty(itemDetails.PackagingDetails))
        {
            purchaseOrderItem.SetPackagingDetails(itemDetails.PackagingDetails, itemDetails.DeliveryMethod);
        }

        if (itemDetails.EstimatedDeliveryDate.HasValue)
        {
            purchaseOrderItem.SetEstimatedDeliveryDate(itemDetails.EstimatedDeliveryDate.Value);
        }

        if (itemDetails.UnitPrice.HasValue)
        {
            purchaseOrderItem.UpdateUnitPrice(itemDetails.UnitPrice.Value);
        }

        if (!string.IsNullOrEmpty(itemDetails.SupplierNotes))
        {
            purchaseOrderItem.AddSupplierNotes(itemDetails.SupplierNotes);
        }

        if (!string.IsNullOrEmpty(itemDetails.Specifications))
        {
            purchaseOrderItem.Specifications = itemDetails.Specifications;
        }

        await _purchaseOrderItemRepository.UpdateAsync(purchaseOrderItem);

        _logger.LogInformation("Purchase order item {PurchaseOrderItemId} updated successfully", purchaseOrderItemId);
        return purchaseOrderItem;
    }

    public async Task<DistributionValidationResult> ValidateDistributionPlanAsync(Guid customerOrderId, DistributionPlan distributionPlan)
    {
        _logger.LogDebug("Validating distribution plan for customer order {CustomerOrderId}", customerOrderId);

        var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);
        if (customerOrder == null)
        {
            var result = new DistributionValidationResult();
            result.AddError($"Customer order {customerOrderId} not found");
            return result;
        }

        // Set the customer order ID in the distribution plan if not set
        if (distributionPlan.CustomerOrderId == Guid.Empty)
        {
            distributionPlan.CustomerOrderId = customerOrderId;
        }

        return await _distributionAlgorithmService.ValidateDistributionAsync(distributionPlan);
    }

    public async Task<string> GeneratePurchaseOrderNumberAsync(Guid supplierId, Guid customerOrderId)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);

        if (supplier == null || customerOrder == null)
        {
            throw new ArgumentException("Invalid supplier or customer order ID");
        }

        // Generate a unique purchase order number
        // Format: PO-{CustomerOrderNumber}-{SupplierCode}-{Sequence}
        var supplierCode = supplier.Name.Length >= 3 ? supplier.Name.Substring(0, 3).ToUpper() : supplier.Name.ToUpper();
        var existingPOCount = await GetPurchaseOrderCountForCustomerOrderAsync(customerOrderId);
        var sequence = (existingPOCount + 1).ToString("D3");

        return $"PO-{customerOrder.OrderNumber}-{supplierCode}-{sequence}";
    }

    private async Task<PurchaseOrder> CreatePurchaseOrderForSupplierAsync(
        CustomerOrder customerOrder, 
        Supplier supplier, 
        SupplierAllocation allocation, 
        Guid createdBy)
    {
        var purchaseOrderNumber = await GeneratePurchaseOrderNumberAsync(supplier.Id, customerOrder.Id);

        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = purchaseOrderNumber,
            CustomerOrderId = customerOrder.Id,
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Created,
            RequiredDeliveryDate = customerOrder.RequestedDeliveryDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // Create purchase order items based on customer order items and allocation
        var remainingAllocation = allocation.AllocatedQuantity;
        
        foreach (var orderItem in customerOrder.Items.OrderBy(i => i.Quantity))
        {
            if (remainingAllocation <= 0) break;

            var allocationForThisItem = Math.Min(remainingAllocation, orderItem.Quantity);
            
            var purchaseOrderItem = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = purchaseOrder.Id,
                OrderItemId = orderItem.Id,
                ProductCode = orderItem.ProductCode,
                Description = orderItem.Description,
                AllocatedQuantity = allocationForThisItem,
                Unit = orderItem.Unit,
                UnitPrice = orderItem.UnitPrice,
                Specifications = orderItem.Specifications,
                CreatedAt = DateTime.UtcNow
            };

            purchaseOrderItem.ValidatePurchaseOrderItem();
            purchaseOrder.Items.Add(purchaseOrderItem);
            
            remainingAllocation -= allocationForThisItem;
        }

        // Validate the purchase order
        purchaseOrder.ValidatePurchaseOrder();
        purchaseOrder.CalculateTotalValue();

        // Save to database
        await _purchaseOrderRepository.AddAsync(purchaseOrder);

        // Transition to sent to supplier status
        purchaseOrder.TransitionTo(PurchaseOrderStatus.SentToSupplier);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder);

        return purchaseOrder;
    }

    private async Task CheckAndUpdateCustomerOrderStatusAsync(Guid customerOrderId)
    {
        var purchaseOrders = await GetPurchaseOrdersByCustomerOrderAsync(customerOrderId);
        
        if (purchaseOrders.Any() && purchaseOrders.All(po => po.IsConfirmed))
        {
            var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);
            if (customerOrder != null && customerOrder.Status == OrderStatus.AwaitingSupplierConfirmation)
            {
                customerOrder.TransitionTo(OrderStatus.InProduction);
                await _customerOrderRepository.UpdateAsync(customerOrder);

                await CreateAuditTrailAsync(customerOrderId, "CustomerOrderStatusUpdated", 
                    "All purchase orders confirmed, moved to production", Guid.Empty);
            }
        }
        else if (purchaseOrders.Any() && purchaseOrders.Any(po => po.IsConfirmed))
        {
            var customerOrder = await _customerOrderRepository.GetByIdAsync(customerOrderId);
            if (customerOrder != null && customerOrder.Status == OrderStatus.PurchaseOrdersCreated)
            {
                customerOrder.TransitionTo(OrderStatus.AwaitingSupplierConfirmation);
                await _customerOrderRepository.UpdateAsync(customerOrder);
            }
        }
    }

    private async Task<int> GetPurchaseOrderCountForCustomerOrderAsync(Guid customerOrderId)
    {
        var purchaseOrders = await GetPurchaseOrdersByCustomerOrderAsync(customerOrderId);
        return purchaseOrders.Count;
    }

    private async Task CreateAuditTrailAsync(Guid customerOrderId, string action, string details, Guid performedBy, Guid? purchaseOrderId = null)
    {
        // This would typically save to an audit table
        // For now, we'll just log it
        _logger.LogInformation("Audit: {Action} for customer order {CustomerOrderId} by {PerformedBy}. Details: {Details}", 
            action, customerOrderId, performedBy, details);
        
        await Task.CompletedTask; // Placeholder for actual audit implementation
    }
}