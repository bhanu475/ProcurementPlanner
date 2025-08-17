using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

public interface IProcurementPlanningService
{
    /// <summary>
    /// Creates purchase orders from a customer order using the provided distribution plan
    /// </summary>
    /// <param name="customerOrderId">The customer order to convert</param>
    /// <param name="distributionPlan">The distribution plan specifying supplier allocations</param>
    /// <returns>List of created purchase orders</returns>
    Task<List<PurchaseOrder>> CreatePurchaseOrdersAsync(Guid customerOrderId, DistributionPlan distributionPlan);

    /// <summary>
    /// Generates a distribution suggestion for a customer order
    /// </summary>
    /// <param name="customerOrderId">The customer order to generate suggestions for</param>
    /// <returns>Distribution suggestion with supplier allocations</returns>
    Task<DistributionSuggestion> SuggestSupplierDistributionAsync(Guid customerOrderId);

    /// <summary>
    /// Confirms a purchase order from supplier side
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order to confirm</param>
    /// <param name="confirmation">Supplier confirmation details</param>
    /// <returns>Updated purchase order</returns>
    Task<PurchaseOrder> ConfirmPurchaseOrderAsync(Guid purchaseOrderId, SupplierConfirmation confirmation);

    /// <summary>
    /// Rejects a purchase order from supplier side
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order to reject</param>
    /// <param name="rejectionReason">Reason for rejection</param>
    /// <returns>Updated purchase order</returns>
    Task<PurchaseOrder> RejectPurchaseOrderAsync(Guid purchaseOrderId, string rejectionReason);

    /// <summary>
    /// Gets all purchase orders for a specific supplier
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of purchase orders for the supplier</returns>
    Task<List<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(Guid supplierId, PurchaseOrderStatus? status = null);

    /// <summary>
    /// Gets all purchase orders for a specific customer order
    /// </summary>
    /// <param name="customerOrderId">The customer order ID</param>
    /// <returns>List of purchase orders for the customer order</returns>
    Task<List<PurchaseOrder>> GetPurchaseOrdersByCustomerOrderAsync(Guid customerOrderId);

    /// <summary>
    /// Updates purchase order item details (packaging, delivery estimates, etc.)
    /// </summary>
    /// <param name="purchaseOrderItemId">The purchase order item to update</param>
    /// <param name="itemDetails">Updated item details</param>
    /// <returns>Updated purchase order item</returns>
    Task<PurchaseOrderItem> UpdatePurchaseOrderItemAsync(Guid purchaseOrderItemId, PurchaseOrderItemUpdate itemDetails);

    /// <summary>
    /// Validates that a distribution plan can be executed
    /// </summary>
    /// <param name="customerOrderId">The customer order ID</param>
    /// <param name="distributionPlan">The distribution plan to validate</param>
    /// <returns>Validation result</returns>
    Task<DistributionValidationResult> ValidateDistributionPlanAsync(Guid customerOrderId, DistributionPlan distributionPlan);

    /// <summary>
    /// Generates purchase order number
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="customerOrderId">The customer order ID</param>
    /// <returns>Generated purchase order number</returns>
    Task<string> GeneratePurchaseOrderNumberAsync(Guid supplierId, Guid customerOrderId);
}