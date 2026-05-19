using FluentValidation.TestHelper;
using SupplierTracking.Application.Orders.Queries.GetOrders;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Queries;

public class GetOrdersQueryValidatorTests
{
    private readonly GetOrdersQueryValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenQueryIsDefault()
    {
        var result = _validator.TestValidate(new GetOrdersQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_WhenPageIsLessThanOne(int page)
    {
        var result = _validator.TestValidate(new GetOrdersQuery(Page: page));
        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(1000)]
    public void Should_Fail_WhenPageSizeIsOutOfRange(int pageSize)
    {
        var result = _validator.TestValidate(new GetOrdersQuery(PageSize: pageSize));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)]
    public void Should_Pass_WhenPageSizeIsValid(int pageSize)
    {
        var result = _validator.TestValidate(new GetOrdersQuery(PageSize: pageSize));
        result.ShouldNotHaveValidationErrorFor(q => q.PageSize);
    }

    [Theory]
    [InlineData(OrderStatuses.Draft)]
    [InlineData(OrderStatuses.Sent)]
    [InlineData(OrderStatuses.Confirmed)]
    [InlineData(OrderStatuses.InTransit)]
    [InlineData(OrderStatuses.Delivered)]
    [InlineData(OrderStatuses.Cancelled)]
    [InlineData(null)]
    public void Should_Pass_WhenStatusIsValidOrNull(string? status)
    {
        var result = _validator.TestValidate(new GetOrdersQuery(Status: status));
        result.ShouldNotHaveValidationErrorFor(q => q.Status);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("INVALID")]
    [InlineData("pending")]
    [InlineData("")]
    public void Should_Fail_WhenStatusIsInvalid(string status)
    {
        var result = _validator.TestValidate(new GetOrdersQuery(Status: status));
        result.ShouldHaveValidationErrorFor(q => q.Status);
    }
}
