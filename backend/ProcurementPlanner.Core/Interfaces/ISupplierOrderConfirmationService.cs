using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Interfaces;

/// <summary>
/// Service for handling supplier order confirmation workflows
/// </summary>
public interface ISupplierOrderConfirmationService
{
    /// <summary>
    /// Gets all purchase orders assigned to a specific supplier
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of purchase orders for the supplier</returns>
    Task<List<PurchaseOrder>> GetSupplierPurchaseOrdersAsync(Guid supplierId, PurchaseOrderStatus? status = null);

    /// <summary>
    /// Gets a specific purchase order for a supplier with validation
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID</param>
    /// <param name="supplierId">The supplier ID for authorization</param>
    /// <returns>Purchase order if supplier is authorized</returns>
    Task<PurchaseOrder> GetSupplierPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId);

    /// <summary>
    /// Confirms a purchase order with packaging and delivery details
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order to confirm</param>
    /// <param name="supplierId">The supplier ID for authorization</param>
    /// <param name="confirmation">Confirmation details including packaging and delivery info</param>
    /// <returns>Updated purchase order</returns>
    Task<PurchaseOrder> ConfirmPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId, SupplierOrderConfirmation confirmation);

    /// <summary>
    /// Rejects a purchase order with reason
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order to reject</param>
    /// <param name="supplierId">The supplier ID for authorization</param>
    /// <param name="rejectionReason">Reason for rejection</param>
    /// <returns>Updated purchase order</returns>
    Task<PurchaseOrder> RejectPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId, string rejectionReason);

    /// <summary>
    /// Updates packaging and delivery details for purchase order items
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID</param>
    /// <param name="supplierId">The supplier ID for authorization</param>
    /// <param name="itemUpdates">List of item updates</param>
    /// <returns>Updated purchase order</returns>
    Task<PurchaseOrder> UpdatePurchaseOrderItemsAsync(Guid purchaseOrderId, Guid supplierId, List<SupplierItemUpdate> itemUpdates);

    /// <summary>
    /// Validates delivery dates against customer requirements
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID</param>
    /// <param name="estimatedDeliveryDates">Dictionary of item ID to estimated delivery date</param>
    /// <returns>Validation result</returns>
    Task<DeliveryDateValidationResult> ValidateDeliveryDatesAsync(Guid purchaseOrderId, Dictionary<Guid, DateTime> estimatedDeliveryDates);

    /// <summary>
    /// Gets supplier dashboard summary with order counts and metrics
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <returns>Dashboard summary</returns>
    Task<SupplierDashboardSummary> GetSupplierDashboardSummaryAsync(Guid supplierId);

    /// <summary>
    /// Sends notification to supplier about new purchase order
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID</param>
    /// <returns>True if notification sent successfully</returns>
    Task<bool> NotifySupplierOfNewOrderAsync(Guid purchaseOrderId);

    /// <summary>
    /// Gets supplier order history with filtering options
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="filter">Filter options</param>
    /// <returns>Paginated list of historical orders</returns>
    Task<PagedResult<PurchaseOrder>> GetSupplierOrderHistoryAsync(Guid supplierId, SupplierOrderHistoryFilter filter);
}