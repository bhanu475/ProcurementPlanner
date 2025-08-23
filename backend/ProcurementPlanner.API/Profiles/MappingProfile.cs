using AutoMapper;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Order Management Mappings
        CreateMap<CreateOrderDto, CreateOrderRequest>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()); // Will be set from JWT token

        CreateMap<CreateOrderItemDto, CreateOrderItemRequest>();

        CreateMap<UpdateOrderDto, UpdateOrderRequest>();
        CreateMap<UpdateOrderItemDto, UpdateOrderItemRequest>();

        CreateMap<OrderFilterDto, OrderFilterRequest>();
        CreateMap<DashboardFilterDto, DashboardFilterRequest>();

        CreateMap<CustomerOrder, OrderResponseDto>()
            .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.TotalQuantity))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => src.Items.Sum(i => i.TotalPrice)))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue));

        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        CreateMap<PagedResult<CustomerOrder>, PagedOrderResponseDto>()
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage));

        CreateMap<OrderDashboardSummary, OrderDashboardResponseDto>()
            .ForMember(dest => dest.StatusCounts, opt => opt.MapFrom(src => 
                src.StatusCounts.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)))
            .ForMember(dest => dest.ProductTypeCounts, opt => opt.MapFrom(src => 
                src.ProductTypeCounts.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)));

        CreateMap<OrdersByDeliveryDate, OrdersByDeliveryDateDto>();
        CreateMap<OrdersByCustomer, OrdersByCustomerDto>();

        // Supplier Management Mappings
        CreateMap<CreateSupplierRequest, Supplier>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore())
            .ForMember(dest => dest.Performance, opt => opt.Ignore());

        CreateMap<UpdateSupplierRequest, Supplier>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Capabilities, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore())
            .ForMember(dest => dest.Performance, opt => opt.Ignore());

        CreateMap<CreateSupplierCapabilityRequest, SupplierCapability>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
            .ForMember(dest => dest.Supplier, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<Supplier, SupplierResponse>();

        CreateMap<SupplierCapability, SupplierCapabilityResponse>()
            .ForMember(dest => dest.AvailableCapacity, opt => opt.MapFrom(src => src.AvailableCapacity))
            .ForMember(dest => dest.CapacityUtilizationRate, opt => opt.MapFrom(src => src.CapacityUtilizationRate))
            .ForMember(dest => dest.IsOverCommitted, opt => opt.MapFrom(src => src.IsOverCommitted));

        CreateMap<SupplierPerformanceMetrics, SupplierPerformanceResponse>()
            .ForMember(dest => dest.OverallPerformanceScore, opt => opt.MapFrom(src => src.OverallPerformanceScore))
            .ForMember(dest => dest.CancellationRate, opt => opt.MapFrom(src => src.CancellationRate))
            .ForMember(dest => dest.IsReliableSupplier, opt => opt.MapFrom(src => src.IsReliableSupplier))
            .ForMember(dest => dest.IsPreferredSupplier, opt => opt.MapFrom(src => src.IsPreferredSupplier));

        // Procurement Planning Mappings
        CreateMap<DistributionPlanDto, DistributionPlan>()
            .ForMember(dest => dest.CustomerOrderId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        CreateMap<SupplierAllocationDto, SupplierAllocation>()
            .ForMember(dest => dest.SupplierName, opt => opt.Ignore())
            .ForMember(dest => dest.AllocationPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.AvailableCapacity, opt => opt.Ignore())
            .ForMember(dest => dest.PerformanceScore, opt => opt.Ignore())
            .ForMember(dest => dest.QualityRating, opt => opt.Ignore())
            .ForMember(dest => dest.OnTimeDeliveryRate, opt => opt.Ignore());

        CreateMap<SupplierConfirmationRequest, SupplierConfirmation>()
            .ForMember(dest => dest.ConfirmedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ConfirmedBy, opt => opt.Ignore());

        CreateMap<PurchaseOrderItemConfirmationDto, PurchaseOrderItemConfirmation>();

        CreateMap<PurchaseOrderItemUpdateRequest, PurchaseOrderItemUpdate>();

        CreateMap<DistributionSuggestion, DistributionSuggestionResponse>()
            .ForMember(dest => dest.IsFullyAllocated, opt => opt.MapFrom(src => src.IsFullyAllocated))
            .ForMember(dest => dest.UnallocatedQuantity, opt => opt.MapFrom(src => src.UnallocatedQuantity));

        CreateMap<SupplierAllocation, SupplierAllocationResponse>();

        CreateMap<PurchaseOrder, PurchaseOrderResponse>()
            .ForMember(dest => dest.CustomerOrderNumber, opt => opt.MapFrom(src => src.CustomerOrder.OrderNumber))
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.Name))
            .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.TotalQuantity))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue));

        CreateMap<PurchaseOrderItem, PurchaseOrderItemResponse>()
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        CreateMap<DistributionValidationResult, DistributionValidationResponse>();

        CreateMap<SupplierCapacityValidation, SupplierCapacityValidationResponse>();

        // Supplier Portal Mappings
        CreateMap<SupplierOrderConfirmationDto, SupplierOrderConfirmation>();
        CreateMap<SupplierItemUpdateDto, SupplierItemUpdate>();
        
        CreateMap<SupplierDashboardSummary, SupplierDashboardDto>();
        CreateMap<SupplierPerformanceSnapshot, SupplierPerformanceDto>();
        
        CreateMap<PurchaseOrder, PurchaseOrderSummaryDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustomerOrder.CustomerName))
            .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.TotalQuantity))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue))
            .ForMember(dest => dest.DaysUntilDelivery, opt => opt.MapFrom(src => src.DaysUntilDelivery));

        CreateMap<PurchaseOrder, PurchaseOrderDetailDto>()
            .ForMember(dest => dest.CustomerOrderNumber, opt => opt.MapFrom(src => src.CustomerOrder.OrderNumber))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustomerOrder.CustomerName))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerOrder.CustomerId))
            .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.CustomerOrder.ProductType))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue))
            .ForMember(dest => dest.DaysUntilDelivery, opt => opt.MapFrom(src => src.DaysUntilDelivery));

        CreateMap<PurchaseOrderItem, PurchaseOrderItemDetailDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        CreateMap<SupplierOrderHistoryFilterDto, SupplierOrderHistoryFilter>();

        CreateMap<DeliveryDateValidationResult, DeliveryDateValidationDto>();
        CreateMap<DeliveryDateValidationError, ValidationErrorDto>();
        CreateMap<DeliveryDateValidationWarning, ValidationWarningDto>();
    }
}