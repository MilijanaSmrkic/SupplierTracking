using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;

namespace SupplierTracking.Application.Orders.Events;

internal sealed class OrderStatusChangedEventHandler
    : INotificationHandler<OrderStatusChangedEvent>
{
    private readonly IOrderNotificationService _notificationService;
    private readonly ILogger<OrderStatusChangedEventHandler> _logger;

    public OrderStatusChangedEventHandler(
        IOrderNotificationService notificationService,
        ILogger<OrderStatusChangedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    public async Task Handle(
        OrderStatusChangedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Broadcasting status change for order {OrderNumber}: {FromStatus} → {ToStatus}",
            notification.OrderNumber, notification.FromStatus, notification.ToStatus);

        await _notificationService.NotifyOrderStatusChangedAsync(notification, cancellationToken);
    }
}
