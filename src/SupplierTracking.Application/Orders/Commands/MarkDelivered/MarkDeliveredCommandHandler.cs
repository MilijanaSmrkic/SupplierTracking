using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Events;

namespace SupplierTracking.Application.Orders.Commands.MarkDelivered;

internal sealed class MarkDeliveredCommandHandler : IRequestHandler<MarkDeliveredCommand>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly IPublisher          _publisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<MarkDeliveredCommandHandler> _logger;

    public MarkDeliveredCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ICurrentUserService currentUser,
        ILogger<MarkDeliveredCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork      = unitOfWork;
        _publisher       = publisher;
        _currentUser     = currentUser;
        _logger          = logger;
    }

    public async Task Handle(MarkDeliveredCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with id {request.OrderId} was not found.");

        var previousStatus = order.Status;
        order.MarkDelivered(_currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} delivered — supplier '{SupplierName}', total {Total:C}",
            order.OrderNumber, order.Supplier?.Name, order.TotalAmount);

        try
        {
            await _publisher.Publish(new OrderStatusChangedEvent(
                order.Id, order.OrderNumber, order.SupplierId,
                order.Supplier?.Name ?? string.Empty,
                previousStatus, order.Status,
                _currentUser.UserId, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish OrderStatusChangedEvent for order {OrderId} (status: {Status})",
                order.Id, order.Status);
        }
    }
}
