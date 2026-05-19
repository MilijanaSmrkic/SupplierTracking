using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.ConfirmOrder;
using SupplierTracking.Application.Orders.Commands.MarkDelivered;
using SupplierTracking.Application.Orders.Commands.MarkInTransit;

namespace SupplierTracking.Application.Webhooks;

internal sealed class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly IPublisher          _publisher;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandler(
        IOrderRepository orderRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<ProcessWebhookCommandHandler> logger)
    {
        _orderRepository    = orderRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork         = unitOfWork;
        _publisher          = publisher;
        _logger             = logger;
    }

    public async Task Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            throw new KeyNotFoundException($"Supplier with id {request.SupplierId} was not found.");

        var order = await _orderRepository.GetByOrderNumberAsync(
            request.Payload.OrderNumber, cancellationToken);

        if (order is null)
            throw new KeyNotFoundException(
                $"Order '{request.Payload.OrderNumber}' was not found.");

        if (order.SupplierId != request.SupplierId)
        {
            _logger.LogWarning(
                "Webhook from supplier {SupplierId} tried to update order {OrderNumber} " +
                "which belongs to a different supplier",
                request.SupplierId, request.Payload.OrderNumber);

            throw new UnauthorizedAccessException(
                $"Order '{request.Payload.OrderNumber}' does not belong to this supplier.");
        }

        _logger.LogInformation(
            "Webhook received from supplier '{SupplierName}' — event '{Event}' for order {OrderNumber}",
            supplier.Name, request.Payload.Event, request.Payload.OrderNumber);

        switch (request.Payload.Event.ToLowerInvariant())
        {
            case WebhookEvents.Confirmed:
                await _publisher.Publish(
                    new ConfirmOrderCommand(order.Id, request.Payload.Notes),
                    cancellationToken);
                break;

            case WebhookEvents.Shipped:
                await _publisher.Publish(
                    new MarkInTransitCommand(order.Id, request.Payload.TrackingCode),
                    cancellationToken);
                break;

            case WebhookEvents.Delivered:
                await _publisher.Publish(
                    new MarkDeliveredCommand(order.Id),
                    cancellationToken);
                break;

            default:
                _logger.LogWarning(
                    "Unknown webhook event '{Event}' from supplier '{SupplierName}' for order {OrderNumber}",
                    request.Payload.Event, supplier.Name, request.Payload.OrderNumber);

                throw new InvalidOperationException(
                    $"Unknown webhook event: '{request.Payload.Event}'. " +
                    $"Supported events: {WebhookEvents.Confirmed}, {WebhookEvents.Shipped}, {WebhookEvents.Delivered}.");
        }
    }
}
