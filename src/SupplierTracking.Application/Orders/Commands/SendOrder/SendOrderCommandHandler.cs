using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Orders.Commands.SendOrder;

internal sealed class SendOrderCommandHandler : IRequestHandler<SendOrderCommand>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly IPublisher          _publisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SendOrderCommandHandler> _logger;

    public SendOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ICurrentUserService currentUser,
        ILogger<SendOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork      = unitOfWork;
        _publisher       = publisher;
        _currentUser     = currentUser;
        _logger          = logger;
    }

    public async Task Handle(SendOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order with id {request.OrderId} was not found.");

        if (!order.Items.Any())
            throw new InvalidOperationException("Cannot send an order with no items.");

        var previousStatus = order.Status;
        order.Send(_currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} sent to supplier '{SupplierName}' by user {UserId}",
            order.OrderNumber, order.Supplier?.Name, _currentUser.UserId);

        await _publisher.Publish(new OrderStatusChangedEvent(
            order.Id, order.OrderNumber, order.SupplierId,
            order.Supplier?.Name ?? string.Empty,
            previousStatus, order.Status,
            _currentUser.UserId, DateTime.UtcNow), cancellationToken);
    }
}
