using FluentValidation;
using ProcurementPlanner.API.Models;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Validators;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Supplier name is required")
            .MaximumLength(200)
            .WithMessage("Supplier name cannot exceed 200 characters");

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required")
            .EmailAddress()
            .WithMessage("Contact email must be a valid email address")
            .MaximumLength(255)
            .WithMessage("Contact email cannot exceed 255 characters");

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone is required")
            .MaximumLength(20)
            .WithMessage("Contact phone cannot exceed 20 characters")
            .Matches(@"^[\d\s\-\(\)\+]+$")
            .WithMessage("Contact phone must contain only numbers, spaces, hyphens, parentheses, and plus signs");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MaximumLength(500)
            .WithMessage("Address cannot exceed 500 characters");

        RuleFor(x => x.ContactPersonName)
            .MaximumLength(100)
            .WithMessage("Contact person name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactPersonName));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Capabilities)
            .NotEmpty()
            .WithMessage("At least one capability must be specified");

        RuleForEach(x => x.Capabilities)
            .SetValidator(new CreateSupplierCapabilityRequestValidator());
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Supplier name is required")
            .MaximumLength(200)
            .WithMessage("Supplier name cannot exceed 200 characters");

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required")
            .EmailAddress()
            .WithMessage("Contact email must be a valid email address")
            .MaximumLength(255)
            .WithMessage("Contact email cannot exceed 255 characters");

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone is required")
            .MaximumLength(20)
            .WithMessage("Contact phone cannot exceed 20 characters")
            .Matches(@"^[\d\s\-\(\)\+]+$")
            .WithMessage("Contact phone must contain only numbers, spaces, hyphens, parentheses, and plus signs");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MaximumLength(500)
            .WithMessage("Address cannot exceed 500 characters");

        RuleFor(x => x.ContactPersonName)
            .MaximumLength(100)
            .WithMessage("Contact person name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactPersonName));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class CreateSupplierCapabilityRequestValidator : AbstractValidator<CreateSupplierCapabilityRequest>
{
    public CreateSupplierCapabilityRequestValidator()
    {
        RuleFor(x => x.ProductType)
            .IsInEnum()
            .WithMessage("Product type must be a valid value");

        RuleFor(x => x.MaxMonthlyCapacity)
            .GreaterThan(0)
            .WithMessage("Max monthly capacity must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Max monthly capacity cannot exceed 1,000,000");

        RuleFor(x => x.CurrentCommitments)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Current commitments cannot be negative")
            .LessThanOrEqualTo(x => x.MaxMonthlyCapacity)
            .WithMessage("Current commitments cannot exceed max monthly capacity");

        RuleFor(x => x.QualityRating)
            .InclusiveBetween(0, 5)
            .WithMessage("Quality rating must be between 0 and 5");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateSupplierCapacityRequestValidator : AbstractValidator<UpdateSupplierCapacityRequest>
{
    public UpdateSupplierCapacityRequestValidator()
    {
        RuleFor(x => x.MaxMonthlyCapacity)
            .GreaterThan(0)
            .WithMessage("Max monthly capacity must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Max monthly capacity cannot exceed 1,000,000");

        RuleFor(x => x.CurrentCommitments)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Current commitments cannot be negative")
            .LessThanOrEqualTo(x => x.MaxMonthlyCapacity)
            .WithMessage("Current commitments cannot exceed max monthly capacity");

        RuleFor(x => x.QualityRating)
            .InclusiveBetween(0, 5)
            .WithMessage("Quality rating must be between 0 and 5")
            .When(x => x.QualityRating.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateSupplierPerformanceRequestValidator : AbstractValidator<UpdateSupplierPerformanceRequest>
{
    public UpdateSupplierPerformanceRequestValidator()
    {
        RuleFor(x => x.QualityScore)
            .InclusiveBetween(0, 5)
            .WithMessage("Quality score must be between 0 and 5");

        RuleFor(x => x.DeliveryDays)
            .GreaterThan(0)
            .WithMessage("Delivery days must be greater than 0")
            .LessThanOrEqualTo(365)
            .WithMessage("Delivery days cannot exceed 365 days");
    }
}

public class SupplierFilterRequestValidator : AbstractValidator<SupplierFilterRequest>
{
    public SupplierFilterRequestValidator()
    {
        RuleFor(x => x.ProductType)
            .IsInEnum()
            .WithMessage("Product type must be a valid value")
            .When(x => x.ProductType.HasValue);

        RuleFor(x => x.MinCapacity)
            .GreaterThan(0)
            .WithMessage("Minimum capacity must be greater than 0")
            .When(x => x.MinCapacity.HasValue);

        RuleFor(x => x.MinOnTimeRate)
            .InclusiveBetween(0, 1)
            .WithMessage("Minimum on-time rate must be between 0 and 1")
            .When(x => x.MinOnTimeRate.HasValue);

        RuleFor(x => x.MinQualityScore)
            .InclusiveBetween(0, 5)
            .WithMessage("Minimum quality score must be between 0 and 5")
            .When(x => x.MinQualityScore.HasValue);

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }
}

public class AvailableSuppliersRequestValidator : AbstractValidator<AvailableSuppliersRequest>
{
    public AvailableSuppliersRequestValidator()
    {
        RuleFor(x => x.ProductType)
            .IsInEnum()
            .WithMessage("Product type must be a valid value");

        RuleFor(x => x.RequiredCapacity)
            .GreaterThan(0)
            .WithMessage("Required capacity must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Required capacity cannot exceed 1,000,000");
    }
}