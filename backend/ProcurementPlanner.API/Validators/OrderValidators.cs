using FluentValidation;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required")
            .MaximumLength(50).WithMessage("Customer ID cannot exceed 50 characters");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters");

        RuleFor(x => x.ProductType)
            .IsInEnum().WithMessage("Invalid product type");

        RuleFor(x => x.RequestedDeliveryDate)
            .GreaterThan(DateTime.UtcNow.Date).WithMessage("Delivery date must be in the future");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items.Count <= 100).WithMessage("Order cannot contain more than 100 items");

        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code is required")
            .MaximumLength(50).WithMessage("Product code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters");

        RuleFor(x => x.Specifications)
            .MaximumLength(1000).WithMessage("Specifications cannot exceed 1000 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative")
            .When(x => x.UnitPrice.HasValue);
    }
}

public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerName));

        RuleFor(x => x.RequestedDeliveryDate)
            .GreaterThan(DateTime.UtcNow.Date).WithMessage("Delivery date must be in the future")
            .When(x => x.RequestedDeliveryDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");

        RuleFor(x => x.Items)
            .Must(items => items == null || items.Count <= 100).WithMessage("Order cannot contain more than 100 items");

        RuleForEach(x => x.Items).SetValidator(new UpdateOrderItemDtoValidator())
            .When(x => x.Items != null);
    }
}

public class UpdateOrderItemDtoValidator : AbstractValidator<UpdateOrderItemDto>
{
    public UpdateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code is required")
            .MaximumLength(50).WithMessage("Product code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters");

        RuleFor(x => x.Specifications)
            .MaximumLength(1000).WithMessage("Specifications cannot exceed 1000 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative")
            .When(x => x.UnitPrice.HasValue);
    }
}

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status");
    }
}

public class OrderFilterDtoValidator : AbstractValidator<OrderFilterDto>
{
    public OrderFilterDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.CustomerId)
            .MaximumLength(50).WithMessage("Customer ID cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerId));

        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerName));

        RuleFor(x => x.ProductType)
            .IsInEnum().WithMessage("Invalid product type")
            .When(x => x.ProductType.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.OrderNumber)
            .MaximumLength(50).WithMessage("Order number cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.OrderNumber));

        RuleFor(x => x.DeliveryDateFrom)
            .LessThanOrEqualTo(x => x.DeliveryDateTo)
            .WithMessage("Delivery date from must be less than or equal to delivery date to")
            .When(x => x.DeliveryDateFrom.HasValue && x.DeliveryDateTo.HasValue);

        RuleFor(x => x.CreatedDateFrom)
            .LessThanOrEqualTo(x => x.CreatedDateTo)
            .WithMessage("Created date from must be less than or equal to created date to")
            .When(x => x.CreatedDateFrom.HasValue && x.CreatedDateTo.HasValue);

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || 
                           new[] { "CreatedAt", "OrderNumber", "CustomerName", "DeliveryDate", "Status" }
                           .Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid sort field");
    }
}

public class DashboardFilterDtoValidator : AbstractValidator<DashboardFilterDto>
{
    public DashboardFilterDtoValidator()
    {
        RuleFor(x => x.ProductType)
            .IsInEnum().WithMessage("Invalid product type")
            .When(x => x.ProductType.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.CustomerId)
            .MaximumLength(50).WithMessage("Customer ID cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerId));

        RuleFor(x => x.DeliveryDateFrom)
            .LessThanOrEqualTo(x => x.DeliveryDateTo)
            .WithMessage("Delivery date from must be less than or equal to delivery date to")
            .When(x => x.DeliveryDateFrom.HasValue && x.DeliveryDateTo.HasValue);
    }
}