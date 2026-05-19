using MediatR;
using SupplierTracking.Application.Abstractions.Repositories;

namespace SupplierTracking.Application.Orders.Queries.GetOrderById;

internal sealed class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, OrderDetailResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository) =>
        _orderRepository = orderRepository;

    public async Task<OrderDetailResponse> Handle(
        GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with id {request.Id} was not found.");

        var items = order.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.Sku  ?? string.Empty,
            i.Quantity,
            i.UnitPrice,
            i.TotalPrice)).ToList();

        var logs = order.StatusLogs
            .OrderByDescending(l => l.ChangedAt)
            .Select(l => new StatusLogResponse(
                l.FromStatus,
                l.ToStatus,
                l.ChangedBy?.UserName,
                l.ChangedAt,
                l.Notes)).ToList();

        return new OrderDetailResponse(
            order.Id,
            order.OrderNumber,
            order.SupplierId,
            order.Supplier?.Name        ?? string.Empty,
            order.CreatedBy?.UserName   ?? string.Empty,
            order.Status,
            order.TrackingCode,
            order.Notes,
            order.ExpectedDeliveryDate,
            order.IsOverdue,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            items,
            logs);
    }
}
