using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Entities;
using Xunit;

namespace ProcurementPlanner.Tests.Entities;

public class PurchaseOrderTests
{
    [Fact]
    public void PurchaseOrder_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();

        // Act
        var validationResults = ValidateModel(purchaseOrder);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PurchaseOrder_WithInvalidPurchaseOrderNumber_ShouldFailValidation(string orderNumber)
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.PurchaseOrderNumber = orderNumber;

        // Act
        var validationResults = ValidateModel(purchaseOrder);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("PurchaseOrderNumber"));
    }

    [Fact]
    public void PurchaseOrder_WithTooLongPurchaseOrderNumber_ShouldFailValidation()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.PurchaseOrderNumber = new string('A', 51); // Exceeds 50 character limit

        // Act
        var validationResults = ValidateModel(purchaseOrder);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("PurchaseOrderNumber"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PurchaseOrder_WithNegativeTotalValue_ShouldFailValidation(decimal totalValue)
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.TotalValue = totalValue;

        // Act
        var validationResults = ValidateModel(purchaseOrder);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("TotalValue"));
    }

    [Fact]
    public void PurchaseOrder_TotalQuantity_ShouldSumAllItemQuantities()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            new() { AllocatedQuantity = 10 },
            new() { AllocatedQuantity = 20 },
            new() { AllocatedQuantity = 5 }
        };

        // Act
        var totalQuantity = purchaseOrder.TotalQuantity;

        // Assert
        Assert.Equal(35, totalQuantity);
    }

    [Fact]
    public void PurchaseOrder_TotalQuantity_WithNoItems_ShouldReturnZero()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>();

        // Act
        var totalQuantity = purchaseOrder.TotalQuantity;

        // Assert
        Assert.Equal(0, totalQuantity);
    }

    [Fact]
    public void PurchaseOrder_IsOverdue_WithPastDeliveryDateAndNotDelivered_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.RequiredDeliveryDate = DateTime.UtcNow.AddDays(-1);
        purchaseOrder.Status = PurchaseOrderStatus.InProduction;

        // Act
        var isOverdue = purchaseOrder.IsOverdue;

        // Assert
        Assert.True(isOverdue);
    }

    [Fact]
    public void PurchaseOrder_IsOverdue_WithPastDeliveryDateButDelivered_ShouldReturnFalse()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.RequiredDeliveryDate = DateTime.UtcNow.AddDays(-1);
        purchaseOrder.Status = PurchaseOrderStatus.Delivered;

        // Act
        var isOverdue = purchaseOrder.IsOverdue;

        // Assert
        Assert.False(isOverdue);
    }

    [Fact]
    public void PurchaseOrder_IsAwaitingSupplierResponse_WithSentToSupplierStatus_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act
        var isAwaiting = purchaseOrder.IsAwaitingSupplierResponse;

        // Assert
        Assert.True(isAwaiting);
    }

    [Fact]
    public void PurchaseOrder_IsConfirmed_WithConfirmedStatus_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Confirmed;

        // Act
        var isConfirmed = purchaseOrder.IsConfirmed;

        // Assert
        Assert.True(isConfirmed);
    }

    [Fact]
    public void PurchaseOrder_IsRejected_WithRejectedStatus_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Rejected;

        // Act
        var isRejected = purchaseOrder.IsRejected;

        // Assert
        Assert.True(isRejected);
    }

    [Fact]
    public void PurchaseOrder_DaysUntilDelivery_ShouldCalculateCorrectly()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.RequiredDeliveryDate = DateTime.UtcNow.AddDays(5).Date;

        // Act
        var daysUntilDelivery = purchaseOrder.DaysUntilDelivery;

        // Assert
        Assert.Equal(5, daysUntilDelivery);
    }

    [Fact]
    public void PurchaseOrder_CanTransitionTo_ValidTransitions_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();

        // Act & Assert
        purchaseOrder.Status = PurchaseOrderStatus.Created;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.SentToSupplier));

        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Confirmed));
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Rejected));

        purchaseOrder.Status = PurchaseOrderStatus.Confirmed;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.InProduction));

        purchaseOrder.Status = PurchaseOrderStatus.InProduction;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.ReadyForShipment));

        purchaseOrder.Status = PurchaseOrderStatus.ReadyForShipment;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Shipped));

        purchaseOrder.Status = PurchaseOrderStatus.Shipped;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Delivered));
    }

    [Fact]
    public void PurchaseOrder_CanTransitionTo_InvalidTransitions_ShouldReturnFalse()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();

        // Act & Assert
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;
        Assert.False(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Created));

        purchaseOrder.Status = PurchaseOrderStatus.Delivered;
        Assert.False(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.InProduction));

        purchaseOrder.Status = PurchaseOrderStatus.Rejected;
        Assert.False(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Confirmed));
    }

    [Fact]
    public void PurchaseOrder_CanTransitionTo_CancelledStatus_ShouldAllowFromAnyStatusExceptDeliveredOrCancelled()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();

        // Act & Assert
        purchaseOrder.Status = PurchaseOrderStatus.Created;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Cancelled));

        purchaseOrder.Status = PurchaseOrderStatus.InProduction;
        Assert.True(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Cancelled));

        purchaseOrder.Status = PurchaseOrderStatus.Delivered;
        Assert.False(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Cancelled));

        purchaseOrder.Status = PurchaseOrderStatus.Cancelled;
        Assert.False(purchaseOrder.CanTransitionTo(PurchaseOrderStatus.Cancelled));
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_ValidTransition_ShouldUpdateStatus()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Created;

        // Act
        purchaseOrder.TransitionTo(PurchaseOrderStatus.SentToSupplier);

        // Assert
        Assert.Equal(PurchaseOrderStatus.SentToSupplier, purchaseOrder.Status);
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Delivered;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => purchaseOrder.TransitionTo(PurchaseOrderStatus.InProduction));
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_ConfirmedStatus_ShouldSetConfirmedAt()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act
        purchaseOrder.TransitionTo(PurchaseOrderStatus.Confirmed);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Confirmed, purchaseOrder.Status);
        Assert.NotNull(purchaseOrder.ConfirmedAt);
        Assert.True(purchaseOrder.ConfirmedAt.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_RejectedStatus_ShouldSetRejectedAt()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act
        purchaseOrder.TransitionTo(PurchaseOrderStatus.Rejected, "Quality concerns");

        // Assert
        Assert.Equal(PurchaseOrderStatus.Rejected, purchaseOrder.Status);
        Assert.NotNull(purchaseOrder.RejectedAt);
        Assert.Equal("Quality concerns", purchaseOrder.RejectionReason);
        Assert.True(purchaseOrder.RejectedAt.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_ShippedStatus_ShouldSetShippedAt()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.ReadyForShipment;

        // Act
        purchaseOrder.TransitionTo(PurchaseOrderStatus.Shipped);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Shipped, purchaseOrder.Status);
        Assert.NotNull(purchaseOrder.ShippedAt);
        Assert.True(purchaseOrder.ShippedAt.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void PurchaseOrder_TransitionTo_DeliveredStatus_ShouldSetDeliveredAt()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Shipped;

        // Act
        purchaseOrder.TransitionTo(PurchaseOrderStatus.Delivered);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Delivered, purchaseOrder.Status);
        Assert.NotNull(purchaseOrder.DeliveredAt);
        Assert.True(purchaseOrder.DeliveredAt.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void PurchaseOrder_ConfirmOrder_ShouldTransitionToConfirmedWithNotes()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act
        purchaseOrder.ConfirmOrder("Order confirmed, will start production");

        // Assert
        Assert.Equal(PurchaseOrderStatus.Confirmed, purchaseOrder.Status);
        Assert.Equal("Order confirmed, will start production", purchaseOrder.SupplierNotes);
        Assert.NotNull(purchaseOrder.ConfirmedAt);
    }

    [Fact]
    public void PurchaseOrder_RejectOrder_WithReason_ShouldTransitionToRejected()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act
        purchaseOrder.RejectOrder("Insufficient capacity");

        // Assert
        Assert.Equal(PurchaseOrderStatus.Rejected, purchaseOrder.Status);
        Assert.Equal("Insufficient capacity", purchaseOrder.RejectionReason);
        Assert.NotNull(purchaseOrder.RejectedAt);
    }

    [Fact]
    public void PurchaseOrder_RejectOrder_WithEmptyReason_ShouldThrowException()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.SentToSupplier;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => purchaseOrder.RejectOrder(""));
    }

    [Fact]
    public void PurchaseOrder_ValidatePurchaseOrder_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items.Add(new PurchaseOrderItem { AllocatedQuantity = 10 });

        // Act & Assert
        purchaseOrder.ValidatePurchaseOrder(); // Should not throw
    }

    [Fact]
    public void PurchaseOrder_ValidatePurchaseOrder_WithEmptyPurchaseOrderNumber_ShouldThrowException()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.PurchaseOrderNumber = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => purchaseOrder.ValidatePurchaseOrder());
    }

    [Fact]
    public void PurchaseOrder_ValidatePurchaseOrder_WithPastDeliveryDate_ShouldThrowException()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.RequiredDeliveryDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => purchaseOrder.ValidatePurchaseOrder());
    }

    [Fact]
    public void PurchaseOrder_ValidatePurchaseOrder_WithNoItems_ShouldThrowException()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => purchaseOrder.ValidatePurchaseOrder());
    }

    [Fact]
    public void PurchaseOrder_CalculateTotalValue_ShouldSumAllItemTotalPrices()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            new() { AllocatedQuantity = 10, UnitPrice = 5.0m },
            new() { AllocatedQuantity = 20, UnitPrice = 3.0m }
        };

        // Act
        purchaseOrder.CalculateTotalValue();

        // Assert
        Assert.Equal(110.0m, purchaseOrder.TotalValue); // (10 * 5.0) + (20 * 3.0) = 50 + 60 = 110
    }

    [Fact]
    public void PurchaseOrder_HasItemForProduct_WithMatchingProduct_ShouldReturnTrue()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            new() { ProductCode = "PROD-001" },
            new() { ProductCode = "PROD-002" }
        };

        // Act
        var hasItem = purchaseOrder.HasItemForProduct("PROD-001");

        // Assert
        Assert.True(hasItem);
    }

    [Fact]
    public void PurchaseOrder_HasItemForProduct_WithNonMatchingProduct_ShouldReturnFalse()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            new() { ProductCode = "PROD-001" },
            new() { ProductCode = "PROD-002" }
        };

        // Act
        var hasItem = purchaseOrder.HasItemForProduct("PROD-003");

        // Assert
        Assert.False(hasItem);
    }

    [Fact]
    public void PurchaseOrder_GetItemByProductCode_WithMatchingProduct_ShouldReturnItem()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        var expectedItem = new PurchaseOrderItem { ProductCode = "PROD-001", AllocatedQuantity = 10 };
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            expectedItem,
            new() { ProductCode = "PROD-002" }
        };

        // Act
        var item = purchaseOrder.GetItemByProductCode("PROD-001");

        // Assert
        Assert.NotNull(item);
        Assert.Equal(expectedItem, item);
    }

    [Fact]
    public void PurchaseOrder_GetItemByProductCode_WithNonMatchingProduct_ShouldReturnNull()
    {
        // Arrange
        var purchaseOrder = CreateValidPurchaseOrder();
        purchaseOrder.Items = new List<PurchaseOrderItem>
        {
            new() { ProductCode = "PROD-001" },
            new() { ProductCode = "PROD-002" }
        };

        // Act
        var item = purchaseOrder.GetItemByProductCode("PROD-003");

        // Assert
        Assert.Null(item);
    }

    private static PurchaseOrder CreateValidPurchaseOrder()
    {
        return new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = "PO-001",
            CustomerOrderId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Status = PurchaseOrderStatus.Created,
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(7),
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