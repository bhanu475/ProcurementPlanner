using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class CustomerOrderTests
{
    [Fact]
    public void CustomerOrder_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerId = "DODAAC123",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CustomerOrder_WithInvalidOrderNumber_ShouldFailValidation(string orderNumber)
    {
        // Arrange
        var order = CreateValidOrder();
        order.OrderNumber = orderNumber;

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("OrderNumber"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CustomerOrder_WithInvalidCustomerId_ShouldFailValidation(string customerId)
    {
        // Arrange
        var order = CreateValidOrder();
        order.CustomerId = customerId;

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("CustomerId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CustomerOrder_WithInvalidCustomerName_ShouldFailValidation(string customerName)
    {
        // Arrange
        var order = CreateValidOrder();
        order.CustomerName = customerName;

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("CustomerName"));
    }

    [Fact]
    public void CustomerOrder_WithTooLongOrderNumber_ShouldFailValidation()
    {
        // Arrange
        var order = CreateValidOrder();
        order.OrderNumber = new string('A', 51); // Exceeds 50 character limit

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("OrderNumber"));
    }

    [Fact]
    public void CustomerOrder_CanTransitionTo_ValidTransitions_ShouldReturnTrue()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        order.Status = OrderStatus.Submitted;
        Assert.True(order.CanTransitionTo(OrderStatus.UnderReview));

        order.Status = OrderStatus.UnderReview;
        Assert.True(order.CanTransitionTo(OrderStatus.PlanningInProgress));

        order.Status = OrderStatus.PlanningInProgress;
        Assert.True(order.CanTransitionTo(OrderStatus.PurchaseOrdersCreated));

        order.Status = OrderStatus.PurchaseOrdersCreated;
        Assert.True(order.CanTransitionTo(OrderStatus.AwaitingSupplierConfirmation));

        order.Status = OrderStatus.AwaitingSupplierConfirmation;
        Assert.True(order.CanTransitionTo(OrderStatus.InProduction));

        order.Status = OrderStatus.InProduction;
        Assert.True(order.CanTransitionTo(OrderStatus.ReadyForDelivery));

        order.Status = OrderStatus.ReadyForDelivery;
        Assert.True(order.CanTransitionTo(OrderStatus.Delivered));
    }

    [Fact]
    public void CustomerOrder_CanTransitionTo_InvalidTransitions_ShouldReturnFalse()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        order.Status = OrderStatus.UnderReview;
        Assert.False(order.CanTransitionTo(OrderStatus.Submitted));

        order.Status = OrderStatus.Delivered;
        Assert.False(order.CanTransitionTo(OrderStatus.InProduction));

        order.Status = OrderStatus.PlanningInProgress;
        Assert.False(order.CanTransitionTo(OrderStatus.Delivered));
    }

    [Fact]
    public void CustomerOrder_CanTransitionTo_CancelledStatus_ShouldAllowFromAnyStatusExceptDelivered()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        order.Status = OrderStatus.Submitted;
        Assert.True(order.CanTransitionTo(OrderStatus.Cancelled));

        order.Status = OrderStatus.InProduction;
        Assert.True(order.CanTransitionTo(OrderStatus.Cancelled));

        order.Status = OrderStatus.Delivered;
        Assert.False(order.CanTransitionTo(OrderStatus.Cancelled));
    }

    [Fact]
    public void CustomerOrder_TransitionTo_ValidTransition_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Status = OrderStatus.Submitted;

        // Act
        order.TransitionTo(OrderStatus.UnderReview);

        // Assert
        Assert.Equal(OrderStatus.UnderReview, order.Status);
    }

    [Fact]
    public void CustomerOrder_TransitionTo_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Status = OrderStatus.Delivered;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.TransitionTo(OrderStatus.InProduction));
    }

    [Fact]
    public void CustomerOrder_TotalQuantity_ShouldSumAllItemQuantities()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Items = new List<OrderItem>
        {
            new() { Quantity = 10 },
            new() { Quantity = 20 },
            new() { Quantity = 5 }
        };

        // Act
        var totalQuantity = order.TotalQuantity;

        // Assert
        Assert.Equal(35, totalQuantity);
    }

    [Fact]
    public void CustomerOrder_TotalQuantity_WithNoItems_ShouldReturnZero()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Items = new List<OrderItem>();

        // Act
        var totalQuantity = order.TotalQuantity;

        // Assert
        Assert.Equal(0, totalQuantity);
    }

    [Fact]
    public void CustomerOrder_IsOverdue_WithPastDeliveryDateAndNotDelivered_ShouldReturnTrue()
    {
        // Arrange
        var order = CreateValidOrder();
        order.RequestedDeliveryDate = DateTime.UtcNow.AddDays(-1);
        order.Status = OrderStatus.InProduction;

        // Act
        var isOverdue = order.IsOverdue;

        // Assert
        Assert.True(isOverdue);
    }

    [Fact]
    public void CustomerOrder_IsOverdue_WithPastDeliveryDateButDelivered_ShouldReturnFalse()
    {
        // Arrange
        var order = CreateValidOrder();
        order.RequestedDeliveryDate = DateTime.UtcNow.AddDays(-1);
        order.Status = OrderStatus.Delivered;

        // Act
        var isOverdue = order.IsOverdue;

        // Assert
        Assert.False(isOverdue);
    }

    [Fact]
    public void CustomerOrder_IsOverdue_WithFutureDeliveryDate_ShouldReturnFalse()
    {
        // Arrange
        var order = CreateValidOrder();
        order.RequestedDeliveryDate = DateTime.UtcNow.AddDays(1);
        order.Status = OrderStatus.InProduction;

        // Act
        var isOverdue = order.IsOverdue;

        // Assert
        Assert.False(isOverdue);
    }

    [Theory]
    [InlineData(ProductType.LMR)]
    [InlineData(ProductType.FFV)]
    public void CustomerOrder_WithValidProductType_ShouldAcceptAllTypes(ProductType productType)
    {
        // Arrange
        var order = CreateValidOrder();
        order.ProductType = productType;

        // Act
        var validationResults = ValidateModel(order);

        // Assert
        Assert.Empty(validationResults);
    }

    private static CustomerOrder CreateValidOrder()
    {
        return new CustomerOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerId = "DODAAC123",
            CustomerName = "Test Customer",
            ProductType = ProductType.LMR,
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = OrderStatus.Submitted,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}