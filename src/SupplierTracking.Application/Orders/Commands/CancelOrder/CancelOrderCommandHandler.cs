using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Events;

namespace SupplierTracking.Application.Orders.Commands.CancelOrder;

internal sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly IPublisher          _publisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ICurrentUserService currentUser,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork      = unitOfWork;
        _publisher       = publisher;
        _currentUser     = currentUser;
        _logger          = logger;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with id {request.OrderId} was not found.");

        if (!_currentUser.IsManager && order.CreatedByUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You are not authorized to cancel this order.");

        var previousStatus = order.Status;
        order.Cancel(_currentUser.UserId, request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} cancelled by user {UserId} — reason: '{Reason}'",
            order.OrderNumber, _currentUser.UserId, request.Reason ?? "N/A");

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
