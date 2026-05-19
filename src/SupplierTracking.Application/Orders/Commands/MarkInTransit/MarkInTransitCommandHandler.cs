using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Events;

namespace SupplierTracking.Application.Orders.Commands.MarkInTransit;

internal sealed class MarkInTransitCommandHandler : IRequestHandler<MarkInTransitCommand>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly IPublisher          _publisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<MarkInTransitCommandHandler> _logger;

    public MarkInTransitCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ICurrentUserService currentUser,
        ILogger<MarkInTransitCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork      = unitOfWork;
        _publisher       = publisher;
        _currentUser     = currentUser;
        _logger          = logger;
    }

    public async Task Handle(MarkInTransitCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with id {request.OrderId} was not found.");

        var previousStatus = order.Status;
        order.MarkInTransit(request.TrackingCode, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} marked InTransit — tracking '{TrackingCode}', supplier '{SupplierName}'",
            order.OrderNumber, request.TrackingCode ?? "N/A", order.Supplier?.Name);

        await _publisher.Publish(new OrderStatusChangedEvent(
            order.Id, order.OrderNumber, order.SupplierId,
            order.Supplier?.Name ?? string.Empty,
            previousStatus, order.Status,
            _currentUser.UserId, DateTime.UtcNow), cancellationToken);
    }
}
