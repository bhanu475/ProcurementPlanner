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
    }
}