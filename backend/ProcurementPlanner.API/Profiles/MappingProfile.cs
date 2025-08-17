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
    }
}