using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using ProcurementPlanner.Infrastructure.Data;

namespace ProcurementPlanner.Infrastructure.Services;

public class SupplierOrderConfirmationService : ISupplierOrderConfirmationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupplierOrderConfirmationService> _logger;

    public SupplierOrderConfirmationService(
        ApplicationDbContext context,
        ILogger<SupplierOrderConfirmationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PurchaseOrder>> GetSupplierPurchaseOrdersAsync(Guid supplierId, PurchaseOrderStatus? status = null)
    {
        try
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Items)
                    .ThenInclude(poi => poi.OrderItem)
                .Include(po => po.CustomerOrder)
                .Include(po => po.Supplier)
                .Where(po => po.SupplierId == supplierId);

            if (status.HasValue)
            {
                query = query.Where(po => po.Status == status.Value);
            }

            var orders = await query
                .OrderByDescending(po => po.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} purchase orders for supplier {SupplierId}", 
                orders.Count, supplierId);

            return orders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase orders for supplier {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<PurchaseOrder> GetSupplierPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Items)
                    .ThenInclude(poi => poi.OrderItem)
                .Include(po => po.CustomerOrder)
                .Include(po => po.Supplier)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && po.SupplierId == supplierId);

            if (purchaseOrder == null)
            {
                throw new UnauthorizedAccessException($"Purchase order {purchaseOrderId} not found or not authorized for supplier {supplierId}");
            }

            _logger.LogInformation("Retrieved purchase order {PurchaseOrderId} for supplier {SupplierId}", 
                purchaseOrderId, supplierId);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order {PurchaseOrderId} for supplier {SupplierId}", 
                purchaseOrderId, supplierId);
            throw;
        }
    }

    public async Task<PurchaseOrder> ConfirmPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId, SupplierOrderConfirmation confirmation)
    {
        var transaction = _context.Database.ProviderName?.Contains("InMemory") == true ? null : await _context.Database.BeginTransactionAsync();
        
        try
        {
            var purchaseOrder = await GetSupplierPurchaseOrderAsync(purchaseOrderId, supplierId);

            // Validate that order can be confirmed
            if (!purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Confirmed))
            {
                throw new InvalidOperationException($"Purchase order {purchaseOrderId} cannot be confirmed from status {purchaseOrder.Status}");
            }

            // Validate delivery dates if provided
            if (confirmation.ItemUpdates.Any(u => u.EstimatedDeliveryDate.HasValue))
            {
                var deliveryDates = confirmation.ItemUpdates
                    .Where(u => u.EstimatedDeliveryDate.HasValue)
                    .ToDictionary(u => u.PurchaseOrderItemId, u => u.EstimatedDeliveryDate!.Value);

                var validationResult = await ValidateDeliveryDatesAsync(purchaseOrderId, deliveryDates);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Delivery date validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                }
            }

            // Update purchase order status
            purchaseOrder.ConfirmOrder(confirmation.SupplierNotes);

            // Update item details
            foreach (var itemUpdate in confirmation.ItemUpdates)
            {
                var item = purchaseOrder.Items.FirstOrDefault(i => i.Id == itemUpdate.PurchaseOrderItemId);
                if (item != null)
                {
                    if (!string.IsNullOrWhiteSpace(itemUpdate.PackagingDetails))
                    {
                        item.SetPackagingDetails(itemUpdate.PackagingDetails, itemUpdate.DeliveryMethod);
                    }

                    if (itemUpdate.EstimatedDeliveryDate.HasValue)
                    {
                        item.SetEstimatedDeliveryDate(itemUpdate.EstimatedDeliveryDate.Value);
                    }

                    if (itemUpdate.UnitPrice.HasValue)
                    {
                        item.UpdateUnitPrice(itemUpdate.UnitPrice.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(itemUpdate.SupplierNotes))
                    {
                        item.AddSupplierNotes(itemUpdate.SupplierNotes);
                    }
                }
            }

            // Recalculate total value
            purchaseOrder.CalculateTotalValue();

            await _context.SaveChangesAsync();
            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Purchase order {PurchaseOrderId} confirmed by supplier {SupplierId}", 
                purchaseOrderId, supplierId);

            // Send notification to planners (would be implemented with actual notification service)
            await NotifyPlannersOfOrderConfirmationAsync(purchaseOrder);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            _logger.LogError(ex, "Error confirming purchase order {PurchaseOrderId} for supplier {SupplierId}", 
                purchaseOrderId, supplierId);
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<PurchaseOrder> RejectPurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId, string rejectionReason)
    {
        var transaction = _context.Database.ProviderName?.Contains("InMemory") == true ? null : await _context.Database.BeginTransactionAsync();
        
        try
        {
            var purchaseOrder = await GetSupplierPurchaseOrderAsync(purchaseOrderId, supplierId);

            // Validate that order can be rejected
            if (!purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Rejected))
            {
                throw new InvalidOperationException($"Purchase order {purchaseOrderId} cannot be rejected from status {purchaseOrder.Status}");
            }

            // Reject the order
            purchaseOrder.RejectOrder(rejectionReason);

            await _context.SaveChangesAsync();
            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Purchase order {PurchaseOrderId} rejected by supplier {SupplierId} with reason: {Reason}", 
                purchaseOrderId, supplierId, rejectionReason);

            // Send notification to planners
            await NotifyPlannersOfOrderRejectionAsync(purchaseOrder, rejectionReason);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            _logger.LogError(ex, "Error rejecting purchase order {PurchaseOrderId} for supplier {SupplierId}", 
                purchaseOrderId, supplierId);
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<PurchaseOrder> UpdatePurchaseOrderItemsAsync(Guid purchaseOrderId, Guid supplierId, List<SupplierItemUpdate> itemUpdates)
    {
        var transaction = _context.Database.ProviderName?.Contains("InMemory") == true ? null : await _context.Database.BeginTransactionAsync();
        
        try
        {
            var purchaseOrder = await GetSupplierPurchaseOrderAsync(purchaseOrderId, supplierId);

            // Validate delivery dates if provided
            var deliveryDates = itemUpdates
                .Where(u => u.EstimatedDeliveryDate.HasValue)
                .ToDictionary(u => u.PurchaseOrderItemId, u => u.EstimatedDeliveryDate!.Value);

            if (deliveryDates.Any())
            {
                var validationResult = await ValidateDeliveryDatesAsync(purchaseOrderId, deliveryDates);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Delivery date validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                }
            }

            // Update item details
            foreach (var itemUpdate in itemUpdates)
            {
                var item = purchaseOrder.Items.FirstOrDefault(i => i.Id == itemUpdate.PurchaseOrderItemId);
                if (item != null)
                {
                    if (!string.IsNullOrWhiteSpace(itemUpdate.PackagingDetails))
                    {
                        item.SetPackagingDetails(itemUpdate.PackagingDetails, itemUpdate.DeliveryMethod);
                    }

                    if (itemUpdate.EstimatedDeliveryDate.HasValue)
                    {
                        item.SetEstimatedDeliveryDate(itemUpdate.EstimatedDeliveryDate.Value);
                    }

                    if (itemUpdate.UnitPrice.HasValue)
                    {
                        item.UpdateUnitPrice(itemUpdate.UnitPrice.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(itemUpdate.SupplierNotes))
                    {
                        item.AddSupplierNotes(itemUpdate.SupplierNotes);
                    }
                }
            }

            // Recalculate total value
            purchaseOrder.CalculateTotalValue();

            await _context.SaveChangesAsync();
            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Updated {Count} items for purchase order {PurchaseOrderId} by supplier {SupplierId}", 
                itemUpdates.Count, purchaseOrderId, supplierId);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating purchase order items for {PurchaseOrderId} by supplier {SupplierId}", 
                purchaseOrderId, supplierId);
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<DeliveryDateValidationResult> ValidateDeliveryDatesAsync(Guid purchaseOrderId, Dictionary<Guid, DateTime> estimatedDeliveryDates)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.CustomerOrder)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                throw new ArgumentException($"Purchase order {purchaseOrderId} not found");
            }

            var result = new DeliveryDateValidationResult
            {
                CustomerRequiredDate = purchaseOrder.RequiredDeliveryDate
            };

            foreach (var (itemId, estimatedDate) in estimatedDeliveryDates)
            {
                // Check if estimated date is in the past
                if (estimatedDate <= DateTime.UtcNow.Date)
                {
                    result.AddError(itemId, "Estimated delivery date cannot be in the past");
                    continue;
                }

                // Check if estimated date is after required delivery date
                if (estimatedDate > purchaseOrder.RequiredDeliveryDate)
                {
                    result.AddError(itemId, $"Estimated delivery date ({estimatedDate:yyyy-MM-dd}) is after required delivery date ({purchaseOrder.RequiredDeliveryDate:yyyy-MM-dd})");
                    continue;
                }

                // Add warning if delivery is very close to required date (less than 2 days buffer)
                var daysDifference = (purchaseOrder.RequiredDeliveryDate - estimatedDate).Days;
                if (daysDifference < 2)
                {
                    result.AddWarning(itemId, $"Estimated delivery date leaves only {daysDifference} day(s) buffer before required delivery");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating delivery dates for purchase order {PurchaseOrderId}", purchaseOrderId);
            throw;
        }
    }

    public async Task<SupplierDashboardSummary> GetSupplierDashboardSummaryAsync(Guid supplierId)
    {
        try
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Performance)
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            if (supplier == null)
            {
                throw new ArgumentException($"Supplier {supplierId} not found");
            }

            var orders = await _context.PurchaseOrders
                .Include(po => po.Items)
                .Where(po => po.SupplierId == supplierId)
                .ToListAsync();

            var pendingOrders = orders.Where(po => po.Status == PurchaseOrderStatus.SentToSupplier).ToList();
            var confirmedOrders = orders.Where(po => po.Status == PurchaseOrderStatus.Confirmed).ToList();
            var inProductionOrders = orders.Where(po => po.Status == PurchaseOrderStatus.InProduction).ToList();
            var overdueOrders = orders.Where(po => po.IsOverdue).ToList();

            var recentOrders = orders
                .OrderByDescending(po => po.CreatedAt)
                .Take(5)
                .ToList();

            var upcomingDeliveries = orders
                .Where(po => po.Status == PurchaseOrderStatus.Confirmed || po.Status == PurchaseOrderStatus.InProduction)
                .Where(po => po.RequiredDeliveryDate >= DateTime.UtcNow.Date)
                .OrderBy(po => po.RequiredDeliveryDate)
                .Take(5)
                .ToList();

            var summary = new SupplierDashboardSummary
            {
                SupplierId = supplierId,
                SupplierName = supplier.Name,
                PendingOrdersCount = pendingOrders.Count,
                ConfirmedOrdersCount = confirmedOrders.Count,
                InProductionOrdersCount = inProductionOrders.Count,
                OverdueOrdersCount = overdueOrders.Count,
                TotalPendingValue = pendingOrders.Sum(po => po.TotalValue ?? 0),
                TotalConfirmedValue = confirmedOrders.Sum(po => po.TotalValue ?? 0),
                RecentOrders = recentOrders,
                UpcomingDeliveries = upcomingDeliveries,
                Performance = new SupplierPerformanceSnapshot
                {
                    OnTimeDeliveryRate = supplier.Performance?.OnTimeDeliveryRate ?? 0,
                    QualityScore = supplier.Performance?.QualityScore ?? 0,
                    TotalOrdersCompleted = supplier.Performance?.TotalOrdersCompleted ?? 0,
                    OrdersCompletedThisMonth = orders.Count(po => po.Status == PurchaseOrderStatus.Delivered && 
                                                               po.DeliveredAt?.Month == DateTime.UtcNow.Month &&
                                                               po.DeliveredAt?.Year == DateTime.UtcNow.Year),
                    AverageOrderValue = orders.Where(po => po.TotalValue.HasValue).Any() 
                        ? orders.Where(po => po.TotalValue.HasValue).Average(po => po.TotalValue!.Value) 
                        : 0,
                    LastDelivery = orders.Where(po => po.DeliveredAt.HasValue).Max(po => po.DeliveredAt) ?? DateTime.MinValue
                }
            };

            _logger.LogInformation("Generated dashboard summary for supplier {SupplierId}", supplierId);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard summary for supplier {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<bool> NotifySupplierOfNewOrderAsync(Guid purchaseOrderId)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.CustomerOrder)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found for notification", purchaseOrderId);
                return false;
            }

            // Create notification record
            var notification = new SupplierNotification
            {
                SupplierId = purchaseOrder.SupplierId,
                PurchaseOrderId = purchaseOrderId,
                Subject = $"New Purchase Order: {purchaseOrder.PurchaseOrderNumber}",
                Message = $"You have received a new purchase order {purchaseOrder.PurchaseOrderNumber} " +
                         $"for customer order {purchaseOrder.CustomerOrder.OrderNumber}. " +
                         $"Please review and confirm by {purchaseOrder.RequiredDeliveryDate:yyyy-MM-dd}.",
                Type = NotificationType.NewPurchaseOrder
            };

            // In a real implementation, this would send email/SMS
            // For now, we'll just log the notification
            _logger.LogInformation("Notification sent to supplier {SupplierId} for new purchase order {PurchaseOrderId}", 
                purchaseOrder.SupplierId, purchaseOrderId);

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for purchase order {PurchaseOrderId}", purchaseOrderId);
            return false;
        }
    }

    public async Task<PagedResult<PurchaseOrder>> GetSupplierOrderHistoryAsync(Guid supplierId, SupplierOrderHistoryFilter filter)
    {
        try
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Items)
                .Include(po => po.CustomerOrder)
                .Where(po => po.SupplierId == supplierId);

            // Apply filters
            if (filter.StartDate.HasValue)
            {
                query = query.Where(po => po.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(po => po.CreatedAt <= filter.EndDate.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(po => po.Status == filter.Status.Value);
            }

            if (filter.ProductType.HasValue)
            {
                query = query.Where(po => po.CustomerOrder.ProductType == filter.ProductType.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                query = query.Where(po => po.CustomerOrder.CustomerName.Contains(filter.CustomerName));
            }

            if (filter.MinValue.HasValue)
            {
                query = query.Where(po => po.TotalValue >= filter.MinValue.Value);
            }

            if (filter.MaxValue.HasValue)
            {
                query = query.Where(po => po.TotalValue <= filter.MaxValue.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "createdat" => filter.SortDescending ? query.OrderByDescending(po => po.CreatedAt) : query.OrderBy(po => po.CreatedAt),
                "status" => filter.SortDescending ? query.OrderByDescending(po => po.Status) : query.OrderBy(po => po.Status),
                "totalvalue" => filter.SortDescending ? query.OrderByDescending(po => po.TotalValue) : query.OrderBy(po => po.TotalValue),
                "requireddeliverydate" => filter.SortDescending ? query.OrderByDescending(po => po.RequiredDeliveryDate) : query.OrderBy(po => po.RequiredDeliveryDate),
                _ => filter.SortDescending ? query.OrderByDescending(po => po.CreatedAt) : query.OrderBy(po => po.CreatedAt)
            };

            // Apply pagination
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} orders from history for supplier {SupplierId}", 
                items.Count, supplierId);

            return new PagedResult<PurchaseOrder>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for supplier {SupplierId}", supplierId);
            throw;
        }
    }

    private async Task NotifyPlannersOfOrderConfirmationAsync(PurchaseOrder purchaseOrder)
    {
        // In a real implementation, this would send notifications to LMR planners
        _logger.LogInformation("Notifying planners of order confirmation for purchase order {PurchaseOrderId}", 
            purchaseOrder.Id);
        await Task.CompletedTask;
    }

    private async Task NotifyPlannersOfOrderRejectionAsync(PurchaseOrder purchaseOrder, string rejectionReason)
    {
        // In a real implementation, this would send notifications to LMR planners
        _logger.LogInformation("Notifying planners of order rejection for purchase order {PurchaseOrderId}: {Reason}", 
            purchaseOrder.Id, rejectionReason);
        await Task.CompletedTask;
    }
}