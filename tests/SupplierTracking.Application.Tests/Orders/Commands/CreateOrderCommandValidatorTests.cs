using FluentValidation.TestHelper;
using SupplierTracking.Application.Orders.Commands.CreateOrder;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand ValidCommand() => new(
        SupplierId: Guid.NewGuid(),
        Items: [new OrderItemRequest(Guid.NewGuid(), 2)]);

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenSupplierIdIsEmpty()
    {
        var cmd = ValidCommand() with { SupplierId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.SupplierId);
    }

    [Fact]
    public void Should_Fail_WhenItemsIsEmpty()
    {
        var cmd = ValidCommand() with { Items = [] };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Should_Fail_WhenItemQuantityIsZero()
    {
        var cmd = ValidCommand() with
        {
            Items = [new OrderItemRequest(Guid.NewGuid(), 0)]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Should_Fail_WhenItemQuantityIsNegative()
    {
        var cmd = ValidCommand() with
        {
            Items = [new OrderItemRequest(Guid.NewGuid(), -1)]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Should_Fail_WhenProductIdIsEmpty()
    {
        var cmd = ValidCommand() with
        {
            Items = [new OrderItemRequest(Guid.Empty, 1)]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    [Fact]
    public void Should_Fail_WhenExpectedDeliveryIsInThePast()
    {
        var cmd = ValidCommand() with
        {
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(-1)
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ExpectedDeliveryDate);
    }

    [Fact]
    public void Should_Pass_WhenExpectedDeliveryIsNull()
    {
        var cmd = ValidCommand() with { ExpectedDeliveryDate = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpectedDeliveryDate);
    }

    [Fact]
    public void Should_Fail_WhenNotesExceed1000Characters()
    {
        var cmd = ValidCommand() with { Notes = new string('x', 1001) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
