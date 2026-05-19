using FluentValidation;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    private static readonly string[] ValidStatuses =
    [
        OrderStatuses.Draft,
        OrderStatuses.Sent,
        OrderStatuses.Confirmed,
        OrderStatuses.InTransit,
        OrderStatuses.Delivered,
        OrderStatuses.Cancelled
    ];

    public GetOrdersQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(q => q.Status)
            .Must(s => s is null || ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
