using MediatR;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Orders.Queries.GetOrders;

internal sealed class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository) =>
        _orderRepository = orderRepository;

    public async Task<PagedResult<OrderSummaryResponse>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetPagedAsync(
            request.Page, request.PageSize,
            request.SupplierId, request.Status,
            cancellationToken);

        var total = await _orderRepository.CountAsync(
            request.SupplierId, request.Status, cancellationToken);

        var items = orders.Select(o => new OrderSummaryResponse(
            o.Id,
            o.OrderNumber,
            o.SupplierId,
            o.Supplier?.Name ?? string.Empty,
            o.Status,
            o.Items.Count,
            o.TotalAmount,
            o.ExpectedDeliveryDate,
            o.IsOverdue,
            o.CreatedAt)).ToList();

        return new PagedResult<OrderSummaryResponse>(items, total, request.Page, request.PageSize);
    }
}
